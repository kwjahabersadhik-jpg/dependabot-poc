using CorporateCreditCardServiceReference;
using CreditCardsSystem.Data;
using CreditCardsSystem.Data.Models;
using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Models.Access;
using CreditCardsSystem.Domain.Models.Corporate;
using CreditCardsSystem.Utility.Crypto;
using CreditCardsSystem.Utility.Extensions;
using Kfh.Aurora.Auth;
using Kfh.Aurora.Integration;
using Kfh.Aurora.Logging;
using Kfh.Aurora.Organization;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace CreditCardsSystem.Application.CorporateProfile;

public class CorporateProfileAppService(IIntegrationUtility integrationUtility, IOptions<IntegrationOptions> options, FdrDBContext fdrDBContext, ICustomerProfileAppService customerProfileAppService, IOrganizationClient organizationClient, IConfigurationAppService configurationAppService, IAuthManager authManager, IAuditLogger<CorporateProfileAppService> audit, ICustomerProfileAppService genericCustomerProfileAppService) : BaseApiResponse, IAppService, ICorporateProfileAppService
{
    private readonly FdrDBContext _fdrDBContext = fdrDBContext;
    private readonly ICustomerProfileAppService _customerProfileAppService = customerProfileAppService;
    private readonly ICustomerProfileAppService _genericCustomerProfileAppService = genericCustomerProfileAppService;

    private readonly CorporateCreditCardServiceClient corporateCreditCardServiceClient = integrationUtility.GetClient<CorporateCreditCardServiceClient>(options.Value.Client, options.Value.Endpoints.CorporateCreditCard, options.Value.BypassSslValidation);

    private readonly IOrganizationClient _organizationClient = organizationClient;
    private readonly IConfigurationAppService _configurationAppService = configurationAppService;
    private readonly IAuthManager _authManager = authManager;
    private readonly IAuditLogger<CorporateProfileAppService> _auditLogger = audit;

    [HttpGet]
    public async Task<ApiResponseModel<CorporateProfileDto>> GetProfile(string civilId)
    {

        if (!long.TryParse(civilId, out long _corporateCivilId))
            return Failure<CorporateProfileDto>("Invalid CorporateCivilId");

        CorporateProfileDto corporateProfile = (await _fdrDBContext.CorporateProfiles.AsNoTracking().FirstOrDefaultAsync(x => x.CorporateCivilId == civilId))?.Adapt<CorporateProfileDto>() ?? throw new ApiException(message: "Profile not found");
        await BindProperties(corporateProfile);

        return Success(corporateProfile);
    }

    [HttpGet]
    public async Task<ApiResponseModel<CorporateProfileDto>> GetProfileForEdit(string civilId, bool includeValidation = true)
    {
        if (!long.TryParse(civilId, out long _corporateCivilId))
            return Failure<CorporateProfileDto>("Invalid CorporateCivilId");

        var corporateProfile = (await _fdrDBContext.CorporateProfiles.FindAsync(civilId))?.Adapt<CorporateProfileDto>();

        if (corporateProfile is not null)
        {
            corporateProfile = await BindProperties(corporateProfile, includeValidation, includeCards: true);
            return Success(corporateProfile);
        }


        _auditLogger.Log.Information("Corporate civil id {civilid} not found in FDR.", civilId);
        //TODO : why should we use this method again (diff between customer profile and detail generic)
        var profileInPhenix = await _genericCustomerProfileAppService.GetCustomerProfileMinimal(new() { CivilId = civilId });
        var profile = (profileInPhenix?.Data) ?? throw new ApiException(message: GlobalResources.DataNotFoundCorporate, insertSeriLog: true);

        if (profile.CustomerType.Equals("Personal", StringComparison.InvariantCultureIgnoreCase))
            throw new ApiException(message: GlobalResources.DataNotFoundCorporate, insertSeriLog: true);

        string EnglishName = $"{profile.FirstName} {profile.LastName}";
        string? addressLine1 = string.IsNullOrEmpty(corporateProfile?.AddressLine1) ? profile.CustomerAddresses?[0]?.AddressLine1 : corporateProfile.AddressLine1;
        string? addressLine2 = string.IsNullOrEmpty(corporateProfile?.AddressLine2) ? profile.CustomerAddresses?[0]?.AddressLine2 : corporateProfile.AddressLine2;

        corporateProfile = new CorporateProfileDto()
        {
            CorporateNameEn = EnglishName,
            CorporateNameAr = profile.ArabicName ?? "",
            CorporateCivilId = profile.CivilId,
            RimCode = Convert.ToDecimal(profile.RimCode),
            AddressLine1 = addressLine1,
            AddressLine2 = addressLine2,
            EmbossingName = (EnglishName.Length < 27 ? EnglishName : EnglishName[..26]).ToUpper(),
            IsProfileNotFoundInFDR = true,
            IsActiveRim = profile.RimStatus is not null && profile.RimStatus.Equals("Active", StringComparison.InvariantCultureIgnoreCase)
        };

        corporateProfile = await BindProperties(corporateProfile, includeValidation, includeCards: false);

        return Success(corporateProfile);
    }

    [NonAction]
    public async Task<GlobalLimitDto> GetAndValidateGlobalLimit(string corporateCivilId)
    {
        var globalLimitResult = (await corporateCreditCardServiceClient.getCorporateGlobalLimitAsync(new() { corpCivilID = corporateCivilId }))?.getCorporateGlobalLimit ?? throw new ApiException(message: "Unable to find global limit");
        if (globalLimitResult.corpCreditCardGlobalLimitDTO is null)
            throw new ApiException(message: "No commitment for this corporate, kindly create one for it");

        var globalLimit = globalLimitResult.corpCreditCardGlobalLimitDTO[0].Adapt<GlobalLimitDto>();
        globalLimit.totalUsedLimit = Convert.ToDecimal(globalLimitResult.totalUsedLimit);

        return globalLimit;
    }




    [HttpDelete]
    public async Task<ApiResponseModel<CorporateProfileDto>> DeleteProfileInFdR(string civilId)
    {
        if (!_authManager.HasPermission(Permissions.CorporateProfile.Delete()))
            return Failure<CorporateProfileDto>(GlobalResources.UnAuthorized);

        var customerProfile = _fdrDBContext.CorporateProfiles.FirstOrDefault(x => x.CorporateCivilId == civilId);

        if (customerProfile == null)
            return Failure<CorporateProfileDto>("Corporate Profile not found to delete!");

        if (_fdrDBContext.RequestParameters.Any(x => x.Parameter == "corporate_civil_id" && x.Value == civilId))
            return Failure<CorporateProfileDto>("Cannot delete profile as cards issued against it.");

        _fdrDBContext.CorporateProfiles.Remove(customerProfile);

        await _fdrDBContext.SaveChangesAsync();

        return Success<CorporateProfileDto>(new(), message: $"Corporate Profile has been deleted (civilId: {civilId} ) successfully! ");
    }

    async Task<CorporateProfileDto> BindProperties(CorporateProfileDto corporateProfile, bool includeValidation = true, bool includeCards = true)
    {
        corporateProfile.GlobalLimitDto ??= await GetAndValidateGlobalLimit(corporateProfile.CorporateCivilId);

        if (includeValidation)
        {
            await IsExpired(corporateProfile.GlobalLimitDto);

            if (!HasCommitment(corporateProfile.GlobalLimitDto))
                throw new ApiException(message: "No commitment for this corporate, kindly create one for it");
        }

        var customerClass = await _customerProfileAppService.GetCustomerRimClass();
        corporateProfile.UsedLimit = Convert.ToDecimal(corporateProfile.GlobalLimitDto.totalUsedLimit);
        corporateProfile.AvailableLimit = Convert.ToDecimal(corporateProfile.GlobalLimitDto.UndisbursedAmount);
        corporateProfile.CustomerClass = customerClass.FirstOrDefault(x => x.ClassCode == corporateProfile.RimCode)?.ClassDescriptionEn ?? corporateProfile.RimCode.ToString();
        if (includeCards)
            corporateProfile.CorporateCards = await GetCorporateCards(corporateProfile.CorporateCivilId);

        return corporateProfile;
    }

    [NonAction]
    public async Task IsExpired(GlobalLimitDto card)
    {
        if (!card.MaturityDateSpecified)
            throw new ApiException(message: "Corporate Profile does not have an expiration date");

        DateTime? matureDate = card.MaturityDate;
        if (matureDate is not null && DateTime.Compare(matureDate.Value, DateTime.Now) < 0)//greater means not expired
            throw new ApiException(message: "Corporate Profile is Expired.");

        await Task.CompletedTask;
    }

    private bool HasCommitment(GlobalLimitDto globalLimit) => globalLimit.CommitmentNo != null;

    private async Task<List<CorporateCard>> GetCorporateCards(string civilId)
    {
        var branches = (await _organizationClient.GetBranches()).Select(x => new Branch() { BranchId = x.BranchId, Name = Regex.Replace(x.Name, @"[^A-Za-z]", " ").Trim() });
        var currencies = _fdrDBContext.CardCurrencies.AsNoTracking();
        var allowedCardTypes = (await _configurationAppService.GetValue(ConfigurationBase.SO_AllowedCardTypes))?.Split(",") ?? Array.Empty<string>();
        bool canViewCardNumber = _authManager.HasPermission(Permissions.CreditCardsNumber.View());


        var ownedCards = await (from rp in _fdrDBContext.RequestParameters.AsNoTracking()
                                join request in _fdrDBContext.Requests on rp.ReqId equals request.RequestId
                                where rp.Parameter == "corporate_civil_id" && rp.Value == civilId
                                join reqStatus in _fdrDBContext.RequestStatuses.AsNoTracking() on request.ReqStatus equals reqStatus.StatusId
                                join product in _fdrDBContext.CardDefs.AsNoTracking() on request.CardType equals product.CardType
                                let requestParameterForCardCategory = request.Parameters.FirstOrDefault(x => x.ReqId == request.RequestId && x.Parameter == "IsSupplementaryOrPrimaryChargeCard")
                                let cardSubTypeAndName = GetCreditCardProductName(product, requestParameterForCardCategory == null ? "" : requestParameterForCardCategory.Value)
                                let membership = request.Parameters.FirstOrDefault(x => x.ReqId == request.RequestId && x.Parameter == "CLUB_MEMBERSHIP_ID")
                                let collateral = request.Parameters.FirstOrDefault(x => x.ReqId == request.RequestId && x.Parameter == "ISSUING_OPTION")
                                let CurrencyORG = product.CardDefExts.FirstOrDefault(x => x.Attribute.ToUpper() == "ORG") != null ? product.CardDefExts.First(x => x.Attribute.ToUpper() == "ORG").Value : ""
                                select new CorporateCard
                                {
                                    CivilId = request.CivilId,
                                    MobileNumber = request.Mobile,
                                    CurrencyISO = CurrencyORG,
                                    RequestId = request.RequestId,
                                    ProductType = cardSubTypeAndName.productName,
                                    CardCategory = cardSubTypeAndName.cardCategory,
                                    CardNumber = request.CardNo ?? "",
                                    CardNumberDto = request.CardNo.SaltThis(),//  canViewCardNumber ? request.CardNo ?? "" : request.CardNo.Masked(6, 6),
                                    CardType = request.CardType,
                                    AccountNumber = request.AcctNo!,
                                    BranchId = request.BranchId,
                                    CardLimit = request.ApproveLimit ?? 0,
                                    ExpirationDate = request.Expiry == null || (request.Expiry != null && request.Expiry!.Trim() == "0000") ? null : DateTime.ParseExact(request.Expiry!, ConfigurationBase.ExpiryDateFormat, CultureInfo.InvariantCulture).AddMonths(1).AddDays(-1),
                                    OpenedDate = (DateTime)request.ApproveDate!,
                                    StatusId = request.ReqStatus,
                                    Status = reqStatus.EnglishDescription,
                                    HoldAmount = request.DepositAmount,
                                    ApprovedLimit = request.ApproveLimit ?? 0,
                                    MemberShipId = membership != null ? membership.Value : "",
                                    Collateral = collateral != null ? collateral.Value : "",
                                })
                      .ToListAsync();

        ownedCards.ForEach(x =>
        {
            x.BranchName = branches.FirstOrDefault(b => b.BranchId == x.BranchId)?.Name ?? "";
            x.CurrencyISO = currencies.FirstOrDefault(cur => cur.Org == x.CurrencyISO)?.CurrencyIsoCode ?? "";
        });

        return ownedCards;
    }

    private static (string productName, CardCategoryType cardCategory) GetCreditCardProductName(CardDefinition product, string cardType)
    {
        if (string.IsNullOrEmpty(cardType))
            cardType = "N";

        string? productNameWithType = product.Name;

        if (decimal.TryParse(product.MinLimit, out decimal _minLimit) && decimal.TryParse(product.MaxLimit, out decimal _maxLimit))
            productNameWithType = $"{productNameWithType} -  {Helpers.GetProductType(product.Duality, _minLimit, _maxLimit)}";

        return cardType switch
        {
            "S" => ($"{productNameWithType} (Supplementary)", CardCategoryType.Supplementary),
            "P" => ($"{productNameWithType} (Primary)", CardCategoryType.Primary),
            "N" => ($"{productNameWithType}", CardCategoryType.Normal),
            _ => ($"{productNameWithType}", CardCategoryType.Normal)
        };
    }



}
