using CreditCardsSystem.Application.CardIssuance.Builders;
using CreditCardsSystem.Data;
using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Models.CardIssuance;
using CreditCardsSystem.Domain.Shared.Interfaces;
using CreditCardsSystem.Domain.Shared.Models.Account;
using CreditCardsSystem.Domain.Shared.Models.Reports;
using CreditCardsSystem.Domain.Shared.Models.RequestActivity;
using CreditCardsSystem.Utility.Extensions;
using CreditCardUpdateServiceReference;
using HoldManagementServiceReference;
using Kfh.Aurora.Auth;
using Kfh.Aurora.Common.Shared.Interfaces.Customer;
using Kfh.Aurora.Integration;
using Kfh.Aurora.Logging;
using Kfh.Aurora.Organization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Telerik.DataSource.Extensions;

namespace CreditCardsSystem.Application.CardIssuance;

public class CardIssuanceAppService(IAuditLogger<CardIssuanceAppService> auditLogger,
                                    IConfiguration configuration,
                                    FdrDBContext fdrDBContext,
                                    IIntegrationUtility integrationUtility,
                                    IOptions<IntegrationOptions> options,
                                    ICustomerProfileAppService customerProfileAppService,
                                    IOrganizationClient organizationClient,
                                    IHttpContextAccessor httpContext,
                                    ILogger<CardIssuanceAppService> logger,
                                    IPromotionsAppService promotionsAppService,
                                    IDistributedCache cache,
                                    IRequestAppService requestAppService,
                                    IMemberShipAppService memberShipAppService,
                                    IEmployeeAppService employeeService,
                                    IAccountsAppService accountsAppService,
                                    ICurrencyAppService currencyAppService,
                                    ICardDetailsAppService cardDetailsAppService,
                                    ICardPaymentAppService cardPaymentAppService,
                                    ICustomerProfileAppService genericCustomerProfileAppService,
                                    IRequestActivityAppService requestActivityAppService,
                                    IPreRegisteredPayeeAppService preRegisteredPayeeAppService,
                                    ICardDirector builderDirector,
                                    IUserPreferencesClient userPreferencesClient,
                                    IAuthManager authManager,
                                    ILookupAppService lookupAppService,
                                    IConfigurationAppService configurationAppService,
                                    ICustomerProfileCommonApi customerProfileCommonApi)
    : BaseApiResponse, ICardIssuanceAppService, IAppService
{
    private readonly IAuditLogger<CardIssuanceAppService> _auditLogger = auditLogger;
    private readonly IConfiguration _configuration = configuration;
    #region Variables
    private readonly FdrDBContext _fdrDBContext = fdrDBContext;
    private readonly ICustomerProfileAppService _customerProfileAppService = customerProfileAppService;
    private readonly ICustomerProfileAppService _genericCustomerProfileAppService = genericCustomerProfileAppService;

    private readonly IOrganizationClient _organizationClient = organizationClient;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContext;
    private readonly ILogger<CardIssuanceAppService> _logger = logger;
    private readonly IPromotionsAppService _promotionsAppService = promotionsAppService;
    private readonly IRequestAppService _requestAppService = requestAppService;
    private readonly IDistributedCache _cache = cache;
    private readonly IMemberShipAppService _memberShipAppService = memberShipAppService;
    private readonly IEmployeeAppService _employeeService = employeeService;
    private readonly IAccountsAppService _accountsAppService = accountsAppService;
    private readonly ICurrencyAppService _currencyAppService = currencyAppService;
    private readonly ICardDetailsAppService _cardDetailsAppService = cardDetailsAppService;
    private readonly ICardPaymentAppService _cardPaymentAppService = cardPaymentAppService;
    private readonly IRequestActivityAppService _requestActivityAppService = requestActivityAppService;
    private readonly IPreRegisteredPayeeAppService _preRegisteredPayeeAppService = preRegisteredPayeeAppService;
    private readonly ICardDirector _cardDirector = builderDirector;
    private readonly CreditCardUpdateServicesServiceClient _updateServicesServiceClient = integrationUtility.GetClient<CreditCardUpdateServicesServiceClient>
            (options.Value.Client, options.Value.Endpoints.CreditCardUpdate, options.Value.BypassSslValidation);
    private readonly HoldManagementServiceClient _holdManagementServiceClient = integrationUtility.GetClient<HoldManagementServiceClient>
      (options.Value.Client, options.Value.Endpoints.HoldManagment, options.Value.BypassSslValidation);
    private readonly IAuthManager authManager = authManager;
    private readonly IUserPreferencesClient _userPreferencesClient = userPreferencesClient;
    private readonly ILookupAppService lookupAppService = lookupAppService;
    private readonly IConfigurationAppService configurationAppService = configurationAppService;
    private readonly ICustomerProfileCommonApi customerProfileCommonApi = customerProfileCommonApi;

    #endregion
    #region Constructor

    #endregion

    #region Public Methods

    [HttpGet]
    public async Task<ApiResponseModel<CardDefinitionDto>> GetEligibleCardDetail(int productId, string civilId)
    {
        var response = new ApiResponseModel<CardDefinitionDto>();

        var cardDefinitionResult = await (from cardDef in _fdrDBContext.CardDefs.AsNoTracking().Include(x => x.CardDefExts)
                                          where cardDef.CardType == productId
                                          join matrix in _fdrDBContext.CardtypeEligibilityMatixes.AsNoTracking() on cardDef.CardType equals matrix.CardType
                                          let currencyId = cardDef.CardDefExts.FirstOrDefault(x => x.Attribute.ToUpper() == ConfigurationBase.CurrencyConfigCode)
                                          let productType = Helpers.GetProductType(cardDef.Duality, Convert.ToDecimal(cardDef.MinLimit), Convert.ToDecimal(cardDef.MaxLimit))
                                          let issanceTypeId = Helpers.GetIssuanceType(productType)
                                          select new CardDefinitionDto
                                          {
                                              BinNo = cardDef.BinNo,
                                              ProductId = cardDef.CardType,
                                              Duality = cardDef.Duality,
                                              Fees = cardDef.Fees,
                                              Installments = cardDef.Installments,
                                              Islocked = cardDef.Islocked,
                                              MerchantAcct = cardDef.MerchantAcct,
                                              MonthlyMaxDue = cardDef.MonthlyMaxDue,
                                              Name = cardDef.Name,
                                              SystemNo = cardDef.SystemNo,
                                              MaxLimit = cardDef.MaxLimit != null ? decimal.Parse(cardDef.MaxLimit!) : null, //setting 0 incase of null value and will be considering as prepaid card
                                              MinLimit = cardDef.MinLimit != null ? decimal.Parse(cardDef.MinLimit!) : null,
                                              CurrencyId = currencyId.Value,
                                              Eligibility = new()
                                              {
                                                  IsCoBrandPrepaid = matrix.IsCobrandPrepaid ?? false,
                                                  IsCobrandCredit = matrix.IsCobrandCredit ?? false,
                                                  ProductType = productType,
                                                  IssuanceTypeId = issanceTypeId,
                                                  ProductID = cardDef.CardType,
                                                  ProductName = cardDef.Name,
                                                  //AllowedBranches = matrix.AllowedBranches,
                                                  IsCorporate = (bool)matrix.IsCorporate!,
                                                  Status = (bool)matrix.IsDisabled! ? CardStatuses.InActive : CardStatuses.Active,
                                                  Priority = matrix.Id,
                                                  CurrencyOriginalId = currencyId.Value,
                                                  AllowedNonKfh = matrix.AllowedNonKfh ?? false
                                              }
                                          }).FirstOrDefaultAsync();

        if (cardDefinitionResult is null)
            return response.Fail("Invalid ProductId");


        cardDefinitionResult.Extension = (await _cardDetailsAppService.GetCardDefinitionExtensionsByProductId(productId))?.Data;
        cardDefinitionResult.Promotions = await _promotionsAppService.GetActivePromotionsByProductId(new()
        {
            ProductId = productId,
            CivilId = civilId,
            Collateral = Collateral.PREPAID_CARDS
        });

        return response.Success(cardDefinitionResult);
    }

    [HttpGet]
    public async Task<ApiResponseModel<List<CardEligiblityMatrixDto>>> GetAllProducts(ProductTypes type)
    {
        var response = new ApiResponseModel<List<CardEligiblityMatrixDto>>();

        var allProducts = (await _cache.GetJsonAsync<IEnumerable<CardEligiblityMatrixDto>>("allProducts"))?.AsQueryable();

        if (allProducts == null)
        {
            allProducts = (from matrix in _fdrDBContext.CardtypeEligibilityMatixes.AsNoTracking().Where(x => x.IsDisabled == false)
                           join cardDef in _fdrDBContext.CardDefs.AsNoTracking() on matrix.CardType equals cardDef.CardType
                           join cur in _fdrDBContext.CardCurrencies.AsNoTracking() on cardDef.CardDefExts.First(x => x.Attribute.ToUpper() == (cardDef.Duality != 7 ? "ORG" : "ORG_VISA")).Value equals cur.Org
                           let extention = new CardDefinitionExtentionLiteDto()
                           {
                               UpgradeDowngradeToCardType = cardDef.CardDefExts.FirstOrDefault(x => x.Attribute.ToUpper() == "UPGRADE_DOWNGRADE_TO_CARD_TYPE") != null ? cardDef.CardDefExts.First(x => x.Attribute.ToUpper() == "UPGRADE_DOWNGRADE_TO_CARD_TYPE").Value : "",
                               AgeMaximumLimit = cardDef.CardDefExts.FirstOrDefault(x => x.Attribute.ToUpper() == "AGE_MAX_LIMIT") != null ? cardDef.CardDefExts.First(x => x.Attribute.ToUpper() == "AGE_MAX_LIMIT").Value : "",
                               AgeMinimumLimit = cardDef.CardDefExts.FirstOrDefault(x => x.Attribute.ToUpper() == "AGE_MIN_LIMIT") != null ? cardDef.CardDefExts.First(x => x.Attribute.ToUpper() == "AGE_MIN_LIMIT").Value : "",
                               Currency = cardDef.CardDefExts.FirstOrDefault(x => x.Attribute.ToUpper() == (cardDef.Duality != 7 ? "ORG" : "ORG_VISA")) != null ? cardDef.CardDefExts.First(x => x.Attribute.ToUpper() == (cardDef.Duality != 7 ? "ORG" : "ORG_VISA")).Value : "",
                           }
                           let productType = Helpers.GetProductType(cardDef.Duality, Convert.ToDecimal(cardDef.MinLimit), Convert.ToDecimal(cardDef.MaxLimit))
                           select new CardEligiblityMatrixDto
                           {
                               ProductType = productType,
                               ProductID = matrix.CardType,
                               ProductName = cardDef.Name,
                               IsCorporate = (bool)matrix.IsCorporate!,
                               Status = (bool)matrix.IsDisabled! ? CardStatuses.InActive : CardStatuses.Active,
                               Priority = matrix.Id,
                               AllowedBranches = matrix.AllowedBranches != null ? matrix.AllowedBranches!.Split(",", StringSplitOptions.None) : Array.Empty<string>(),
                               AllowedClassCodes = matrix.AllowedClassCode != null ? matrix.AllowedClassCode!.Split(",", StringSplitOptions.None) : Array.Empty<string>(),
                               Extention = extention,
                               CurrencyOriginalId = extention.Currency,
                               CurrencyDto = cur.Adapt<CardCurrencyDto>(),
                               IsCobrandCredit = (bool)matrix.IsCobrandCredit!,
                               IsCoBrandPrepaid = (bool)matrix.IsCobrandPrepaid!,
                               MinLimit = Convert.ToDecimal(cardDef.MinLimit),
                               MaxLimit = Convert.ToDecimal(cardDef.MaxLimit),
                           });

            await _cache.SetJsonAsync("allProducts", allProducts, 60);
        }

        if (type is ProductTypes.All)
            return response.Success(allProducts.ToList());

        return response.Success(allProducts.Where(x => x.ProductType == type).ToList());
    }



    [HttpPost]
    public async Task<ApiResponseModel<EligibleCardResponse>> GetEligibleCards([FromBody] EligibleCardRequest eligibleCardRequest)
    {
        //var customerProfile = await _genericCustomerProfileAppService.GetGenericCustomerProfile(new CustomerProfileSearchCriteria() { CivilId = civilId });

        //if (!customerProfile.IsSuccess)
        //    return Failure<EligibleCardResponse>(customerProfile.Message);

        await ValidateBiometricStatus(eligibleCardRequest.CivilId!);

        var allProducts = (await _cache.GetJsonAsync<IEnumerable<CardEligiblityMatrixDto>>("allProducts"))?.AsQueryable();

        allProducts ??= (await GetAllProducts(ProductTypes.All))?.Data?.AsQueryable();

        if (allProducts is null)
            return Success<EligibleCardResponse>(new() { EligibleCards = new() });

        var eligibleCards = await CardEligibilityFilter(allProducts, eligibleCardRequest);

        return Success<EligibleCardResponse>(new() { EligibleCards = eligibleCards });
    }



    //[HttpPost]
    //public async Task<ApiResponseModel<CardIssueResponse>> IssueAlousraCard([FromBody] CardIssueRequest request)
    //{
    //    request.Customer.Salary = 0;
    //    request.IssueDetailsModel.Card.RequiredLimit = 0;
    //    var primaryCard = await IssueNewCard(request);
    //    //TODO : needs to remove this below code, since supplementary moved to new page and and now it is not part of issue primary card
    //    if (request.SupplementaryModel != null && request.SupplementaryModel.Count > 0)
    //    {
    //        foreach (var suppCard in request.SupplementaryModel)
    //        {
    //            var SupplementaryRequest = request.Adapt<CardIssueRequest>();
    //            SupplementaryRequest.IssueDetailsModel.Card.ProductId = 25;
    //            SupplementaryRequest.Customer.CivilId = suppCard.CivilId;
    //            SupplementaryRequest.BillingAddressModel.Mobile = suppCard.Mobile;
    //            SupplementaryRequest.Remark = suppCard.Remarks;

    //            suppCard.PrimaryCardRequestID = primaryCard.Data!.RequestId.ToString(CultureInfo.CurrentCulture);
    //            var resultData = await _customerProfileAppService.GetCustomerProfileFromFdRlocalDb(request.Customer.CivilId);
    //            if (resultData.IsSuccessWithData)
    //                suppCard.PrimaryCardHolderName = resultData.Data!.HolderName;

    //            suppCard.PrimaryCivilID = request.Customer.CivilId;
    //            //suppCard.PrimaryCardNo = primaryCard.Data.
    //            SupplementaryRequest.SupplementaryModel = new(1) { suppCard };

    //            var supplementaryCard = await IssueNewCard(SupplementaryRequest);

    //            if (supplementaryCard.IsSuccessWithData)
    //            {
    //                PreregisteredPayee preregisteredPayee = new PreregisteredPayee()
    //                {
    //                    CivilId = request.Customer.CivilId,
    //                    CardNo = supplementaryCard.Data!.RequestId.ToString(CultureInfo.CurrentCulture),
    //                    Description = "",
    //                    StatusId = 3,
    //                    TypeId = 3,
    //                    FullName = suppCard.HolderName
    //                };
    //                await _preRegisteredPayeeAppService.AddPreregisteredPayee(preregisteredPayee);
    //            }
    //        }
    //    }
    //    return primaryCard;
    //}

    [HttpPost]
    public async Task<ApiResponseModel<CardIssueResponse>> IssueAlousraCard([FromBody] CardIssueRequest request)
    {
        await ValidateBiometricStatus(request.Customer.CivilId);

        var response = new ApiResponseModel<CardIssueResponse>();

        if (request == null)
            return response.Fail("Invalid request. Unable to parse, please check your json request");

        request.Customer.Salary = 0;
        request.IssueDetailsModel.Card.RequiredLimit = 0;

        RequestDto? primaryCardRequest = null;

        if (!request.IsWithPrimaryCard && request.SupplementaryModel is not null && request.SupplementaryModel.Any())
        {
            if (decimal.TryParse(request.SupplementaryModel[0]!.PrimaryCardRequestID, out decimal _primaryCardRequest))
            {
                primaryCardRequest = (await _requestAppService.GetRequestDetail(_primaryCardRequest))?.Data;
                request.IssueDetailsModel.Card.ProductId = primaryCardRequest!.CardType;
            }
        }

        var cardDefinition = ((await GetEligibleCardDetail(request.IssueDetailsModel.Card.ProductId, request.Customer.CivilId))?.Data) ?? throw new ApiException(message: "Invalid Product ID");



        //validation only for primary cards 
        if (request.IsWithPrimaryCard)
        {
            await request.ModelValidationAsync(nameof(IssueAlousraCard));
            await ValidateIssueNewCardRequest(request, cardDefinition, response.ValidationErrors);
        }

        //TODO
        // Prepare and update Hold amount (deposit)
        // Get activity type from request paramater value of "OLD_CARD_NO" parameter
        // log



        RequestDto newRequest = await BindRequest(request, cardDefinition, primaryCardRequest);

        using var transaction = await _fdrDBContext.Database.BeginTransactionAsync();
        try
        {
            if (request!.IssueDetailsModel!.CoBrand is not null)
                await _memberShipAppService.DeleteAndCreateMemberShipIfAny(newRequest.CivilId, request.IssueDetailsModel.CoBrand);

            await _requestAppService.CreateNewRequest(newRequest);
            await _requestAppService.AddRequestParameters(newRequest.Parameters, newRequest.RequestId);

            await _promotionsAppService.AddPromotionToBeneficiary(new()
            {
                ApplicationDate = DateTime.Now,
                CivilId = newRequest.CivilId,
                PromotionName = newRequest.Parameters.PromotionName,
                Remarks = newRequest.Remark,
                RequestId = newRequest.RequestId
            });

            await transaction.CommitAsync();

            //The below code only for supplementary charge card. 
            if (request.IsWithPrimaryCard && request.SupplementaryModel.AnyWithNull())
            {
                await IssueSupplementaryCards(new()
                {
                    PrimaryCardRequestID = newRequest.RequestId,
                    SupplementaryCards = request.SupplementaryModel!.AsQueryable().ProjectToType<SupplementaryEditModel>().ToList()
                });
            }
        }
        catch (System.Exception)
        {
            await transaction.RollbackAsync();
            await RollbackMoneyTransaction(request, newRequest);
            throw;
        }

        var result = new CardIssueResponse() { RequestId = newRequest.RequestId };

        _logger.LogInformation("{RequestType} - {NewRequestId}", ConfigurationBase.EVENT_CREATE_REQUEST, newRequest.RequestId);

        //TODO: Insert Request Delivery
        await InsertRequestActivity(request, cardDefinition, newRequest);

        return await Task.FromResult(response.Success(result));
    }

    [HttpPost]
    public async Task<ApiResponseModel<CardIssueResponse>> IssuePrepaidCard([FromBody] PrepaidCardRequest prepaidCardRequest)
    {
        await prepaidCardRequest.ModelValidationAsync(nameof(PrepaidCardRequest));

        await ValidateBiometricStatus(prepaidCardRequest.Customer.CivilId);

        var response = new ApiResponseModel<CardIssueResponse>();

        if (prepaidCardRequest == null)
            return response.Fail("Invalid request. Unable to parse, please check your json request");

        var prepaidCardBuilder = _cardDirector.GetBuilder(ProductTypes.PrePaid) as PrepaidCardBuilder;

        await prepaidCardBuilder!
             .WithRequest(prepaidCardRequest)
             .Validate();

        await prepaidCardBuilder.Prepare();

        await prepaidCardBuilder.Issue();

        await prepaidCardBuilder.InitiateWorkFlow();

        return response.Success(new()
        {
            RequestId = prepaidCardBuilder.NewCardRequestId
        }, message: GlobalResources.SuccessIssue);
    }

    [HttpPost]
    public async Task<ApiResponseModel<CardIssueResponse>> IssueChargeCard([FromBody] ChargeCardRequest chargeCardRequest)
    {
        await ValidateBiometricStatus(chargeCardRequest.Customer.CivilId);

        var response = new ApiResponseModel<CardIssueResponse>();

        if (chargeCardRequest == null)
            return response.Fail("Invalid request. Unable to parse, please check your json request");

        var chargeCardBuilder = _cardDirector.GetBuilder(ProductTypes.ChargeCard) as ChargeCardBuilder;

        await chargeCardBuilder!
             .WithRequest(chargeCardRequest)
             .Validate();

        await chargeCardBuilder.Prepare();

        await chargeCardBuilder.Issue();

        //await chargeCardBuilder.LogRequestActivity();

        await chargeCardBuilder.InitiateWorkFlow();

        decimal primaryRequestId = chargeCardBuilder.NewCardRequestId;

        if (!chargeCardRequest.SupplementaryCards.AnyWithNull())
            return response.Success(new() { RequestId = primaryRequestId });

        var supplementaryCardResponse = (await IssueSupplementaryCards(new()
        {
            PrimaryCardRequestID = primaryRequestId,
            SellerId = chargeCardRequest.SellerId,
            IsConfirmedSellerId = true,
            SupplementaryCards = chargeCardRequest.SupplementaryCards!.AsQueryable().ProjectToType<SupplementaryEditModel>().ToList()
        }))?.Data;

        return response.Success(new()
        {
            RequestId = primaryRequestId,
            SupplementaryCardResponse = supplementaryCardResponse
        }, message: GlobalResources.SuccessIssue);
    }

    [HttpPost]
    public async Task<ApiResponseModel<SupplementaryCardIssueResponse>> IssueSupplementaryCards([FromBody] SupplementaryCardIssueRequest request)
    {
        await ValidateBiometricStatus(request.Customer.CivilId);

        SupplementaryCardIssueResponse response = new();

        var supplementaryChargeCardBuilder = _cardDirector.GetBuilder(ProductTypes.Supplementary) as SupplementaryChargeCardBuilder;

        supplementaryChargeCardBuilder!
            .CollectPrimaryCardData(request.PrimaryCardRequestID);

        foreach (var suppCard in request.SupplementaryCards.ToList())
        {
            try
            {
                request.SupplementaryCards = new() { suppCard };

                await supplementaryChargeCardBuilder
                    .WithRequest(request)
                    .Validate();

                await supplementaryChargeCardBuilder.Prepare();

                var requestId = await supplementaryChargeCardBuilder.Issue();
                await supplementaryChargeCardBuilder.InitiateWorkFlow();
                response.SuccessCards.Add(requestId.ToString());
            }
            catch (System.Exception ex)
            {
                _logger.LogError(exception: ex, ex.Message);
                response.FailedCards.Add(new ApiResponseModel<string>().Fail($"{suppCard.CivilId}-{ex.Message}",
                    validationErrors: ex is ApiException ? (ex as ApiException)?.Errors ?? new() : new()));
                continue;
            }
        }

        return Success(response, message: GlobalResources.SuccessIssue);
    }

    [HttpPost]
    public async Task<ApiResponseModel<CardIssueResponse>> IssueTayseerCard([FromBody] TayseerCardRequest tayseerCardRequest)
    {
        await ValidateBiometricStatus(tayseerCardRequest.Customer.CivilId);

        var response = new ApiResponseModel<CardIssueResponse>();

        if (tayseerCardRequest == null)
            return response.Fail("Invalid request. Unable to parse, please check your json request");

        var prepaidCardBuilder = _cardDirector.GetBuilder(ProductTypes.Tayseer) as TayseerCardBuilder;

        await prepaidCardBuilder!
             .WithRequest(tayseerCardRequest)
             .Validate();

        await prepaidCardBuilder.Prepare();

        await prepaidCardBuilder.Issue();

        await prepaidCardBuilder.InitiateWorkFlow();

        return response.Success(new()
        {
            RequestId = prepaidCardBuilder.NewCardRequestId
        }, message: GlobalResources.SuccessIssue);
    }


    [HttpPost]
    public async Task<ApiResponseModel<CardIssueResponse>> IssueCorporateCard([FromBody] CorporateCardRequest corporateCardRequest)
    {
        var response = new ApiResponseModel<CardIssueResponse>();

        if (corporateCardRequest == null)
            return response.Fail("Invalid request. Unable to parse, please check your json request");

        var corporateCardBuilder = _cardDirector.GetBuilder(ProductTypes.Corporate) as CorporateCardBuilder;

        await corporateCardBuilder!
             .WithRequest(corporateCardRequest)
             .Validate();

        await corporateCardBuilder.Prepare();

        await corporateCardBuilder.Issue();

        await corporateCardBuilder.InitiateWorkFlow();

        return response.Success(new()
        {
            RequestId = corporateCardBuilder.NewCardRequestId
        }, message: GlobalResources.SuccessIssue);
    }
    #endregion

    #region Private Methods
    private async Task ValidateBiometricStatus(string civilId)
    {
        var bioStatus = await customerProfileCommonApi.GetBiometricStatus(civilId);
        if (bioStatus.ShouldStop)
            throw new ApiException(message: GlobalResources.BioMetricRestriction);
    }
    private async Task InsertRequestActivity(CardIssueRequest request, CardDefinitionDto cardDefinition, RequestDto newRequest)
    {
        if (cardDefinition?.Eligibility?.ProductType != ProductTypes.ChargeCard || request.IssueDetailsModel.Collateral == Collateral.AGAINST_SALARY)
            return;

        bool isNewMarginAccount = string.IsNullOrEmpty(request.IssueDetailsModel.Card.CollateralAccountNumber);

        RequestActivityDto? requestActivity = request.IssueDetailsModel.Collateral switch
        {
            Collateral.AGAINST_MARGIN => isNewMarginAccount ? new RequestActivityDto()
            {
                CivilId = newRequest.CivilId,
                IssuanceTypeId = (int)cardDefinition!.Eligibility!.IssuanceTypeId,
                CfuActivityId = (int)CFUActivity.MARGIN_ACCOUNT_CREATE,
                Details = new()
                    {
                        {ReportingConstants.MARGIN_ACCOUNT_NO,  newRequest.Parameters.MarginAccountNumber!} ,
                        {ReportingConstants.MARGIN_AMOUNT,  newRequest.Parameters.MarginAmount! }
                    }
            } : null,
            //TODO: Deposit with old Hold ID
            Collateral.AGAINST_DEPOSIT => new RequestActivityDto()
            {
                CivilId = newRequest.CivilId,
                IssuanceTypeId = (int)cardDefinition!.Eligibility!.IssuanceTypeId,
                CfuActivityId = (int)CFUActivity.HOLD_ADD,
                Details = new()
                    {
                        {ReportingConstants.DEPOSIT_ACCOUNT_NO,  newRequest.Parameters.DepositAccountNumber! } ,
                        {ReportingConstants.DEPOSIT_AMOUNT,  newRequest.Parameters.DepositAmount!},
                        {ReportingConstants.DEPOSIT_NUMBER,  newRequest.Parameters.DepositNumber!}
                    }
            },
            _ => null
        };

        if (requestActivity != null)
        {
            requestActivity.RequestId = newRequest.RequestId;
            requestActivity.BranchId = newRequest.BranchId;
            requestActivity.CivilId = newRequest.CivilId;
            requestActivity.CardType = newRequest.CardType;
            requestActivity.RequestActivityStatusId = (int)RequestActivityStatus.New;
            await _requestActivityAppService.LogRequestActivity(requestActivity!, searchExist: false);
        }
    }
    private async Task RollbackMoneyTransaction(CardIssueRequest request, RequestDto newRequest)
    {
        if (request.IssueDetailsModel.Collateral == Collateral.AGAINST_MARGIN)
        {
            _ = await _cardPaymentAppService.ReverseMonetary(new()
            {
                DebitAccountNumber = request.IssueDetailsModel.Card.DebitAccountNumber ?? "",
                ReferenceNumber = newRequest.Parameters.MarginTransferReferenceNumber,
                MarginAccount = new MarginAccount(request.IssueDetailsModel.Card.RequiredLimit, "", 0)
            });
        }

        if (request.IssueDetailsModel.Collateral == Collateral.AGAINST_DEPOSIT)
        {
            var removeHodlrequest = new removeHoldRequest()
            {
                accountNumber = request.IssueDetailsModel.Card.CollateralAccountNumber ?? "",
                blockNumber = Convert.ToInt64(newRequest.Parameters.DepositNumber)
            };


            if (_configuration.GetValue<bool>("Integration:LogRequest"))
            {
                _auditLogger.Log.Information("RollbackMoneyTransaction {request}", JsonConvert.SerializeObject(removeHodlrequest));
            }
            else
            {
                _auditLogger.Log.Information("RollbackMoneyTransaction civilid# {civilid}", request.Customer.CivilId);
            }

            _ = await _holdManagementServiceClient.removeHoldAsync(removeHodlrequest);
        }
    }


    private async Task<RequestDto> BindRequest(CardIssueRequest request, CardDefinitionDto cardDefinition, RequestDto? primaryCardRequest)
    {
        var customerProfile = ((await _genericCustomerProfileAppService.GetDetailedGenericCustomerProfile(new() { CivilId = request.Customer.CivilId }))?.Data) ??
             throw new ApiException(message: "invalid customer");

        decimal reqId = (await _fdrDBContext.Database.SqlQueryRaw<decimal>("SELECT FDR.SEQ.NEXTVAL FROM DUAL").ToListAsync()).FirstOrDefault();
        reqId = decimal.Parse($"{request.Customer.CivilId[5..]}{reqId}");
        Branch userBranch = await configurationAppService.GetUserBranch();
        request.BranchId = userBranch.BranchId;
        _ = int.TryParse(authManager.GetUser()?.KfhId, out int _userId);


        var currentUser = await _employeeService.GetCurrentLoggedInUser();
        var accounts = await _accountsAppService.GetAllAccounts(request.Customer.CivilId);

        RequestDto newRequest;
        bool isChargeCardSupplementary = request.IsSupplementaryRequest && !request.IsWithPrimaryCard && request.IssueDetailsModel.Card.ProductId is not (ConfigurationBase.AlOsraPrimaryCardTypeId or ConfigurationBase.AlOsraSupplementaryCardTypeId);

        //generate request for charge card supplementary from primary card
        if (isChargeCardSupplementary)
        {
            var supplementaryModel = request.SupplementaryModel?.FirstOrDefault();

            //Copying all data from parent request then will update one by one which needs to be change
            primaryCardRequest ??= new();
            newRequest = primaryCardRequest;

            var billingAddress = primaryCardRequest?.BillingAddress ?? new();
            billingAddress.Mobile = supplementaryModel?.Mobile;

            _ = Enum.TryParse(typeof(Collateral), newRequest!.Parameters.Collateral, out object? _collateral);

            newRequest.RequestId = reqId;
            newRequest.AcctNo = primaryCardRequest?.AcctNo;
            newRequest.CivilId = request.Customer.CivilId;
            newRequest.RequestedLimit = request.IssueDetailsModel.Card.RequiredLimit;
            newRequest.ApproveLimit = request.IssueDetailsModel.Card.RequiredLimit;
            newRequest.ApproveDate = DateTime.Now;
            newRequest.SellerId = request.Seller.SellerId;
            newRequest.BranchId = request.BranchId;
            //TODO Need to check
            //newRequest.Expiry = DateTime.Today.AddYears(3).ToString(ConfigurationBase.ExpiryDateFormat);

            newRequest.Remark = request.Remark;
            newRequest.ReqDate = DateTime.Now;
            newRequest.ReqStatus = (int)CreditCardStatus.Pending;
            newRequest.TellerId = Convert.ToDecimal(authManager.GetUser()?.KfhId);

            newRequest.City = billingAddress.City;
            newRequest.AddressLine1 = billingAddress.AddressLine1;
            newRequest.AddressLine2 = billingAddress.AddressLine2;
            newRequest.FaxReference = billingAddress.FaxReference;
            newRequest.HomePhone = billingAddress.HomePhone ?? 0;
            newRequest.PostOfficeBoxNumber = billingAddress.PostOfficeBoxNumber;
            newRequest.PostalCode = billingAddress.PostalCode ?? 0;
            newRequest.Mobile = billingAddress.Mobile;
            newRequest.Street = billingAddress.Street;
            newRequest.WorkPhone = billingAddress.WorkPhone;

            newRequest.Parameters = new()
            {
                Employment = customerProfile.IsRetired ? "1" : "0",
                Collateral = ((Collateral)_collateral!).ToString(),
                MarginAccountNumber = primaryCardRequest!.Parameters.MarginAccountNumber,
                MarginAmount = primaryCardRequest.Parameters.MarginAmount,
                MarginTransferReferenceNumber = primaryCardRequest.Parameters.MarginTransferReferenceNumber,
                DepositAccountNumber = primaryCardRequest.Parameters.DepositAccountNumber,
                DepositNumber = primaryCardRequest.Parameters.DepositNumber,
                DepositAmount = primaryCardRequest.Parameters.DepositAmount,
                DepositReferenceNumber = primaryCardRequest.Parameters.DepositReferenceNumber,

                Relation = supplementaryModel!.RelationName,
                PrimaryCardCivilId = supplementaryModel.PrimaryCivilID,
                PrimaryCardNumber = primaryCardRequest.CardNo!,
                PrimaryCardRequestId = supplementaryModel.PrimaryCardRequestID.ToString().Replace(".0", ""),
                PrimaryCardHolderName = supplementaryModel.PrimaryCardHolderName,
                CustomerClassCode = customerProfile.RimCode.ToString(),
                KFHCustomer = customerProfile.IsEmployee ? "1" : "0",
                IsSupplementaryOrPrimaryChargeCard = ChargeCardType.S.ToString()
            };

            //Need to be clear, after take it for supplementary card request parameter to avoid duplicate issue 
            newRequest.CardNo = null;
        }
        else
        {
            newRequest = new RequestDto()
            {
                RequestId = reqId,
                AcctNo = request.IssueDetailsModel.Card.DebitAccountNumber,
                CardType = request.IssueDetailsModel.Card.ProductId,
                FdAcctNo = request.IssueDetailsModel.Card.FDAccountNumber,

                ApproveDate = request.IssueDetailsModel.Card.ApproveDate ?? DateTime.Now,
                BranchId = request.BranchId,
                CivilId = request.Customer.CivilId,

                City = request.BillingAddressModel.City,
                AddressLine1 = request.BillingAddressModel.AddressLine1,
                AddressLine2 = request.BillingAddressModel.AddressLine2,
                FaxReference = request.BillingAddressModel.FaxReference,
                HomePhone = request.BillingAddressModel.HomePhone ?? 0,
                PostOfficeBoxNumber = request.BillingAddressModel.PostOfficeBoxNumber,
                PostalCode = request.BillingAddressModel.PostalCode ?? 0,
                Mobile = request.BillingAddressModel.Mobile,
                Street = request.BillingAddressModel.Street,
                WorkPhone = request.BillingAddressModel.WorkPhone,


                //Expiry = request.IssueDetailsModel.Card.Expiry ?? DateTime.Today.AddYears(3).ToString(ConfigurationBase.ExpiryDateFormat),
                ApproveLimit = request.IssueDetailsModel.Card.RequiredLimit,
                RequestedLimit = request.IssueDetailsModel.Card.RequiredLimit,
                MurInstallments = request.Installments?.MurabahaInstallments,
                ReInstallments = request.Installments?.RealEstateInstallment,

                Remark = request.Remark,
                ReqDate = DateTime.Now,
                Salary = request.Customer.Salary,
                ReqStatus = (int)CreditCardStatus.Pending,
                SellerId = request.Seller.SellerId,
                ServicePeriod = 0,
                TellerId = Convert.ToDecimal(authManager.GetUser()?.KfhId),
                Parameters = new()
            };
        }


        await BindSellerDetail();

        if (!isChargeCardSupplementary)
        {
            await BindCollateral();
            await PrepareMargin();
            await PrepareDeposit();
        }

        await BindPromotionDetail();
        await BindRequestParameters();

        return newRequest;

        #region local methods
        async Task PrepareDeposit()
        {
            if (request.IssueDetailsModel.Collateral is not Collateral.AGAINST_DEPOSIT)
                return;

            _ = int.TryParse(request.IssueDetailsModel.Card.DepositNumber, out int _holdId);
            _ = int.TryParse(authManager.GetUser()?.KfhId, out int _userId);
            string description = cardDefinition.Name + "-" + newRequest.RequestedLimit;

            var depositAccount = accounts?.FirstOrDefault(x => x.Acct == request.IssueDetailsModel.Card.CollateralAccountNumber)
                ?? throw new ApiException(new() { new(nameof(request.IssueDetailsModel.Card.CollateralAccountNumber), "Invalid deposit account number!") });


            if (_holdId <= 0)
            {
                await CreateNewHold();
                return;
            }


            viewAllHoldDetailsRequest viewHoldRequest = new() { acct = depositAccount.Acct, holdId = _holdId };

            if (_configuration.GetValue<bool>("Integration:LogRequest"))
            {
                _auditLogger.Log.Information("viewAllHoldDetailsAsync {request}", JsonConvert.SerializeObject(viewHoldRequest));
            }
            else
            {
                _auditLogger.Log.Information("viewAllHoldDetailsAsync Account# {acct} Hold#{holdId}", viewHoldRequest.acct, viewHoldRequest.holdId);
            }


            var holdData = (await _holdManagementServiceClient.viewAllHoldDetailsAsync(viewHoldRequest))?.viewAllHoldDetailsResult;

            if (holdData == null || holdData?.Status?.ToLower() == "closed")
            {
                await CreateNewHold();
                return;
            }

            bool inSufficientBalance = holdData != null && (double)request.IssueDetailsModel.Card.RequiredLimit != holdData.Amount;
            if (inSufficientBalance)
            {
                //removing hold
                removeHoldRequest removeHoldRequest = new()
                {
                    accountNumber = depositAccount.Acct,
                    description = description,
                    userId = _userId,
                    blockNumber = _holdId,
                    blockAmount = (double)request.IssueDetailsModel.Card.RequiredLimit
                };

                if (_configuration.GetValue<bool>("Integration:LogRequest"))
                {
                    _auditLogger.Log.Information("removeHoldAsync {request}", JsonConvert.SerializeObject(removeHoldRequest));
                }
                else
                {
                    _auditLogger.Log.Information("removeHoldAsync CivilId# {CivilId}", request.Customer.CivilId);
                }

                _ = await _holdManagementServiceClient.removeHoldAsync(removeHoldRequest);

                await CreateNewHold();
            }

            //TODO : Update old hold id with new one
            await UpdateHoldId();

            #region local methods
            async Task UpdateHoldId()
            {
                await Task.CompletedTask;
            }
            async Task CreateNewHold()
            {
                if (request.IssueDetailsModel.Card.RequiredLimit > depositAccount.AvailableBalance)
                    throw new ApiException(new() { new(nameof(request.IssueDetailsModel.Card.RequiredLimit), $"Hold amount {request.IssueDetailsModel.Card.RequiredLimit} is greater than deposit account available balance {depositAccount.AvailableBalance.ToMoney()}") });

                addHoldRequest addHoldRequest = new()
                {
                    acctNo = depositAccount.Acct,
                    amount = (double)request.IssueDetailsModel.Card.RequiredLimit,
                    currency = depositAccount.Currency,
                    holdExpiryDate = new DateTime(2075, 12, 31),
                    description = description,
                    userId = _userId,
                };

                if (_configuration.GetValue<bool>("Integration:LogRequest"))
                {
                    _auditLogger.Log.Information("addHoldAsync {request}", JsonConvert.SerializeObject(addHoldRequest));
                }
                else
                {
                    _auditLogger.Log.Information("addHoldAsync acctNo# {acctNo}", addHoldRequest.acctNo);
                }

                var newHold = (await _holdManagementServiceClient.addHoldAsync(addHoldRequest)).addHoldResult;

                newRequest.Parameters.DepositAccountNumber = depositAccount.Acct;
                newRequest.Parameters.DepositNumber = newHold.HoldId.ToString();
                newRequest.Parameters.DepositAmount = request.IssueDetailsModel.Card.RequiredLimit.ToString();
                newRequest.Parameters.DepositReferenceNumber = newHold.ReferenceNo.ToString();

                newRequest.DepositNo = newHold.HoldId.ToString();
                newRequest.DepositAmount = request.IssueDetailsModel.Card.RequiredLimit;
            }

            #endregion

            //TODO Create Voucher
        }

        async Task PrepareMargin()
        {
            if (request.IssueDetailsModel.Collateral is not Collateral.AGAINST_MARGIN)
                return;

            string debitMarginAccountNumber = "";//TODO: assign from UI
            decimal debitMarginAmount = 0;

            MarginAccount marginAccount = new(request.IssueDetailsModel.Card.RequiredLimit, debitMarginAccountNumber, debitMarginAmount);

            if (!string.IsNullOrEmpty(request.IssueDetailsModel.Card.CollateralAccountNumber))
            {
                var account = accounts?.FirstOrDefault(x => x.Acct == request.IssueDetailsModel.Card.CollateralAccountNumber);

                marginAccount.AccountNumber = account?.Acct ?? "";
                marginAccount.AvailableBalance = account?.AvailableBalance ?? 0;
            }
            else
            {
                // creating new margin account
                var debitAccount = accounts?.FirstOrDefault(x => x.Acct == request.IssueDetailsModel.Card.DebitAccountNumber);


                var newMarginAccountResponse = await _accountsAppService.CreateCustomerAccount(
                    new(empID: ConfigurationBase.CreateCustomerAccountEmpId,
                    branchNo: debitAccount?.BranchId?.ToString() ?? "",
                        acctType: "118",
                    acctClassCode: 1,
                    rimNo: customerProfile.RimNo,
                    title1: debitAccount?.Title1 ?? "",
                    title2: debitAccount?.Title2 ?? "",
                    description: "Opening RIM account",
                    faceAmount: 0));



                if (!newMarginAccountResponse.IsSuccess)
                    throw new ApiException(message: "Failed to create the margin account");


                //TODO: Print Voucher
                //await _voucherAppService.CreateVoucher(new()
                // {
                //     AccountNo=debitAccount.Acct,
                //     AccountName= debitAccount.Title,
                //     CivilID= request.Customer.CivilId,
                //     IBAN=debitAccount.Iban,
                //     PrintDate=DateTime.Now,
                //     AccountCurrency=debitAccount.Currency,
                // });

                marginAccount.AccountNumber = newMarginAccountResponse.Data ?? "";
            }


            //Transfer money in case of low balance
            if (marginAccount.HasInSuffienctBalance)
            {
                var transferResponse = await _cardPaymentAppService.TransferMonetary(new()
                {
                    DebitAccountNumber = request.IssueDetailsModel.Card.DebitAccountNumber!,
                    MarginAccount = marginAccount
                });

                if (!transferResponse.IsSuccess)
                    throw new ApiException(message: "Money Transfer Failed");

                newRequest.Parameters.MarginTransferReferenceNumber = transferResponse.Data?.ReferenceNumber ?? "";
            }

            newRequest.Parameters.MarginAccountNumber = marginAccount.AccountNumber;
            newRequest.Parameters.MarginAmount = marginAccount.AvailableBalance.ToString();
        }

        async Task<int> BindCustomerBranchId(string employeeNumber = "", string? debitAccountNumber = "")
        {
            if (!string.IsNullOrEmpty(debitAccountNumber))
            {
                // if the employeeNubmer availble then set 0 branch, else set the branch to officer branch
                return !string.IsNullOrEmpty(employeeNumber) ? 0 : Convert.ToInt16(debitAccountNumber[..2]);
            }

            // added by Haitham Salem: for non kfh we will use Active Directory Branch ID (logged in user) - "currentUser",
            // but for BCD branch and call center because there id's are 999, 995 we will use mapping
            var currentUserBranch = ConfigurationBase.CoBrandADPhnxBranchMapping.Split(',')
                .FirstOrDefault(pnxBranch => pnxBranch.Split('@')[0] == currentUser!.Location)?.Split('@')[1];

            _ = int.TryParse(currentUserBranch, out int _currentUserBranch);

            // if this branch is regular branch (not call center or BCD)
            if (_currentUserBranch != 0)
                return _currentUserBranch;

            //Updating the Logic; If location digit of length 3 and customer branch =0 then raise error
            if (currentUser!.Location is not null && currentUser!.Location.Length == 3)
                throw new ApiException(message: "Issuing Co-Brand card: not allowed branch"); //TODO: we need to change this error message. it seems not related to this validation

            _ = int.TryParse(currentUser.Location, out int _currentUserLocation);

            await Task.CompletedTask;
            return _currentUserLocation;
        }

        async Task BindSellerDetail()
        {
            if (request.Seller.SellerId <= 0) return;

            var validateSeller = await _employeeService.ValidateSellerId(request?.Seller.SellerId?.ToString() ?? "");
            if (validateSeller.IsSuccess)
            {
                newRequest.Parameters.SellerGenderCode = validateSeller.Data?.Gender;
            }

            request!.Customer.CustomerBranchId = await BindCustomerBranchId(customerProfile.EmployeeNumber ?? "", newRequest.AcctNo);

            newRequest!.Parameters.KFHStaffID = customerProfile.EmployeeNumber;
        }

        async Task BindPromotionDetail()
        {
            if (request.PromotionModel?.PromotionId is null) return;


            var selectedCardPromotion = await _promotionsAppService.GetPromotionById(new()
            {
                CivilId = request.Customer!.CivilId,
                ProductId = request.IssueDetailsModel.Card.ProductId,
                PromotionId = request.PromotionModel.PromotionId,
                Collateral = (Collateral)Enum.Parse(typeof(Collateral), newRequest.Parameters.Collateral ?? "0")
            });
            if (selectedCardPromotion == null) return;

            newRequest.Parameters.ServiceNumber = selectedCardPromotion?.serviceNo.ToString();
            newRequest.Parameters.ServiceNumberInMonths = selectedCardPromotion?.numberOfMonths.ToString();
            newRequest.Parameters.CollateralId = selectedCardPromotion?.collateralId.ToString();
            newRequest.Parameters.EarlyClosureFees = selectedCardPromotion?.earlyClosureFees;
            newRequest.Parameters.EarlyClosureMonths = selectedCardPromotion?.earlyClosureMonths;
            newRequest.Parameters.EarlyClosurePercentage = selectedCardPromotion?.earlyClosurePercentage;
            newRequest.Parameters.PCTId = selectedCardPromotion?.pctId;
            newRequest.Parameters.BCDFlag = selectedCardPromotion?.flag;
            newRequest.Parameters.PromotionName = selectedCardPromotion?.promoName;
            newRequest.Parameters.PCTFlag = selectedCardPromotion?.pctFlag;

            await Task.CompletedTask;
        }

        string GetDefaultPCT(string? kfhStaffId)
        {
            string PCT_Default = cardDefinition?.Extension?.PctDefault!;
            string PCT_Default_Staff = cardDefinition?.Extension?.PctDefaultStaff!;

            if (kfhStaffId != "0" && !string.IsNullOrEmpty(PCT_Default_Staff))
                return PCT_Default_Staff;

            return PCT_Default;
        }

        async Task BindCollateral()
        {
            //Try to set in client side
            newRequest.Parameters.Collateral = await GetCollateral();
            if (request.IssueDetailsModel.ActualCollateral is not null && request.IssueDetailsModel.Collateral != request.IssueDetailsModel.ActualCollateral)
            {
                newRequest.Parameters.ActualCollateral = request.IssueDetailsModel.ActualCollateral.ToString();
            }


            async Task<string?> GetCollateral()
            {
                if (cardDefinition!.Extension?.IsPrepaid == "1")
                    return Collateral.PREPAID_CARDS.ToString();

                if (request.IssueDetailsModel.Card.IsForeignCurrencyCard)
                    return Collateral.FOREIGN_CURRENCY_PREPAID_CARDS.ToString();

                if (request.IssueDetailsModel.Collateral is null)
                    return Collateral.EXCEPTION.ToString();

                if (request.IssueDetailsModel.Collateral == Collateral.AGAINST_SALARY && request.IsCBKRulesViolated)
                    return Collateral.SALARY_AND_MARGIN.ToString();

                await Task.CompletedTask;

                return request.IssueDetailsModel.Collateral.ToString();
            }

            await Task.CompletedTask;
        }

        async Task BindRequestParameters()
        {
            bool isCoBrand = request.IssueDetailsModel.CoBrand != null && request.IssueDetailsModel.CoBrand?.Company.CardType > 0;
            if (isCoBrand)
            {
                newRequest.Parameters.ClubMembershipId = request.IssueDetailsModel.CoBrand!.MemberShipId.ToString();
                newRequest.Parameters.CompanyName = request.IssueDetailsModel.CoBrand!.Company.CompanyName.ToString();
                newRequest.Parameters.ClubName = request.IssueDetailsModel.CoBrand!.Company.ClubName!;
            }


            newRequest.Parameters.IsVIP = request.Customer.IsVIP ? "1" : "0";

            newRequest.Parameters.IssuePinMailer = request.PinMailer ? "1" : "0";

            //This validation for supplementary card request
            if (string.IsNullOrEmpty(newRequest.Parameters.Employment))
                newRequest.Parameters.Employment = request.Customer.IsRetiredEmployee ? "1" : "0";

            //This validation for supplementary card request
            if (string.IsNullOrEmpty(newRequest.Parameters.CustomerClassCode))
                newRequest.Parameters.CustomerClassCode = request.Customer.CustomerClassCode;

            if (string.IsNullOrEmpty(newRequest.Parameters.PCTFlag))
                newRequest.Parameters.PCTFlag = GetDefaultPCT(newRequest!.Parameters.KFHStaffID);

            if (request.IssueDetailsModel.Collateral is Collateral.AGAINST_SALARY)
            {
                newRequest.Parameters.T3MaxLimit = request.IssueDetailsModel.Card.T3MaxLimit.ToString();
                newRequest.Parameters.T12MaxLimit = request.IssueDetailsModel.Card.T12MaxLimit.ToString();
                newRequest.Parameters.CINET_ID = request.Customer.CinetId.ToString();
                newRequest.Parameters.CINET = request.Customer.TotalCinet.ToString();

                bool isUSDCard = cardDefinition!.Extension!.Currency == ConfigurationBase.USDollerCurrency;
                if (isUSDCard)
                {
                    newRequest.Parameters.KDGuaranteeSalaryAccountForUSDCard = request.IssueDetailsModel.Card.CollateralAccountNumber;
                }
            }

            if (request.IsWithPrimaryCard)
            {
                newRequest.Parameters.IsSupplementaryOrPrimaryChargeCard = request.SupplementaryModel.AnyWithNull() ? ChargeCardType.P.ToString() : ChargeCardType.N.ToString();
            }

            newRequest.Parameters.CardType = cardDefinition?.Eligibility?.ProductType switch
            {
                ProductTypes.PrePaid => "PrePaid",
                ProductTypes.ChargeCard => request.IsSupplementaryRequest ? "SupplementaryChargeCard" : "ChargeCard",
                ProductTypes.Tayseer => "T3/T12",
                _ => ""
            };
        }
        #endregion
    }


    private async Task ValidateIssueNewCardRequest(CardIssueRequest request, CardDefinitionDto cardDefinition, List<ValidationError> validationErrors)
    {
        if (await IsPendingOrActiveCard())
            throw new ApiException(message: "Cannot issue the same card again!");

        CardCurrencyDto cardCurrency = await _currencyAppService.GetCardCurrency(request.IssueDetailsModel.Card.ProductId);

        //validating sufficient fund if the card is foreign currency
        await ValidateDebitAccountForForeignCurrencyCard();
        await CoBrandValidations();
        await ChargeCardValidations();
        validationErrors.ThrowErrorsIfAny();

        #region local methods

        async Task ChargeCardValidations()
        {
            if (cardDefinition?.Eligibility?.ProductType != ProductTypes.ChargeCard)
                return;

            //uncomment the below validation if we need server side validation
            //await ValidateRequiredLimit(); 

            //No validation for supplementary card
            if (request.SupplementaryModel?.Count != 1)
                if (request.IssueDetailsModel.Collateral == Collateral.AGAINST_SALARY)
                {
                    await ValidateCINET();
                    await VerifySalary();
                }
        }

        async Task ValidateCINET()
        {

            if (request.Customer.CinetId is null || request.Customer.CinetId <= 0)
                validationErrors.Add(new(nameof(request.Customer.CinetId), "You must enter valid Cinet ID."));

            if (request.Customer.TotalCinet is null || request.Customer.TotalCinet <= 0)
                validationErrors.Add(new(nameof(request.Customer.TotalCinet), "You must enter valid Total Cinet."));

            await Task.CompletedTask;
        }
        async Task VerifySalary()
        {
            string accountNumber = request?.IssueDetailsModel?.Card.DebitAccountNumber!;
            //for Salary verification, if the card currency is USD then take the Salary debit account number
            if (cardDefinition!.Extension?.Currency == ConfigurationBase.USDollerCurrency)
            {
                accountNumber = request?.IssueDetailsModel?.Card.CollateralAccountNumber!;

                if (string.IsNullOrEmpty(accountNumber))
                    validationErrors.Add(new ValidationError(nameof(request.IssueDetailsModel.Card.CollateralAccountNumber), "You must choose salary account"));
            }

            if (string.IsNullOrEmpty(accountNumber)) return;


            var verifySalaryResponse = await _accountsAppService.VerifySalary(accountNumber, civilId: request.Customer.CivilId);

            if (!verifySalaryResponse.IsSuccess)
                validationErrors.Add(new ValidationError(nameof(request.Customer.Salary), verifySalaryResponse.Message));

            if (!verifySalaryResponse.Data!.Verified)
                validationErrors.Add(new ValidationError(nameof(request.Customer.Salary), verifySalaryResponse.Data?.Description ?? ""));


            //if the salary (from phenix) greater than the verified salary then we need to take verified one
            request.Customer.Salary = request.Customer.Salary > verifySalaryResponse.Data?.Salary ? verifySalaryResponse.Data.Salary : request.Customer.Salary;
        }


        async Task CoBrandValidations()
        {
            if (request!.IssueDetailsModel!.CoBrand is null) return;

            if (request.IssueDetailsModel.CoBrand?.Company.CardType <= 0)
                validationErrors.AddAndThrow(new(nameof(request.IssueDetailsModel.CoBrand.Company.CardType), "Invalid Co-Brand Product!"));


            request.IssueDetailsModel.Card.ProductId = request.IssueDetailsModel.CoBrand!.Company.CardType;

            var memberShipConflicts = await _memberShipAppService.GetMemberShipIdConflicts(request.Customer.CivilId, request.IssueDetailsModel.CoBrand.Company.CompanyId, request.IssueDetailsModel.CoBrand.MemberShipId.ToString() ?? "");

            if (memberShipConflicts.IsSuccess && memberShipConflicts.Data!.Any())
            {
                validationErrors.AddAndThrow(new(nameof(request.IssueDetailsModel.CoBrand.MemberShipId), GlobalResources.DuplicateMemberShipID));
            }
        }

        //Use this method if we need server side validation
        //async Task ValidateRequiredLimit()
        //{
        //    using IssueDetailsModel model = request.IssueDetailsModel;
        //    StringBuilder message = new();

        //    if (!await ValidateLimitInput())
        //    {
        //        validationErrors.Add(new ValidationError(nameof(model.Card.RequiredLimit), message.ToString()));
        //        return;
        //    }

        //    if (!await ValidateWithFinancialPosition())
        //    {
        //        validationErrors.Add(new ValidationError(nameof(model.Card.RequiredLimit), message.ToString()));
        //        return;
        //    }

        //    #region  Local Methods
        //    async Task<bool> ValidateWithFinancialPosition()
        //    {
        //        string accountNumber = request?.IssueDetailsModel?.Card.DebitAccountNumber!;

        //        //for Salary verification, if the card currency is USD then take the Salary debit account number
        //        if (cardCurrency.CurrencyIsoCode == ConfigurationBase.USDollerCurrency)
        //        {
        //            accountNumber = request?.IssueDetailsModel?.Card.CollateralAccountNumber!;

        //            if (string.IsNullOrEmpty(accountNumber))
        //                message.AppendLine("You must choose salary account");

        //            return false;
        //        }

        //        var financialPositionResponse = await _accountsAppService.GetFinancialPosition(request!.Customer.CivilId, Collateral.AGAINST_SALARY, accountNumber);
        //        if (financialPositionResponse.Data is null)
        //        {
        //            message.AppendLine("Unable to check financial position");
        //            return false;
        //        }
        //        message.Clear();

        //        FinancialPosition financialPosition = new(financialPositionResponse.Data, request, cardCurrency, null, null, cardDefinition);
        //        var isValid = await financialPosition.ValidateRequiredLimit();

        //        message = isValid.message;
        //        return message.Length == 0;
        //    }

        //    async Task<bool> ValidateLimitInput()
        //    {
        //        bool isNotInRange = (model.Card.RequiredLimit < cardDefinition.MinLimit || model.Card.RequiredLimit > cardDefinition.MaxLimit);
        //        bool isNotRounded = (model.Card.RequiredLimit % 10) != 0;

        //        string limitMessage = $" between card limit range minimum {cardDefinition.MinLimit} to maximum {cardDefinition.MaxLimit}";
        //        message.Clear();

        //        if ((model.Card.RequiredLimit <= 0) || isNotInRange)
        //            message.AppendLine($"You should enter value, {limitMessage}");

        //        if (isNotRounded)
        //            message.AppendLine($"You should enter rounded value, {limitMessage}");

        //        return message.Length == 0;
        //    }
        //    #endregion

        //    await Task.CompletedTask;
        //}

        async Task ValidateDebitAccountForForeignCurrencyCard()
        {
            if (!cardCurrency.IsForeignCurrency)
                return;

            request.IssueDetailsModel.Card.IsForeignCurrencyCard = cardCurrency.IsForeignCurrency;

            if (string.IsNullOrEmpty(request.IssueDetailsModel.Card.DebitAccountNumber))
                return;


            var validateSufficient = await _currencyAppService.ValidateSufficientFundForForeignCurrencyCards(request.IssueDetailsModel.Card.ProductId, request.IssueDetailsModel.Card.DebitAccountNumber);
            if (!validateSufficient.IsSuccess)
                validationErrors.AddRange(validateSufficient.ValidationErrors);
        }
        async Task<bool> IsPendingOrActiveCard()
        {
            var statuses = _fdrDBContext.Requests.AsNoTracking().Where(x => x.CivilId == request.Customer.CivilId && x.CardType == request.IssueDetailsModel.Card.ProductId && x.ReqStatus != (int)CreditCardStatus.Closed)
              .Select(x => x.ReqStatus);

            var isDuplicateOrActive = statuses.Any(status => status == (int)CreditCardStatus.Pending || status == (int)CreditCardStatus.Active || status == (int)CreditCardStatus.Approved);

            return await Task.FromResult(isDuplicateOrActive);
        }
        #endregion
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="allProducts"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    private async Task<List<CardEligiblityMatrixDto>> CardEligibilityFilter(IQueryable<CardEligiblityMatrixDto> allProducts, EligibleCardRequest data)
    {
        var eligibleCardTypes = new List<CardEligiblityMatrixDto>();


        Task<List<NonClosedCards>> nonClosedExistingCardsTask = (from request in _fdrDBContext.Requests.AsNoTracking().Where(x => x.CardType != 0 && x.CivilId == data.CivilId && x.ReqStatus != (int)CreditCardStatus.Closed)
                                                                 group request by request.CardType into grouping
                                                                 select new NonClosedCards(grouping.Key, grouping.Select(x => x.ReqStatus))).ToListAsync();

        var debitAccountsTask = _accountsAppService.GetDebitAccounts(data.CivilId!);
        List<Task> tasks = [nonClosedExistingCardsTask, debitAccountsTask];
        await Task.WhenAll(tasks);

        var nonClosedExistingCards = await nonClosedExistingCardsTask;

        var filteredProducts = GetEligibleCardTypes();

        tasks = [RemoveCardsForNonKFHCustomer(),
            RemoveEliminatedCards(),
            FilterByBranch(),
            FilterByCustomerRIMCode(),
            RemoveAlOsraForCorporateCustomer(),
            FilterAlOsraPrimaryAndSupplementary(),
            FilterByAgeLimit(),
            BuildProductDisplayName()];

        await Task.WhenAll(tasks);

        return filteredProducts.ToList();

        #region local methods
        async Task RemoveCardsForNonKFHCustomer()
        {
            var debitAccounts = (await debitAccountsTask)?.Data;

            if (debitAccounts is null || debitAccounts.Count == 0)
            {
                var allowedNonKFHProducts = _fdrDBContext.CardtypeEligibilityMatixes.AsNoTracking().Where(eligible => eligible.AllowedNonKfh == true).Select(card => card.CardType);

                filteredProducts = filteredProducts.Where(product => allowedNonKFHProducts.Any(allowedNonKFHProduct_id => allowedNonKFHProduct_id == product.ProductID));
            }
        }

        IQueryable<CardEligiblityMatrixDto> GetEligibleCardTypes()
        {
            var eligibleCardTypes = new List<CardEligiblityMatrixDto>();
            foreach (var item in allProducts)
            {
                var nonClosedExistingCard = nonClosedExistingCards.FirstOrDefault(x => x.ProductID == item.ProductID);

                if (nonClosedExistingCard != null)
                {
                    if (nonClosedExistingCard.Statuses.Any(x => x == (int)CreditCardStatus.Lost))
                        if (!nonClosedExistingCard.Statuses.Any(x => x is (int)CreditCardStatus.Active or (int)CreditCardStatus.TemporaryClosed))
                            eligibleCardTypes.Add(item);

                    continue;
                }

                eligibleCardTypes.Add(item);
            }
            return eligibleCardTypes.AsQueryable();
        }

        async Task FilterAlOsraPrimaryAndSupplementary()
        {
            bool isPrimaryActive = nonClosedExistingCards.Any(x => x.ProductID == ConfigurationBase.AlOsraPrimaryCardTypeId && x.Statuses.Any(x => x == (int)CreditCardStatus.Active));
            bool isSupplementaryActive = nonClosedExistingCards.Any(x => x.ProductID == ConfigurationBase.AlOsraSupplementaryCardTypeId && x.Statuses.Any(x => x == (int)CreditCardStatus.Active));

            if (!isPrimaryActive)
                filteredProducts = filteredProducts.Where(x => x.ProductID != ConfigurationBase.AlOsraSupplementaryCardTypeId);
            else if (isSupplementaryActive)
                filteredProducts = filteredProducts.Where(x => x.ProductID == ConfigurationBase.AlOsraPrimaryCardTypeId);

        }

        async Task FilterByAgeLimit()
        {
            if (data?.DateOfBirth == null) return;

            var age = Math.Floor(DateTime.Today.Subtract(data?.DateOfBirth ?? DateTime.MinValue).TotalDays / 365);
            filteredProducts = filteredProducts.Where(x => x.AgeMinimumLimit == 0 || x.AgeMinimumLimit > 0 && age >= x.AgeMinimumLimit);
            filteredProducts = filteredProducts.Where(x => x.AgeMaximumLimit == 0 || x.AgeMaximumLimit > 0 && age <= x.AgeMaximumLimit);
        }

        async Task RemoveAlOsraForCorporateCustomer()
        {
            bool isCorporateCustomer = data?.CustomerType.Trim().ToUpper() != ConfigurationBase.Personal;
            if (isCorporateCustomer)
                filteredProducts = filteredProducts.Where(f => f.ProductID != ConfigurationBase.AlOsraPrimaryCardTypeId && f.ProductID != ConfigurationBase.AlOsraSupplementaryCardTypeId);
        }

        async Task FilterByCustomerRIMCode()
        {
            int[] exceptionalProductIds = Array.ConvertAll(ConfigurationBase.ExceptionalProductIdsForCustomerClassFilter.Split(','), s => int.Parse(s));
            filteredProducts = filteredProducts.Where(x => x.AllowedClassCodes.Any(ac => ac.Equals(data.RimCode) && exceptionalProductIds.Any(ep => x.ProductID != ep) || ac.Equals("0")));

        }

        async Task BuildProductDisplayName()
        {
            foreach (var product in filteredProducts.Where(x => x.AgeMinimumLimit > 0 || x.AgeMaximumLimit > 0))
            {
                var cardDisplayName = $"{product.ProductName} - Age ";

                if (product.AgeMinimumLimit > 0 && product.AgeMaximumLimit > 0)
                    cardDisplayName += $"{product.AgeMinimumLimit} - {product.AgeMaximumLimit}";
                else
                    cardDisplayName += product.AgeMinimumLimit > 0 ? $"{product.AgeMinimumLimit} +" : $"{product.AgeMaximumLimit} -";

                product.ProductName = cardDisplayName;
            }
        }
        async Task FilterByBranch()
        {
            var branch = await configurationAppService.GetUserBranch(data.KfhId);
            if (branch.BranchId != 0)
                filteredProducts = filteredProducts.Where(x => x.AllowedBranches.Any(br => br.Equals(branch.BranchId.ToString("0#")) || br.Equals("0")));
        }
        async Task RemoveEliminatedCards()
        {
            int[] eliminatedCards = Array.ConvertAll(ConfigurationBase.EliminatedPlatinumCards.Split(','), s => int.Parse(s));
            if (eliminatedCards.Length != 0)
                filteredProducts = filteredProducts.Where(x => eliminatedCards.Any(extype => extype != x.ProductID));
        }
        #endregion
    }

    private record NonClosedCards(int ProductID, IEnumerable<int> Statuses);

    #endregion
}



