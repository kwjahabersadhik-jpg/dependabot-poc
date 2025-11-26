using CreditCardPromotionServiceReference;
using CreditCardsSystem.Data;
using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Models.BCDPromotions.Requests;
using CreditCardsSystem.Domain.Models.Promotions;
using CreditCardsSystem.Domain.Models.UserSettings;
using CreditCardsSystem.Domain.Shared.Enums;
using CreditCardsSystem.Domain.Shared.Interfaces;
using HrServiceReference;
using Kfh.Aurora.Auth;
using Kfh.Aurora.Integration;
using Kfh.Aurora.Organization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CreditCardsSystem.Application.Promotions;


public class PromotionsAppService(IIntegrationUtility integrationUtility, IOptions<IntegrationOptions> options,
        FdrDBContext fdrDbContext, ILogger<IPromotionsAppService> logger,
        IRequestMaker<PromotionDto> requestMakerAppService, IAuthManager authManager,
        IUserPreferencesClient userPreferencesClient, IOrganizationClient organizationClient)
    : IPromotionsAppService, IAppService
{
    private readonly CreditCardPromotionServicesServiceClient _promotionServiceClient = integrationUtility.GetClient<CreditCardPromotionServicesServiceClient>
        (options.Value.Client, options.Value.Endpoints.PromotionService, options.Value.BypassSslValidation);
    private readonly HrServiceClient _hrServiceClient = integrationUtility.GetClient<HrServiceClient>
        (options.Value.Client, options.Value.Endpoints.Hr, options.Value.BypassSslValidation);


    private async Task<Branch> GetUserBranch()
    {
        var userPreference = await userPreferencesClient.GetUserPreferences(authManager.GetUser()?.KfhId!);

        if (!int.TryParse(userPreference?.FromUserPreferences().DefaultBranchIdValue, out int _defaultBranchId))
            throw new ApiException(message: "Invalid user branch");

        var defaultBranches = userPreference?.FromUserPreferences().UserBranches;
        if (!(defaultBranches?.Any() ?? false))
            defaultBranches = await organizationClient.GetUserBranches(authManager.GetUser()?.KfhId!);

        var defaultBranch = defaultBranches?.FirstOrDefault(x => x.BranchId == _defaultBranchId);
        return defaultBranch is null ? throw new ApiException(message: "Invalid user branch") : defaultBranch;
    }

    [HttpPost]
    public async Task<ApiResponseModel<List<CreditCardPromotionDto>>> GetActivePromotionsByAccountNumber([FromBody] GetActivePromotionsRequest request)
    {
        await request.ModelValidationAsync(nameof(GetActivePromotionsRequest));

        var response = new ApiResponseModel<List<CreditCardPromotionDto>>();
        var userBranch = await GetUserBranch();


        request.UserBranch = userBranch.BranchId == 0 ? 1 : userBranch.BranchId;
        request.Collateral ??= Collateral.PREPAID_CARDS;

        if (string.IsNullOrEmpty(request.AccountNumber))
            return response.Success(await GetActivePromotionsByProductId(request));


        var promotionsResult = (await _promotionServiceClient.getAvailablepromotionsWithAcctAsync(new()
        {
            cardType = request.ProductId.ToString(),
            civilID = request.CivilId,
            acct = request.AccountNumber,
            userBranch = request.UserBranch.ToString(),
            collateralId = (int)request.Collateral
        }))?.getAvailablepromotionsWithAcctResult;

        if (promotionsResult == null)
            return response.Fail("Promotions Not Found");

        var promotions = await FilteringPromotionsByValidation(promotionsResult, request.AccountNumber);

        return response.Success(promotions);

    }


    [NonAction]
    public async Task<List<CreditCardPromotionDto>> GetActivePromotionsByProductId([FromBody] GetActivePromotionsRequest request)
    {
        try
        {
            var promotionsResult = (await _promotionServiceClient.getAvailablepromotionsAsync(new getAvailablepromotionsRequest()
            {
                cardType = request.ProductId.ToString(),
                civilID = request.CivilId,
                userBranch = request.UserBranch.ToString(),
                collateralId = (int)request.Collateral
            }))?.getAvailablepromotionsResult;

            if (promotionsResult == null)
                return new();

            var promotions = await FilteringPromotionsByValidation(promotionsResult);

            return promotions;
        }
        catch (System.Exception)
        {
            return new();
        }

    }

    [NonAction]
    public async Task AddPromotionToBeneficiary(AddPromotionToBeneficiaryRequest request)
    {
        if (string.IsNullOrEmpty(request.PromotionName)) return;

        var selectedPromotion = await fdrDbContext.Promotions.AsNoTracking().FirstOrDefaultAsync(x => x.PromotionName == request.PromotionName);
        if (selectedPromotion != null)
        {
            await fdrDbContext.PromotionBeneficiaries.AddAsync(new()
            {
                CivilId = request.CivilId,
                PromotionId = selectedPromotion.PromotionId,
                CardNo = request.RequestId.ToString(),
                ApplicationDate = request.ApplicationDate,
                Remarks = request.Remarks
            });
        }
        await fdrDbContext.SaveChangesAsync();
    }
    private async Task<List<CreditCardPromotionDto>> FilteringPromotionsByValidation(object[]? promotionsResult, string? accountNumber = "")
    {
        var promotions = promotionsResult?.AsQueryable().ProjectToType<CreditCardPromotionDto>();
        promotions = promotions?.Where(x => !x.promoName.Equals(ConfigurationBase.FreeCardsPromotionName));
        promotions = promotions?.Where(x => !x.isSupplementaryChargeCard);

        if (!string.IsNullOrEmpty(accountNumber))
        {
            var employeeInfo = (await _hrServiceClient.getEmployeeNoByAcctAsync(new() { acctNo = accountNumber }))?.getEmployeeNoByAcctResult;
            if (employeeInfo is null)
                promotions = promotions?.Where(x => x.isStaff != 1);
        }


        return promotions?.ToList() ?? new();
    }

    [NonAction]
    public async Task<CreditCardPromotionDto?> GetPromotionById(GetActivePromotionsRequest request)
    {
        var availablePromotions = await GetActivePromotionsByAccountNumber(request);
        if (!availablePromotions.IsSuccessWithData)
            return null;

        return availablePromotions.Data?.FirstOrDefault(x => x.promotionId == request.PromotionId)?.Adapt<CreditCardPromotionDto?>();
    }

    [HttpGet]
    public async Task<ApiResponseModel<List<PromotionDto>>> GetPromotions()
    {
        //don't map dynamically
        var promotions = await fdrDbContext.Promotions.AsNoTracking().Select(p => new PromotionDto
        {
            PromotionId = p.PromotionId,
            PromotionName = p.PromotionName,
            PromotionDescription = p.PromotionDescription,
            StartDate = p.StartDate,
            EndDate = p.EndDate ?? DateTime.MinValue,
            IsLocked = p.Islocked,
            Status = p.Status,
            UsageFlag = p.UsageFlag
        }).ToListAsync();

        return new ApiResponseModel<List<PromotionDto>>().Success(promotions);
    }

    [HttpPost]
    public async Task<ApiResponseModel<AddRequestResponse>> AddRequest([FromBody] RequestDto<PromotionDto> request)
    {
        await using var transaction = await fdrDbContext.Database.BeginTransactionAsync();
        try
        {

            if (request.ActivityType == (int)ActivityType.Add)
            {
                request.Title = "New Promotion";
                request.Description = $"New promotion has been added with the name  '{request.NewData.PromotionName}'";
            }

            if (request.ActivityType == (int)ActivityType.Edit)
            {
                request.Title = "Updated Promotion";
                request.Description = $"The promotion data for {request.NewData.PromotionName} has been updated";
            }

            if (request.ActivityType == (int)ActivityType.Delete)
            {
                request.Title = "Deleted Promotion";
                request.Description = $"The promotion data for {request.NewData.PromotionName} has been deleted";
            }

            await requestMakerAppService.AddRequest(request);

            switch (request.ActivityType)
            {
                case (int)ActivityType.Edit:
                    await UpdateLockStatus(request.OldData.PromotionId);
                    break;

                case (int)ActivityType.Delete:
                    await UpdateLockStatus(request.NewData.PromotionId);
                    break;
            }

            await transaction.CommitAsync();
            return new ApiResponseModel<AddRequestResponse>().Success(null);

        }
        catch (System.Exception e)
        {
            await transaction.RollbackAsync();
            logger.LogError(e, nameof(AddRequest));
            return new ApiResponseModel<AddRequestResponse>().Fail("something went wrong during adding the request");

        }

    }

    [HttpPut]
    public async Task<ApiResponseModel<AddRequestResponse>> UpdateLockStatus(int id)
    {
        var result = await fdrDbContext.Promotions
                .Where(p => p.PromotionId == id)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.Islocked, p => true));

        return new ApiResponseModel<AddRequestResponse>() { IsSuccess = true };
    }
}
