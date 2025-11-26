using CorporateCreditCardServiceReference;
using CreditCardsSystem.Data;
using CreditCardsSystem.Data.Models;
using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Models.Access;
using CreditCardsSystem.Domain.Models.Card;
using CreditCardsSystem.Domain.Models.CardIssuance;
using CreditCardsSystem.Domain.Models.CardOperation;
using CreditCardsSystem.Domain.Models.Corporate;
using CreditCardsSystem.Domain.Models.Workflow;
using CreditCardsSystem.Domain.Shared.Models.Reports;
using CreditCardsSystem.Domain.Shared.Models.RequestActivity;
using CreditCardsSystem.Utility.Extensions;
using CreditCardTransactionInquiryServiceReference;
using CreditCardUpdateServiceReference;
using Kfh.Aurora.Auth;
using Kfh.Aurora.Common.Shared.Interfaces.Customer;
using Kfh.Aurora.Integration;
using Kfh.Aurora.Logging;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.ServiceModel;

namespace CreditCardsSystem.Application.CardOperations;

public class ChangeLimitAppService(
    IIntegrationUtility integrationUtility,
    IOptions<IntegrationOptions> options,
    FdrDBContext fdrDBContext,
    ICardDetailsAppService cardDetailsAppService,
    IRequestActivityAppService requestActivityAppService,
    IEmployeeAppService employeeAppService,
    IAuditLogger<ChangeLimitAppService> auditLogger,
    IRequestAppService requestAppService,
    ICorporateProfileAppService corporateProfileService,
    IAuthManager authManager,
    ICustomerProfileCommonApi customerProfileCommonApi) : BaseApiResponse, IChangeLimitAppService, IAppService
{
    #region Private Fields

    private readonly CreditCardInquiryServicesServiceClient _creditCardInquiryServiceClient = integrationUtility.GetClient<CreditCardInquiryServicesServiceClient>(options.Value.Client, options.Value.Endpoints.CreditCardTransactionInquiry, options.Value.BypassSslValidation);
    private readonly CreditCardUpdateServicesServiceClient _creditCardUpdateServiceClient = integrationUtility.GetClient<CreditCardUpdateServicesServiceClient>(options.Value.Client, options.Value.Endpoints.CreditCardUpdate, options.Value.BypassSslValidation);
    private readonly CorporateCreditCardServiceClient _corporateCreditCardServiceClient = integrationUtility.GetClient<CorporateCreditCardServiceClient>(options.Value.Client, options.Value.Endpoints.CorporateCreditCard, options.Value.BypassSslValidation);
    private readonly FdrDBContext _fdrDBContext = fdrDBContext;
    private readonly ICardDetailsAppService _cardDetailsAppService = cardDetailsAppService;
    private readonly IEmployeeAppService _employeeAppService = employeeAppService;
    private readonly IRequestActivityAppService _requestActivityAppService = requestActivityAppService;
    private readonly IRequestAppService _requestAppService = requestAppService;
    private readonly IAuditLogger<ChangeLimitAppService> _auditLogger = auditLogger;
    private readonly ICorporateProfileAppService _corporateProfileService = corporateProfileService;
    private readonly IAuthManager _authManager = authManager;
    private readonly ICustomerProfileCommonApi _customerProfileCommonApi = customerProfileCommonApi;

    #endregion

    #region public methods 
    [HttpGet]
    public async Task<ApiResponseModel<List<ChangeLimitHistoryDto>>> GetChangeLimitHistory(decimal requestId)
    {
        string requestIdStr = requestId.ToString();

        var histories = await _fdrDBContext.ChangeLimitHistories.Where(x => x.ReqId == requestIdStr)
            .OrderBy(x => x.Id)
            .ProjectToType<ChangeLimitHistoryDto>().ToListAsync();

        return Success(histories);
    }

    [HttpPost]
    public async Task<ApiResponseModel<ProcessResponse>> RequestChangeLimit([FromBody] ChangeLimitRequest request)
    {

        await ValidateBiometricStatus(request.RequestId);

        if (!_authManager.HasPermission(Permissions.ChangeLimit.Request()))
            return Failure<ProcessResponse>(GlobalResources.NotAuthorized);

        var response = new ApiResponseModel<ProcessResponse>();
        CorporateProfileDto corporateProfile = new();
        CardDetailsResponse Card = new();
        Collateral collateral = Collateral.NONE;

        await request.ModelValidationAsync(nameof(ChangeLimitRequest));
        await ValidateRequest();


        _auditLogger.Log.Information("Change limit: Requesting change limit for requestId:{requestid}", request.RequestId);

        var Id = (await _fdrDBContext.Database.SqlQueryRaw<long>("select FDR.CHANGE_LIMIT_HISTORY_SEQ.nextval from Dual").ToListAsync()).FirstOrDefault();

        using var transaction = await _fdrDBContext.Database.BeginTransactionAsync();
        try
        {
            await _fdrDBContext.ChangeLimitHistories.AddAsync(new()
            {
                Id = Id,
                ReqId = request.RequestId.ToString(),
                OldLimit = Card.ApproveLimit,
                NewLimit = request.NewLimit,
                Status = ChangeLimitStatus.PENDING.ToString(),
                LogDate = DateTime.Now,
                IsTempLimitChange = request.IsTemporary ? "1" : "0",
                InitiatorId = _authManager.GetUser()?.KfhId ?? "",
                RefuseReason = "",
                UserComments = request.Comments,
                PurgeDays = request.PurgeDays,
                MarginAccount = collateral == Collateral.AGAINST_MARGIN ? request.CardAccount : null,
                DepositNumber = collateral == Collateral.AGAINST_DEPOSIT ? request.SelectedHold?.HoldId : null,
                DepositAccount = collateral == Collateral.AGAINST_DEPOSIT ? request.CardAccount : null,
                KfhSalary = request.KFHSalary,
            });

            await _fdrDBContext.SaveChangesAsync();

            await UpdatedChangeLimitRequestParameters();

            if (Card.ProductType == ProductTypes.Tayseer && collateral is not (Collateral.AGAINST_MARGIN or Collateral.AGAINST_DEPOSIT))
                await InsertTayseerCreditCheck(request, Card);

            //Print After SalesForm

            await _fdrDBContext.SaveChangesAsync();

            await transaction.CommitAsync();

            await LogRequestActivity();

            _auditLogger.Log.Information("Change limit: Successfully created change limit request for requestId:{requestid}", request.RequestId);

            return Success<ProcessResponse>(new(), message: GlobalResources.WaitingForApproval);
        }
        catch (System.Exception ex)
        {
            _auditLogger.Log.Error(ex, "Change limit: Failed change limit request for requestId:{requestid}", _authManager.GetUser()?.Name, request.RequestId);
            await transaction.RollbackAsync();
            throw;
        }

        async Task InsertTayseerCreditCheck(ChangeLimitRequest request, CardDetailsResponse card)
        {
            var newId = _fdrDBContext.TayseerCreditCheckings.Max(x => x.Id) + 1;
            await _fdrDBContext.TayseerCreditCheckings.AddAsync(new()
            {
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Convert.ToDecimal(_authManager.GetUser()?.KfhId ?? "0"),
                Id = newId,
                RequestId = request.RequestId,
                CapsDate = request.CapsDate,
                CapsType = Convert.ToBoolean(request.CapsType),
                CinetInstallment = request.CinetInstallment,
                CinetSalary = request.CinetSalary,
                CreditCardNumber = card.CardNumber,
                EntryType = 2, // Haitham: means change limit, in approval i will rely on this
                IsRetiree = request.IsRetiree,
                IsThereAguarantor = request.IsGuarantor,
                KfhSalary = request.KFHSalary,
                //OtherBankCreditLimit = request.OtherBankCreditLimit,
                NewLimit = request.NewLimit,
                IsInDelinquentList = request.InDelinquent,
                IsInKfhBlackList = request.InKFHBlackList,
                IsInCinetBalckList = Convert.ToDecimal(request.InCinetBlackList),
                IsThereAnException = request.IsException,
                ExceptionDescription = request.Remarks,
                Status = 3 // Pending, we will rely on this and entry type
            });

            await _fdrDBContext.SaveChangesAsync();
        }
        async Task LogRequestActivity()
        {
            var requestActivityDto = new RequestActivityDto()
            {
                CivilId = Card.CivilId,
                RequestActivityStatusId = (int)(request.NewLimit < Card.ApproveLimit ? RequestActivityStatus.Approved : RequestActivityStatus.Pending),
                IssuanceTypeId = (int)Card.IssuanceType,
                RequestId = Card.RequestId,
                CfuActivityId = (int)CFUActivity.LIMIT_CHANGE_INCR,
                Details = new()
        {
            { ReportingConstants.KEY_LIMIT_CHANGE_TYPE, request.IsTemporary?"Temporary":"Permanent" },
            { ReportingConstants.KEY_Current_Limit, Card.ApproveLimit.ToString()},
            { ReportingConstants.KEY_New_Limit, request.NewLimit.ToString()},
            { ReportingConstants.KEY_Purge_Days, request.PurgeDays.ToString()},
            { ReportingConstants.KEY_User_Comments, request.Comments}
        }
            };

            if (Card.ProductType == ProductTypes.Tayseer)
            {

                requestActivityDto.Details.Add(ReportingConstants.KEY_KFH_Salary, request.KFHSalary.ToString());
                requestActivityDto.Details.Add(ReportingConstants.KEY_Is_Retiree, request.IsRetiree ? "1" : "0");
                requestActivityDto.Details.Add(ReportingConstants.KEY_Is_Guarantor, request.IsRetiree ? "1" : "0");
                requestActivityDto.Details.Add(ReportingConstants.KEY_Cinet_Salary, request.CinetSalary.ToString());
                requestActivityDto.Details.Add(ReportingConstants.KEY_Cinet_Installment, request.CinetInstallment.ToString());
                //requestActivityDto.Details.Add(ReportingConstants.KEY_Other_Bank_Limit, request.OtherBankCreditLimit.ToString());
                requestActivityDto.Details.Add(ReportingConstants.KEY_Caps_Type, request.CapsType.ToString());

                if (request.CapsDate is not null)
                    requestActivityDto.Details.Add(ReportingConstants.KEY_Caps_Date, request.CapsDate?.Formed() ?? "");

                requestActivityDto.Details.Add(ReportingConstants.KEY_In_Delinquent_List, request.InDelinquent ? "1" : "0");
                requestActivityDto.Details.Add(ReportingConstants.KEY_In_Black_List, request.InKFHBlackList ? "1" : "0");
                requestActivityDto.Details.Add(ReportingConstants.KEY_In_Cinet_Black_List, request.InCinetBlackList ? "1" : "0");
                requestActivityDto.Details.Add(ReportingConstants.KEY_Exception, request.IsException ? "1" : "0");
                requestActivityDto.Details.Add(ReportingConstants.KEY_REMARKS, request.Remarks.ToString());

            }

            if (collateral is Collateral.AGAINST_DEPOSIT or Collateral.AGAINST_DEPOSIT_USD)
            {
                requestActivityDto.Details.Add(ReportingConstants.DEPOSIT_ACCOUNT_NO, request.CardAccount.ToString());
                requestActivityDto.Details.Add(ReportingConstants.DEPOSIT_AMOUNT, request.SelectedHold?.Amount.ToString() ?? "");
            }


            if (collateral is Collateral.AGAINST_MARGIN)
            {
                requestActivityDto.Details.Add(ReportingConstants.MARGIN_ACCOUNT_NO, request.CardAccount.ToString());
                requestActivityDto.Details.Add(ReportingConstants.MARGIN_AMOUNT, request.MarginAmount?.ToString() ?? "");
            }



            #region workflowwariables
            requestActivityDto.WorkflowVariables = new() {
                { "Description", $"Request change limit for {Card.CardNumber.Masked(6,6).SplitByFour()}" },
                { WorkflowVariables.IsTemporary, request.IsTemporary?"Temporary":"Permanent" },
                { WorkflowVariables.ApprovedLimit, Card.ApproveLimit.ToString()},
                { WorkflowVariables.NewLimit, request.NewLimit.ToString()},
                { WorkflowVariables.PurgeDays, request.PurgeDays.ToString()},
                { WorkflowVariables.UserComments, request.Comments},
                { WorkflowVariables.CardNumber, Card.CardNumberDto??""}
                };
            if (Card.ProductType == ProductTypes.Tayseer)
            {

                requestActivityDto.WorkflowVariables.Add(WorkflowVariables.KFHSalary, request.KFHSalary.ToString());
                requestActivityDto.WorkflowVariables.Add(WorkflowVariables.IsRetired, request.IsRetiree.ToText());
                requestActivityDto.WorkflowVariables.Add(WorkflowVariables.IsGuarantor, request.IsRetiree.ToText());
                requestActivityDto.WorkflowVariables.Add(WorkflowVariables.CinetSalary, request.CinetSalary.ToString());
                requestActivityDto.WorkflowVariables.Add(WorkflowVariables.CinetInstallment, request.CinetInstallment.ToString());
                //requestActivityDto.WorkflowVariables.Add(WorkflowVariables.OtherBankLimit, request.OtherBankCreditLimit.ToString());
                requestActivityDto.WorkflowVariables.Add(WorkflowVariables.CapsType, request.CapsType.ToString());

                if (request.CapsDate is not null)
                    requestActivityDto.WorkflowVariables.Add(WorkflowVariables.CapsDate, request.CapsDate?.Formed() ?? "");

                requestActivityDto.WorkflowVariables.Add(WorkflowVariables.InDelinquentList, request.InDelinquent.ToText());
                requestActivityDto.WorkflowVariables.Add(WorkflowVariables.InBlackList, request.InKFHBlackList.ToText());
                requestActivityDto.WorkflowVariables.Add(WorkflowVariables.InCinetBlackList, request.InCinetBlackList.ToText());
                requestActivityDto.WorkflowVariables.Add(WorkflowVariables.Exception, request.IsException.ToText());
                requestActivityDto.WorkflowVariables.Add(WorkflowVariables.Remarks, request.Remarks.ToString());

            }
            if (collateral is Collateral.AGAINST_DEPOSIT or Collateral.AGAINST_DEPOSIT_USD)
            {
                requestActivityDto.WorkflowVariables.Add(WorkflowVariables.DepositAccountNumber, request.CardAccount.ToString());
                requestActivityDto.WorkflowVariables.Add(WorkflowVariables.DepositAmount, request.SelectedHold?.Amount.ToString() ?? "");
            }
            if (collateral is Collateral.AGAINST_MARGIN)
            {
                requestActivityDto.WorkflowVariables.Add(WorkflowVariables.MarginAccountNumber, request.CardAccount.ToString());
                requestActivityDto.WorkflowVariables.Add(WorkflowVariables.MarginAmount, request.MarginAmount?.ToString() ?? "");
            }
            #endregion


            await _requestActivityAppService.LogRequestActivity(requestActivityDto, isNeedWorkflow: true);
            //TODO Log
        }

        async Task ValidateRequest()
        {

            await CheckingPendingRequest(request.RequestId);

            var cardInfo = await _cardDetailsAppService.GetCardInfo(request.RequestId, includeCardBalance: true);

            if (!cardInfo.IsSuccessWithData)
                throw new ApiException(message: cardInfo.Message);

            Card = cardInfo.Data!;

            if (Enum.TryParse(cardInfo.Data!.Parameters!.Collateral, out Collateral _collateral))
                collateral = _collateral;

            await IsValidCorporateCard();

            await IsValidNewLimit();


            if (Card.ProductType == ProductTypes.ChargeCard)
                await IsValidChargeCard();

            if (Card.ProductType == ProductTypes.Tayseer)
                await IsValidTayseerCard();


            if (Card.IsPrimaryCard)
                await ValidateNewLimitWithSupplementary(request.RequestId, request.NewLimit);

            if (Card.IsSupplementaryCard)
                await ValidateNewLimitWithParentLimitHistory();

            if (response.ValidationErrors.Any())
                throw new ApiException(errors: response.ValidationErrors);


            async Task CheckingPendingRequest(decimal requestId)
            {
                var chageHistory = await GetChangeLimitHistory(requestId);
                if (chageHistory.Data?.Any(x => x.Status == ChangeLimitStatus.PENDING.ToString()) ?? false)
                    throw new ApiException(message: GlobalResources.NotAllowedToAddLimitChangeRequest);
            }

            async Task IsValidNewLimit()
            {
                //if ((request.KFHSalary <= 0 && (collateral is Collateral.AGAINST_SALARY)) || request.KFHSalary < 0)
                //    response.ValidationErrors.Add(new(nameof(request.KFHSalary), GlobalResources.EnterKfhSalary));

                if (request.NewLimit == 0)
                    response.ValidationErrors.Add(new(nameof(request.NewLimit), GlobalResources.NewLimitIsWrong));

                if (request.NewLimit > Card.ApproveLimit)
                {
                    if (Card.CardStatus != CreditCardStatus.Active)
                        response.ValidationErrors.Add(new(nameof(request.NewLimit), GlobalResources.LimitIncreaseInActiveCardsBlock));

                    if (request.InDelinquent)
                        response.ValidationErrors.Add(new(nameof(request.NewLimit), GlobalResources.LimitIncreaseDelCreditCards));
                }

                await Task.CompletedTask;

                if (response.ValidationErrors.Any())
                    throw new ApiException(errors: response.ValidationErrors, message: "Validation failed!");
            }

            async Task IsValidChargeCard()
            {
                if (request.IsTemporary && request.PurgeDays == 0)
                    response.ValidationErrors.Add(new(nameof(request.PurgeDays), GlobalResources.PurgeDaysAreRequired));

                if (!request.IsTemporary && request.PurgeDays != 0)
                    response.ValidationErrors.Add(new(nameof(request.PurgeDays), GlobalResources.PurgeDaysShouldNotBeSelected));

                if (request.IsTemporary && request.NewLimit > Card.ApproveLimit && (collateral is Collateral.AGAINST_MARGIN or Collateral.AGAINST_DEPOSIT))
                    response.ValidationErrors.Add(new(nameof(request.NewLimit), GlobalResources.TempLimitDecreaseChargeCards));

                if (request.IsTemporary && request.NewLimit > Card.ApproveLimit && (collateral is Collateral.AGAINST_CORPORATE_CARD))
                    response.ValidationErrors.Add(new(nameof(request.NewLimit), GlobalResources.TempLimitCorporatreChargeCards));

                bool isHavingCollateral = collateral is Collateral.AGAINST_DEPOSIT or Collateral.AGAINST_DEPOSIT_USD or Collateral.AGAINST_MARGIN;

                if (isHavingCollateral && string.IsNullOrEmpty(request.CardAccount))
                    response.ValidationErrors.Add(new(nameof(request.CardAccount), GlobalResources.RequiredCardAccount));

                await ValidateHoldAmount();


                if (response.ValidationErrors.Any())
                    throw new ApiException(errors: response.ValidationErrors);

                async Task ValidateHoldAmount()
                {
                    if (collateral is Collateral.AGAINST_MARGIN && string.IsNullOrEmpty(request.CardAccount))
                        response.ValidationErrors.Add(new(nameof(request.NewLimit), GlobalResources.NoMarginAccountSelected));

                    if (collateral is Collateral.AGAINST_DEPOSIT && string.IsNullOrEmpty(request.CardAccount))
                        response.ValidationErrors.Add(new(nameof(request.NewLimit), GlobalResources.NoHoldSelected));

                    decimal HoldAmount = Convert.ToDecimal(request.SelectedHold?.Amount ?? 0);

                    if (request.NewLimit > HoldAmount && collateral is Collateral.AGAINST_MARGIN)
                        response.ValidationErrors.Add(new(nameof(request.NewLimit), GlobalResources.LimitMarginBalance));

                    if (request.NewLimit > HoldAmount && collateral is Collateral.AGAINST_DEPOSIT)
                        response.ValidationErrors.Add(new(nameof(request.NewLimit), GlobalResources.LimitHoldAmount));


                    if (collateral is Collateral.AGAINST_CORPORATE_CARD)
                    {

                        var corporateProfileResult = await _corporateProfileService.GetProfile(Card.CivilId!);
                        if (!corporateProfileResult.IsSuccessWithData)
                        {
                            response.ValidationErrors.Add(new(nameof(request.NewLimit), corporateProfileResult.Message));
                            throw new ApiException(errors: response.ValidationErrors);
                        }



                        corporateProfile = corporateProfileResult.Data!;

                        if ((request.NewLimit - Card.ApproveLimit) > corporateProfile.AvailableLimit)
                            response.ValidationErrors.Add(new(nameof(request.NewLimit), GlobalResources.LimitIncreaseCorporate));
                    }

                }

            }

            async Task IsValidTayseerCard()
            {
                if (Card.ProductType != ProductTypes.Tayseer) return;


                var cardDefinition = await _cardDetailsAppService.GetCardWithExtension(Card.CardType);


                if (request.NewLimit < cardDefinition.MinLimit || request.NewLimit > cardDefinition.MaxLimit)
                { // Tayseer Temp limit change: more than credit limit is not approved, less than credit limit is not approved
                  // Tayseer Permanent limit change: more than limit is not approved and less than limit is approved

                    // in temporary limit change the new limit should be in card limit range
                    if (request.IsTemporary)
                        throw new ApiException(errors: new List<ValidationError>() { new(nameof(request.NewLimit), GlobalResources.NewLimitShouldBeInTayseerCardLimitRange) });

                }

                if (collateral is Collateral.AGAINST_DEPOSIT or Collateral.AGAINST_MARGIN)
                    return;

                if (request.KFHSalary <= 0)
                    response.ValidationErrors.Add(new(nameof(request.KFHSalary), GlobalResources.EnterKfhSalary));




                bool EnableTayseerCreditChecking = collateral is Collateral.AGAINST_SALARY or Collateral.EXCEPTION;

                if (EnableTayseerCreditChecking)
                {
                    if (request.CinetSalary <= 0)
                        response.ValidationErrors.Add(new(nameof(request.CinetSalary), GlobalResources.EnterCinetSalary));

                    //if (request.CinetInstallment <= 0)
                    //    response.ValidationErrors.Add(new(nameof(request.CinetInstallment), GlobalResources.EnterCinetInstallment));

                    //if (request.OtherBankCreditLimit <= 0)
                    //    response.ValidationErrors.Add(new(nameof(request.OtherBankCreditLimit), GlobalResources.EnterOtherBankLimit));

                    if (request.CapsType != 0 && request.CapsDate == DateTime.MinValue)
                        response.ValidationErrors.Add(new(nameof(request.CapsType), GlobalResources.EnterCapsDate));
                }


                if (request.IsException && string.IsNullOrEmpty(request.Remarks))
                    response.ValidationErrors.Add(new(nameof(request.Remarks), GlobalResources.EnterExceptionDescription));

            }

            async Task IsValidCorporateCard()
            {
                if (collateral is not Collateral.AGAINST_CORPORATE_CARD) return;

                var corporateProfileResult = await _corporateProfileService.GetProfile(Card.CorporateCivilId!);
                if (!corporateProfileResult.IsSuccessWithData)
                {
                    response.ValidationErrors.Add(new(nameof(request.NewLimit), corporateProfileResult.Message));
                    throw new ApiException(errors: response.ValidationErrors);
                }

            }


            async Task ValidateNewLimitWithSupplementary(decimal requestId, decimal limitToCompare)
            {

                var supplementaries = await _cardDetailsAppService.GetSupplementaryCardsByRequestId(requestId);

                decimal supplementaryLimits = supplementaries.Data?.Sum(x => x.CardData?.ApprovedLimit) ?? 0;

                string suplLimitMoreThanPrimLimit = string.Format(GlobalResources.SuplLimitMoreThanPrimLimit, limitToCompare.ToString(), supplementaryLimits.ToString());

                if (limitToCompare < supplementaryLimits)
                    response.ValidationErrors.Add(new(nameof(request.NewLimit), Card.IsPrimaryCard ? GlobalResources.NewPrimaryofSuplLimitNotCover : suplLimitMoreThanPrimLimit));
            }

            async Task ValidateNewLimitWithParentLimitHistory()
            {
                if (!decimal.TryParse(Card.Parameters.PrimaryCardRequestId, out decimal _primaryCardRequestId))
                    throw new ApiException(message: "Missing primary card request id in request parameters");

                await CheckingPendingRequest(_primaryCardRequestId);

                var primaryCardInfo = await _cardDetailsAppService.GetCardInfo(_primaryCardRequestId);
                decimal primaryCardLimit = primaryCardInfo.Data.ApproveLimit;
                await ValidateNewLimitWithSupplementary(_primaryCardRequestId, primaryCardLimit);
            }
        }

        async Task UpdatedChangeLimitRequestParameters()
        {
            var oldRequestParameter = await _fdrDBContext.RequestParameters.FirstOrDefaultAsync(x => x.ReqId == request.RequestId && x.Parameter == "CHANGE_LIMIT_REQUEST");
            if (oldRequestParameter != null)
            {
                _fdrDBContext.RequestParameters.Remove(oldRequestParameter);
            }

            await _fdrDBContext.RequestParameters.AddAsync(new RequestParameter() { ReqId = request.RequestId, Parameter = "CHANGE_LIMIT_REQUEST", Value = "1" });

            if (collateral is not Collateral.AGAINST_CORPORATE_CARD) return;


            List<RequestParameter> requestParameters = new() {
            new() { ReqId=request.RequestId, Parameter="cmt_type" } ,
            new() { ReqId=request.RequestId, Parameter="commitment_no" } ,
            new() { ReqId=request.RequestId, Parameter="amt" } ,
            new() { ReqId=request.RequestId, Parameter="undisbursed" } ,
            new() { ReqId=request.RequestId, Parameter="mat_dt" } ,
            new() { ReqId=request.RequestId, Parameter="status" } ,
        };
            _fdrDBContext.RequestParameters.RemoveRange(requestParameters);
            var requestParameter = new RequestParameterDto()
            {
                CommitmentType = corporateProfile.GlobalLimitDto.CommitmentType,
                CommitmentNo = corporateProfile.GlobalLimitDto.CommitmentNo ?? "",
                Amount = corporateProfile.GlobalLimitDto.Amount.ToString(),
                Undisbursed = corporateProfile.GlobalLimitDto.UndisbursedAmount.ToString(),
                MaturityDate = corporateProfile.GlobalLimitDto.MaturityDate.Formed(),
                Status = corporateProfile.GlobalLimitDto.Status ?? "",

            };
            await _requestAppService.AddRequestParameters(requestParameter, request.RequestId);

            await _fdrDBContext.SaveChangesAsync();
        }
    }

    [HttpGet]
    public async Task<ApiResponseModel<ProcessResponse>> DeleteChangeLimit(decimal id)
    {
        var changeLimitReq = await _fdrDBContext.ChangeLimitHistories.FirstOrDefaultAsync(x => x.Id == id);

        if (changeLimitReq == null)
            return Failure<ProcessResponse>(message: "Invalid Id");

        var requestId = Convert.ToDecimal(changeLimitReq.ReqId);

        _auditLogger.Log.Information("Delete change limit: Requesting delete change limit for requestId:{requestid}", requestId);

        using var transaction = await _fdrDBContext.Database.BeginTransactionAsync();
        try
        {
            _fdrDBContext.Remove(changeLimitReq);

            await _fdrDBContext.SaveChangesAsync();
            var cardInfo = await _cardDetailsAppService.GetCardInfo(requestId);

            if (cardInfo.Data.ProductType == ProductTypes.Tayseer)
            {
                var changeLimitRequest = await _fdrDBContext.RequestParameters.FirstOrDefaultAsync(rp => rp.ReqId == requestId && rp.Parameter == "CHANGE_LIMIT_REQUEST");
                if (changeLimitRequest is not null)
                {
                    _fdrDBContext.RequestParameters.Remove(changeLimitRequest);
                }

                var cancelledRequest = _fdrDBContext.TayseerCreditCheckings.Where(rp => rp.RequestId == requestId && rp.Status == (int)CreditCardStatus.Cancelled);
                if (cancelledRequest.AnyWithNull())
                {
                    _fdrDBContext.TayseerCreditCheckings.RemoveRange(cancelledRequest);
                }
            }

            await _fdrDBContext.SaveChangesAsync();

            await transaction.CommitAsync();
            _auditLogger.Log.Information("Delete change limit: Success for requestId:{requestid}", requestId);
        }
        catch (System.Exception)
        {
            await transaction.RollbackAsync();
            _auditLogger.Log.Information("Delete change limit: failed for requestId:{requestid}", requestId);
            throw;
        }


        return Success<ProcessResponse>(new(), message: $"Successfully deleted change limit request for requestId:{requestId}");
    }

    [HttpPost]
    public async Task<ApiResponseModel<ProcessResponse>> CancelChangeLimit([FromBody] decimal id, ChangeLimitStatus status)
    {
        var changeLimitReq = await _fdrDBContext.ChangeLimitHistories.FindAsync(id);

        if (changeLimitReq == null)
            return Failure<ProcessResponse>(message: "Invalid Id");

        var requestId = Convert.ToDecimal(changeLimitReq.ReqId);

        using var transaction = await _fdrDBContext.Database.BeginTransactionAsync();
        try
        {
            changeLimitReq.Status = ((int)status).ToString();

            _fdrDBContext.RequestParameters.Remove(new() { ReqId = requestId, Parameter = "CANCEL_CHANGE_LIMIT_REQUEST" });
            await _fdrDBContext.SaveChangesAsync();

            await _fdrDBContext.RequestParameters.AddAsync(new() { ReqId = requestId, Parameter = "CANCEL_CHANGE_LIMIT_REQUEST", Value = "1" });
            await _fdrDBContext.SaveChangesAsync();

            await transaction.CommitAsync();
            _auditLogger.Log.Information("Cancel Change limit: Success change limit cancellation for Id:{id} and status is {status}", id, status.ToString());
        }
        catch (System.Exception)
        {
            await transaction.RollbackAsync();
            _auditLogger.Log.Information("Cancel Change limit: Failed to change status of change limit for Id:{id} and status is {status}", id, status.ToString());
            throw;
        }

        return Success<ProcessResponse>(new(), message: $"Successfully Cancelled change limit request for Id:{id} and status is {status}");
    }

    [HttpPost]
    public async Task<ApiResponseModel<ProcessResponse>> ProcessChangeLimitRequest([FromBody] ProcessChangeLimitRequest request)
    {
        var requestActivity = (await _requestActivityAppService.GetRequestActivityById(request.RequestActivityId)).Data ?? throw new ApiException(message: "Unable to find Request");
        var cardInfo = (await _cardDetailsAppService.GetCardInfo(requestActivity.RequestId))?.Data ?? throw new ApiException(message: "Invalid request Id");
        var changeLimitHistory = (await GetChangeLimitHistory(requestActivity.RequestId))?.Data;

        _auditLogger.LogWithEvent(nameof(ProcessChangeLimitRequest)).Information("Change limit: {action} for requestId:{requestid}, requestActivityId:{requestActivityId}", request.ActionType.GetDescription(), requestActivity.RequestId, request.RequestActivityId);

        bool isRequestedToCancelChangeLimit = cardInfo.Parameters.CancelChangeLimitRequest == "1";

        _ = Enum.TryParse(cardInfo!.Parameters?.Collateral, out Collateral _collateral);
        request.CardNumber = cardInfo.CardNumber.Masked(6, 6);
        request.Activity = CFUActivity.LIMIT_CHANGE_INCR;

        if (request.ActionType == ActionType.Rejected)
            return await Reject();

        return await Approve();

        //TODO


        #region local functions

        async Task<ApiResponseModel<ProcessResponse>> Reject()
        {
            if (isRequestedToCancelChangeLimit)
                await RejectCancelChangeLimitRequest(cardInfo, changeLimitHistory);

            await _requestActivityAppService.CompleteActivity(request);

            _auditLogger.Log.Information("Change limit: Successfully rejected change limit request for requestId:{requestid}", request);

            return Success(new ProcessResponse() { CardNumber = "" }, message: "Successfully rejected change limit request");
            #region local functions
            async Task RejectCancelChangeLimitRequest(CardDetailsResponse cardInfo, List<ChangeLimitHistoryDto> changeLimitHistory)
            {
                var CancelLimitRequest = changeLimitHistory!.FirstOrDefault(x => x.Status == ChangeLimitStatus.CANCEL_TEMP_LIMIT.ToString());

                await UpdateChangeLimitHistoryStatus(CancelLimitRequest.Id, ChangeLimitStatus.APPROVED);
                _fdrDBContext.RequestParameters.Remove(new() { ReqId = cardInfo.RequestId, Parameter = "CANCEL_CHANGE_LIMIT_REQUEST" });
            }
            #endregion
        }


        async Task<ApiResponseModel<ProcessResponse>> Approve()
        {
            await ApprovalValidation();

            var transaction = await _fdrDBContext.Database.BeginTransactionAsync();

            try
            {

                if (isRequestedToCancelChangeLimit)
                    await ApproveCancelChangeLimitRequest();
                else
                    await ApproveChangeLimitRequest();

                await _requestAppService.UpdateCollateralDetails(cardInfo.RequestId, request.CollateralAccountNumber, request.CollateralNumber, request.CollateralAmount);

                await transaction.CommitAsync();
            }
            catch (System.Exception ex)
            {
                _auditLogger.Log.Error(ex, "Change limit: Failed to approve change limit for requestId:{requestid}", cardInfo.RequestId);

                await transaction.RollbackAsync();
                throw;
            }


            //*************************************************************************************************************************************************************************************************************************************************************
            // Haitham Salem : Logging Request Activity for Limit Change / Increase Only **********************************************************************************************************************************************************************************
            //*************************************************************************************************************************************************************************************************************************************************************

            //var pendingActivity = (await _requestActivityAppService.GetAllRequestActivity(new() { RequestId = cardInfo.RequestId, Status = RequestActivityStatus.Pending, CFUActivity = CFUActivity.LIMIT_CHANGE_INCR }))?.Data;

            //if (pendingActivity.AnyWithNull())
            //{
            //    var requestActivityId = (int)pendingActivity!.FirstOrDefault()?.RequestActivityID!; //(int)request.RequestActivityId
            //                                                                                        //New change limits requests are updated to be inserted to request activity in maker stages, so only its status will be updated here -- SAMEEH                              
            //    await ApproveRequestActivity(cardInfo.IssuanceType, cardInfo.CivilId, requestActivityId, RequestActivityStatus.Approved);
            //}
            //else
            //{
            //    await _requestActivityAppService.LogRequestActivity(new()
            //    {
            //        IssuanceTypeId = (int)cardInfo.IssuanceType,
            //        CivilId = cardInfo.CivilId,
            //        RequestId = cardInfo.RequestId,
            //        CfuActivityId = (int)CFUActivity.LIMIT_CHANGE_INCR,
            //        RequestActivityStatusId = (int)RequestActivityStatus.New
            //    });
            //}


            await _requestActivityAppService.CompleteActivity(request);
            _auditLogger.Log.Information("Change limit: Successfully approved change limit request for requestId:{requestid}", cardInfo.RequestId);
            return Success(new ProcessResponse(), message: "Successfully approved change limit request");

            /// Filter non-closed supplementary cards
            #region local functions

            async Task ApprovalValidation()
            {
                var currentUser = _authManager.GetUser();

                //maker cannot approve his own request
                if (currentUser.KfhId.Equals(requestActivity.TellerId.ToString("0")))
                    throw new ApiException(message: GlobalResources.MakerCheckerAreSame);

                //if (cardInfo.ProductType == ProductTypes.ChargeCard)
                //    await GetCardNewInstallment();

                await Task.CompletedTask;
            }

            //async Task<decimal> GetCardNewInstallment()
            //{
            //    try
            //    {
            //        var cardDef = await _cardDetailsAppService.GetCardWithExtension(cardInfo.CardType);

            //        if (cardDef.Installments == 0)
            //            return 1;

            //        return pendingRequest.NewLimit / cardDef.Installments ?? 0;
            //    }
            //    catch (System.Exception ex)
            //    {
            //        _auditLogger.Log.Information("failed to get new installment for card {card}, Civil Id : {civilid}", cardInfo.ProductName, cardInfo.CivilId);
            //        return -1;
            //    }

            //}

            async Task ApproveCancelChangeLimitRequest()
            {
                var CancelLimitRequest = changeLimitHistory!.FirstOrDefault(x => x.Status == ChangeLimitStatus.CANCEL_TEMP_LIMIT.ToString());
                try
                {
                    //submit vmx call
                    var cancelledResult = (await _creditCardUpdateServiceClient.performTemporaryCreditLimitUpdateAsync(new()
                    {
                        cardNo = cardInfo.CardNumber,
                        tempCreditLimit = Convert.ToDouble(CancelLimitRequest!.NewLimit),
                        noOfExpiryDays = CancelLimitRequest.PurgeDays
                    }))?.performTemporaryCreditLimitUpdateResult;

                    if (!cancelledResult.isSuccessful)
                        throw new ApiException(message: cancelledResult.description);
                }
                catch (System.Exception)
                {

                }


                await UpdateChangeLimitHistoryStatus(CancelLimitRequest.Id, ChangeLimitStatus.TEMP_LIMIT_CANCELED);
                _fdrDBContext.RequestParameters.Remove(new() { ReqId = cardInfo.RequestId, Parameter = "CANCEL_CHANGE_LIMIT_REQUEST" });
            }

            async Task ApproveChangeLimitRequest()
            {
                var pendingRequest = changeLimitHistory!.FirstOrDefault(x => x.Status == ChangeLimitStatus.PENDING.ToString());

                if (pendingRequest.InitiatorId.Equals(_authManager.GetUser()?.KfhId))
                    throw new ApiException(message: GlobalResources.MakerCheckerAreSame);


                if (!_authManager.HasPermission(Permissions.ChangeLimit.EnigmaApprove()))
                    throw new ApiException(message: GlobalResources.NoAuthorized);


                var lastRequest = changeLimitHistory?.Last();
                decimal? newLimit = lastRequest!.NewLimit;
                bool isTempChange = lastRequest!.ChangeType == GlobalResources.Temp;
                int purgeDays = lastRequest.PurgeDays;

                CorporateProfileDto corporateProfile = await ValidateCorporateProfile(cardInfo.CardType, cardInfo.Parameters.CorporateCivilId, newLimit);

                pendingRequest.ApproveDate = DateTime.Now;
                pendingRequest.ApproverId = _authManager.GetUser()?.KfhId;
                pendingRequest.Status = ChangeLimitStatus.APPROVED.ToString();
                await UpdateChangeLimitHistoryAndRequestParameters(pendingRequest, corporateProfile);

                //reloading again to get latest history
                changeLimitHistory = (await GetChangeLimitHistory(requestActivity.RequestId))?.Data;
                lastRequest = changeLimitHistory?.Last();

                decimal OldLimit = lastRequest is null ? cardInfo.ApproveLimit : lastRequest.OldLimit;
                bool isLimitIncrease = newLimit > OldLimit;
                bool IsReadyToUpdateRequestActivity = false;

                try
                {

                    //submit vmx call
                    if (isTempChange)
                    {
                        var updateResult = (await _creditCardUpdateServiceClient.performTemporaryCreditLimitUpdateAsync(new()
                        {
                            cardNo = cardInfo.CardNumber,
                            tempCreditLimit = Convert.ToDouble(newLimit),
                            noOfExpiryDays = purgeDays
                        }))?.performTemporaryCreditLimitUpdateResult;

                        if (!updateResult.isSuccessful)
                            throw new ApiException(message: updateResult.description);

                        IsReadyToUpdateRequestActivity = isLimitIncrease;
                    }
                    else
                    {
                        bool? permanentLimitChangedInMq = cardInfo.IsSupplementaryCard
                            ? (await _creditCardUpdateServiceClient.changeSupplementaryLimitAsync(new() { cardNo = cardInfo.CardNumber, limit = Convert.ToInt64(newLimit) }))?.changeSupplementaryLimitResult?.isSuccessful
                            : (await _creditCardUpdateServiceClient.performChangeMqCreditLimitAsync(new() { cardNo = cardInfo.CardNumber, newLimit = Convert.ToInt64(newLimit) }))?.performChangeMqCreditLimitResult;

                        IsReadyToUpdateRequestActivity = (permanentLimitChangedInMq ?? false) && isLimitIncrease;
                        //*************************************************************************************************************************************************************************************************************************************************************
                        // Haitham Salem : Logging Request Activity for Limit Change / Increase Only **********************************************************************************************************************************************************************************
                        //*************************************************************************************************************************************************************************************************************************************************************
                        //commented by SAMEEH because its updated to insert reuest activity details in maker stage, and here only update status to APPROVED -- SAMEEH 
                    }

                }
                catch (System.Exception ex) when (ex is CommunicationException or FaultException)
                {

                    var secondaryCardDetailsResponse = (await _creditCardInquiryServiceClient.getBalanceStatusCardDetailAsync(new getBalanceStatusCardDetailRequest()
                    {
                        cardNo = cardInfo.CardNumber,
                    }))?.getBalanceStatusCardDetailResult.Adapt<CardDetailsResponse>();

                    if (secondaryCardDetailsResponse.Limit != changeLimitHistory?.Last()?.NewLimit)
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                    else
                    {
                        IsReadyToUpdateRequestActivity = true;
                    }

                }
                catch (System.Exception)
                {
                    throw;
                }

            }
            #endregion
        }
        #endregion
    }


    [HttpPost]
    public async Task InsertTayseerCreditCheckingRecord(TayseerCreditCheckingDto tayseerObj)
    {
        tayseerObj.Id = (await _fdrDBContext.Database.SqlQueryRaw<long>("select FDR.SEQ_TAYSEER_CREDIT_CHECKING.nextval from Dual").ToListAsync()).FirstOrDefault();
        await _fdrDBContext.TayseerCreditCheckings.AddAsync(tayseerObj.Adapt<TayseerCreditChecking>());
        await _fdrDBContext.SaveChangesAsync();
    }


    [HttpPost]
    public async Task<ApiResponseModel<CreditCheckCalculationResponse>> CheckCBKViolationStatus([FromBody] CreditCheckCalculationRequest request)
    {
        if (request.PreviousInstallments is null)
            throw new ApiException(message: "Invalid PreviousInstallments");

        CreditCheckCalculationResponse cBKReq = new() { isThereDBRException = "", isThereException = "", LastApproved = request.NewCreditCheck };

        int age = CalculateAge(request.DateOfBirth);

        decimal currentCardInstallment, approvedKFHLimit;

        decimal kfhPreviousCardInstallment = request.PreviousInstallments?.PrevKFHCardInstallment ?? 0;
        decimal kfhPreviousCardLimit = request.PreviousInstallments?.PrevKFHCardLimit ?? 0;

        var req = (await _cardDetailsAppService.GetCardInfo(request.RequestId))?.Data;

        if (request.NewCreditCheck.EntryType == 1)//new entry type
            approvedKFHLimit = req.ApproveLimit;
        else
        {
            // Haitham: version 2.5 no need to apply this validation because we already applied limit validation while initiating limit change request
            approvedKFHLimit = request.NewCreditCheck.NewLimit ?? 0;//variable entry type
            /*checkLimit = CheckLimitWithinProductRange((int)approvedKFHLimit, ddlRequestType.SelectedValue);            */
        }


        // Haitham's Caption: these are the calculated three major keys
        currentCardInstallment = req.Installment;


        if (request.NewCreditCheck.EntryType == (int)EntryType.LimitChange)
        {
            // in case of change limit current card installment should be the new card
            // limit / no of installments, not as new credit checking (entryType = 1)
            // in new credit checking record we consider the current inst. based on lblThisCardInsallmentValue
            currentCardInstallment = (request.NewCreditCheck.NewLimit ?? 0) / currentCardInstallment;
        }

        decimal percentage = ((currentCardInstallment + request.NewCreditCheck.CinetInstallment + kfhPreviousCardInstallment) / request.NewCreditCheck.KfhSalary) * 100;

        string isThereDBRException = "DBR exception is not allowed, the Card can’t be processed";

        int targetPercentage = request.NewCreditCheck.IsRetiree ? 30 : 40;

        cBKReq.DBRValue = percentage > targetPercentage;




        cBKReq.PercentageValue = (Math.Round(percentage, 3, MidpointRounding.AwayFromZero)).ToString() + " %";


        string isThereException = "There's an exception you must set exception value to yes and fill in remarks.";

        cBKReq.LimitGreaterThan10000Value = (approvedKFHLimit + kfhPreviousCardLimit + request.NewCreditCheck.OtherBankCreditLimit) > 10000;
        cBKReq.LimitSalaryX10Value = (approvedKFHLimit + kfhPreviousCardLimit + request.NewCreditCheck.OtherBankCreditLimit) > (request.NewCreditCheck.KfhSalary * 10);
        cBKReq.OverAgeValue = ((age > 0) && (age > 62));
        cBKReq.GuarantorValue = ((!request.NewCreditCheck.IsThereAguarantor) && (age < 21));
        cBKReq.RetireeValue = request.NewCreditCheck.IsRetiree;
        cBKReq.CapsValue = (request.NewCreditCheck.CapsType > 0);

        if (request.NewCreditCheck.IsThereAnException)
            return Success(cBKReq);

        if (cBKReq.DBRValue)
            cBKReq.isThereDBRException = isThereDBRException;

        if (cBKReq.LimitGreaterThan10000Value || cBKReq.LimitSalaryX10Value || cBKReq.OverAgeValue || cBKReq.RetireeValue || cBKReq.CapsValue)
            cBKReq.isThereException = isThereException;

        if (string.IsNullOrEmpty(cBKReq.isThereDBRException) && string.IsNullOrEmpty(cBKReq.isThereException))
            return Success(cBKReq);

        StringBuilder validationErrors = new();

        if (!string.IsNullOrEmpty(cBKReq.isThereDBRException))
            validationErrors.AppendLine(isThereDBRException);

        if (!string.IsNullOrEmpty(cBKReq.isThereException))
            validationErrors.AppendLine(isThereException);

        return Failure<CreditCheckCalculationResponse>(message: validationErrors.ToString());
    }


    [HttpPost]
    public async Task<ApiResponseModel<CreditCheckCalculationResponse>> CalculateCreditChecking([FromBody] CreditCheckCalculationRequest request)
    {
        CreditCheckCalculationResponse cBKReq = new() { isThereDBRException = "", isThereException = "", LastApproved = request.NewCreditCheck };

        var logs = from tcc in _fdrDBContext.TayseerCreditCheckings.AsNoTracking()
                   where tcc.RequestId == request.RequestId
                   select tcc;

        if (request.NewCreditCheck is null)
        {
            TayseerCreditCheckingDto? lastApprovedCredit = (await logs.Where(tcc => tcc.Status == (int)CreditCheckStatus.Approved).OrderByDescending(x => x.Id).FirstOrDefaultAsync())?.Adapt<TayseerCreditCheckingDto>();
            request.NewCreditCheck = lastApprovedCredit;
        }


        if (request.NewCreditCheck is null)
            return Success<CreditCheckCalculationResponse>(new() { IsValidCreditCheckData = false }, message: "No active credit check data");

        if (request.PreviousInstallments is null)
            throw new ApiException(message: "Invalid PreviousInstallments");





        int age = CalculateAge(request.DateOfBirth);

        decimal currentCardInstallment, approvedKFHLimit;
        decimal kfhPreviousCardInstallment = request.PreviousInstallments?.PrevKFHCardInstallment ?? 0;
        decimal kfhPreviousCardLimit = request.PreviousInstallments?.PrevKFHCardLimit ?? 0;

        var req = (await _cardDetailsAppService.GetCardInfo(request.RequestId))?.Data;

        if (request.NewCreditCheck.EntryType == 1)//new entry type
            approvedKFHLimit = req.ApproveLimit;
        else
        {
            // Haitham: version 2.5 no need to apply this validation because we already applied limit validation while initiating limit change request
            approvedKFHLimit = request.NewCreditCheck.NewLimit ?? 0;//variable entry type
            /*checkLimit = CheckLimitWithinProductRange((int)approvedKFHLimit, ddlRequestType.SelectedValue);            */
        }


        // Haitham's Caption: these are the calculated three major keys
        currentCardInstallment = req.Installment;


        if (request.NewCreditCheck.EntryType == (int)EntryType.LimitChange)
        {
            // in case of change limit current card installment should be the new card
            // limit / no of installments, not as new credit checking (entryType = 1)
            // in new credit checking record we consider the current inst. based on lblThisCardInsallmentValue
            currentCardInstallment = (request.NewCreditCheck.NewLimit ?? 0) / currentCardInstallment;
        }

        decimal percentage = ((currentCardInstallment + request.NewCreditCheck.CinetInstallment + kfhPreviousCardInstallment) / request.NewCreditCheck.KfhSalary) * 100;

        string isThereDBRException = "DBR exception is not allowed, the Card can’t be processed";

        int targetPercentage = request.NewCreditCheck.IsRetiree ? 30 : 40;

        cBKReq.DBRValue = percentage > targetPercentage;
        cBKReq.PercentageValue = (Math.Round(percentage, 3, MidpointRounding.AwayFromZero)).ToString() + " %";


        string isThereException = "There's an exception you must set exception value to yes and fill in remarks.";

        cBKReq.LimitGreaterThan10000Value = (approvedKFHLimit + kfhPreviousCardLimit + request.NewCreditCheck.OtherBankCreditLimit) > 10000;
        cBKReq.LimitSalaryX10Value = (approvedKFHLimit + kfhPreviousCardLimit + request.NewCreditCheck.OtherBankCreditLimit) > (request.NewCreditCheck.KfhSalary * 10);
        cBKReq.OverAgeValue = ((age > 0) && (age > 62));
        cBKReq.GuarantorValue = ((!request.NewCreditCheck.IsThereAguarantor) && (age < 21));
        cBKReq.RetireeValue = request.NewCreditCheck.IsRetiree;
        cBKReq.CapsValue = (request.NewCreditCheck.CapsType > 0);

        if (request.NewCreditCheck.IsThereAnException)
            return Success(cBKReq);

        if (cBKReq.DBRValue)
            cBKReq.isThereDBRException = isThereDBRException;

        if (cBKReq.LimitGreaterThan10000Value || cBKReq.LimitSalaryX10Value || cBKReq.OverAgeValue || cBKReq.RetireeValue || cBKReq.CapsValue)
            cBKReq.isThereException = isThereException;

        if (string.IsNullOrEmpty(cBKReq.isThereDBRException) && string.IsNullOrEmpty(cBKReq.isThereException))
            return Success(cBKReq);

        List<ValidationError> validationErrors = new();

        if (!string.IsNullOrEmpty(cBKReq.isThereDBRException))
            validationErrors.Add(new("", isThereDBRException));

        if (!string.IsNullOrEmpty(cBKReq.isThereException))
            validationErrors.Add(new(nameof(request.NewCreditCheck.IsThereAnException), isThereException));


        return Failure<CreditCheckCalculationResponse>(validationErrors: validationErrors);
    }

    [HttpGet]
    public async Task<TayseerCreditCheckingData> GetCreditCardCheckingPreviousLog(decimal requestId)
    {
        TayseerCreditCheckingData data = new();
        var logs = from tcc in _fdrDBContext.TayseerCreditCheckings.AsNoTracking()
                   where tcc.RequestId == requestId
                   select tcc;

        data.Footer = logs.Where(tcc => tcc.EntryType == 2 && tcc.Status == 3).Adapt<IEnumerable<TayseerCreditCheckingDto>>().ToList();
        data.Logs = logs.Where(tcc => tcc.Status != 3).Adapt<IEnumerable<TayseerCreditCheckingDto>>().ToList();
        var approvedLog = await logs.OrderByDescending(x => x.Id).FirstOrDefaultAsync(tcc => tcc.Status == 1);

        if (approvedLog is not null)
            data.ApprovedLog = approvedLog.Adapt<TayseerCreditCheckingDto>();

        return await Task.FromResult(data);
    }

    [HttpGet]
    public async Task<PreviousInstallments> GetPreviousInstallments(string civilId)
    {



        var _filtered_Req_Status = new int[] { (int)CreditCardStatus.Pending,
                                                     (int)CreditCardStatus.PendingForCreditCheckingReview,
                                                     (int)CreditCardStatus.CreditCheckingReviewRejected,
                                                     (int)CreditCardStatus.CreditCheckingReviewed,
                                                     (int)CreditCardStatus.CreditCheckingRejected };


        var externalStatus = new string[] { "C", "L", "U" };

        var oldFdAcnos = await (from _rp in _fdrDBContext.RequestParameters.AsNoTracking()
                                where _rp.Parameter == "OLD_FD_ACCT_NO"
                                select _rp.Value).ToListAsync();

        var tayseerCardRequest = await (from _r in _fdrDBContext.Requests.AsNoTracking()
                                        join _d in _fdrDBContext.CardDefs.AsNoTracking() on _r.CardType equals _d.CardType
                                        join _rp in _fdrDBContext.RequestParameters.AsNoTracking() on _r.RequestId equals _rp.ReqId
                                        where _d.Duality == 7 && _r.CivilId == civilId && _filtered_Req_Status.Any(_req_status => _r.ReqStatus == _req_status)
                                        && (_rp.Parameter == "ISSUING_OPTION" && (_rp.Value == "AGAINST_SALARY" || _rp.Value == "EXCEPTION"))
                                        select _r.RequestId).ToListAsync();

        var cbkLimitCreditChecks = await (from _tcc in _fdrDBContext.TayseerCreditCheckings.AsNoTracking()
                                          where _tcc.EntryType == (short)EntryType.LimitChange
                                          select _tcc.RequestId).ToListAsync();


        var _result = await (from cbkCard in _fdrDBContext.CbkCards.AsNoTracking()
                             from request in _fdrDBContext.Requests.AsNoTracking()
                             from cardDef in _fdrDBContext.CardDefs.AsNoTracking()
                             where cbkCard.ReqId == request.RequestId && request.CardType == cardDef.CardType && cbkCard.Cid == civilId
                             && cardDef.Duality == 7
                             && ((cbkCard.IssuingOption ?? "").Contains(Collateral.AGAINST_SALARY.ToString()) || cbkCard.IssuingOption.Contains(Collateral.EXCEPTION.ToString()))
                             && (!externalStatus.Contains(cbkCard.ExternalStatus ?? "K") || ((cbkCard.GrossBalanceSign ?? "").Contains("-") ? (cbkCard.GrossBalance ?? 0) * -1 : (cbkCard.GrossBalance ?? 0)) > 0)
                             && !oldFdAcnos.Contains(request.FdAcctNo ?? "-100")
                             /* Discard The Currently created card because in percentage it is calculated separately*/
                             && !tayseerCardRequest.Contains(cbkCard.ReqId.Value)
                             /* in case of limit change discard this card from previous card inst. because we already considered it in current card Inst. */
                             && !cbkLimitCreditChecks.Contains(cbkCard.ReqId.Value)

                             select new PreviousKFHLimitAndInstallments()
                             {
                                 ReqID = request.RequestId,
                                 CID = cbkCard.Cid ?? "",
                                 CreditLimitAmount = cbkCard.CreditLimitAmount / 1000,
                                 IssueDate = cbkCard.IssueDate,
                                 CardNo = request.CardNo ?? "",
                                 GrossBalance = cbkCard.GrossBalance / 1000,
                                 ExternalStatus = cbkCard.ExternalStatus ?? "",
                                 IssuingOption = cbkCard.IssuingOption ?? "",
                                 CardType = request.CardType,
                                 Duality = cardDef.Duality,
                                 Installments = cardDef.Installments
                             }).ToListAsync();


        PreviousInstallments response = new()
        {
            PreviousKFHLimitAndInstallments = _result,
            PrevKFHCardLimit = _result?.Select(i => i.CreditLimitAmount).Sum() ?? 0,
            PrevKFHCardInstallment = _result?.Where(x => x.Installments > 0).Select(i => (i.CreditLimitAmount / i.Installments)).Sum() ?? 0
        };



        return response;


    }
    #endregion

    #region private methods
    private async Task ValidateBiometricStatus(decimal requestId)
    {
        var request = _fdrDBContext.Requests.AsNoTracking().FirstOrDefault(x => x.RequestId == requestId) ?? throw new ApiException(message: "Invalid request Id "); 
        var bioStatus = await _customerProfileCommonApi.GetBiometricStatus(request!.CivilId);
        if (bioStatus.ShouldStop)
            throw new ApiException(message: GlobalResources.BioMetricRestriction);
    }
    private static int CalculateAge(DateTime birthday)
    {
        int yearsOld = DateTime.Today.Year - birthday.Year;
        if (DateTime.Today < birthday.AddYears(yearsOld)) yearsOld--;
        return yearsOld;
    }

    private async Task UpdateChangeLimitHistoryStatus(decimal id, ChangeLimitStatus newStatus)
    {
        await _fdrDBContext.ChangeLimitHistories.Where(x => x.Id == id).ExecuteUpdateAsync((s) => s.SetProperty(x => x.Status, newStatus.ToString()));
    }
    private async Task UpdateChangeLimitHistoryAndRequestParameters(ChangeLimitHistoryDto pendingRequest, CorporateProfileDto corporateProfile)
    {

        _auditLogger.LogWithEvent(nameof(UpdateChangeLimitHistoryAndRequestParameters)).Information("updating change limit history for requestId:{requestid}, Id:{requestActivityId}", pendingRequest.ReqId, pendingRequest.Id);

        _fdrDBContext.ChangeLimitHistories.Update(pendingRequest.Adapt<ChangeLimitHistory>());
        await _fdrDBContext.SaveChangesAsync();

        _auditLogger.LogWithEvent(nameof(UpdateChangeLimitHistoryAndRequestParameters)).Information("Updated  change limit history status for requestId:{requestid}, Id:{requestActivityId}", pendingRequest.ReqId, pendingRequest.Id);


        decimal requestId = pendingRequest.ReqId;
        var requestParameter = new RequestParameterDto();
        bool isCorporateCard = !string.IsNullOrEmpty(corporateProfile.CorporateCivilId);


        if (isCorporateCard)
        {
            List<RequestParameter> requestParameters = new() {
                    new() { ReqId=requestId, Parameter="cmt_type" } ,
                    new() { ReqId=requestId, Parameter="commitment_no" } ,
                    new() { ReqId=requestId, Parameter="amt" } ,
                    new() { ReqId=requestId, Parameter="undisbursed" } ,
                    new() { ReqId=requestId, Parameter="mat_dt" } ,
                    new() { ReqId=requestId, Parameter="status" } ,
            };
            _fdrDBContext.RequestParameters.RemoveRange(requestParameters);

            requestParameter = new RequestParameterDto()
            {
                CommitmentType = corporateProfile.GlobalLimitDto.CommitmentType,
                CommitmentNo = corporateProfile.GlobalLimitDto.CommitmentNo ?? "",
                Amount = corporateProfile.GlobalLimitDto.Amount.ToString(),
                Undisbursed = corporateProfile.GlobalLimitDto.UndisbursedAmount.ToString(),
                MaturityDate = corporateProfile.GlobalLimitDto.MaturityDate.Formed(),
                Status = corporateProfile.GlobalLimitDto.Status ?? "",
            };
        }
        else
        {
            var oldRequestParameter = await _fdrDBContext.RequestParameters.FirstOrDefaultAsync(x => x.ReqId == requestId && x.Parameter == "CHANGE_LIMIT_REQUEST");
            if (oldRequestParameter != null)
            {
                _fdrDBContext.RequestParameters.Remove(oldRequestParameter);
            }
            requestParameter = new RequestParameterDto() { ChangeLimitRequest = "1" };
        }

        await _requestAppService.AddRequestParameters(requestParameter, requestId);


        if (pendingRequest.ChangeType == GlobalResources.Permanent && pendingRequest.Status == ChangeLimitStatus.APPROVED.ToString())
        {
            var cardRequest = await _fdrDBContext.Requests.FirstOrDefaultAsync(x => x.RequestId == requestId);
            cardRequest.ApproveLimit = pendingRequest.NewLimit;
        }

        await _fdrDBContext.SaveChangesAsync();
    }
    private async Task<ApiResponseModel<ProcessResponse>> ApproveRequestActivity(IssuanceTypes issuanceType, string civilId, decimal requestActivityId, RequestActivityStatus status = RequestActivityStatus.Rejected)
    {
        await _requestActivityAppService.UpdateRequestActivityStatus(new()
        {
            IssuanceTypeId = (int)issuanceType,
            CivilId = civilId,
            RequestActivityId = requestActivityId,
            RequestActivityStatusId = (int)status
        });
        return Success(new ProcessResponse() { CardNumber = "" });
    }
    private async Task<ApiResponseModel<ProcessResponse>> RejectRequestActivity(string reasonForRejection, IssuanceTypes issuanceType, string civilId, decimal requestActivityId, RequestActivityStatus status = RequestActivityStatus.Rejected)
    {
        await _requestActivityAppService.UpdateRequestActivityStatus(new()
        {
            ReasonForRejection = reasonForRejection,
            IssuanceTypeId = (int)issuanceType,
            CivilId = civilId,
            RequestActivityId = requestActivityId,
            RequestActivityStatusId = (int)status
        });
        return Success(new ProcessResponse() { CardNumber = "" }, message: "Successfully Rejected");
    }
    private async Task<CorporateProfileDto> GetCorporateProfile(string corporateCivilId)
    {
        var corporateProfile = (await _fdrDBContext.CorporateProfiles.FirstOrDefaultAsync(x => x.CorporateCivilId == corporateCivilId)
           ?? throw new ApiException(errors: null, message: "Corporate profile not found")).Adapt<CorporateProfileDto>();

        bool canViewCardNumber = _authManager.HasPermission(Permissions.CreditCardsNumber.View());


        var corporateCards = await (from paramter in _fdrDBContext.RequestParameters.AsNoTracking()
                                    join cardRequest in _fdrDBContext.Requests.AsNoTracking() on paramter.ReqId equals cardRequest.RequestId
                                    where paramter.Parameter == "corporate_civil_id" && paramter.Value == corporateProfile.CorporateCivilId
                                    select new CorporateCard
                                    {
                                        RequestId = paramter.ReqId,
                                        CivilId = cardRequest.CivilId,
                                        BankAccountNumber = cardRequest.AcctNo ?? string.Empty,
                                        ApprovedLimit = cardRequest.ApproveLimit ?? 0,
                                        CardNumber = cardRequest.CardNo,
                                        CardNumberDto = canViewCardNumber ? cardRequest.CardNo ?? "" : cardRequest.CardNo.Masked(6, 6),
                                        CardType = cardRequest.CardType,
                                        RequestStatus = cardRequest.ReqStatus,
                                        CardExpiry = cardRequest.Expiry,
                                        FixedDepositAccountNumber = cardRequest.FdAcctNo,
                                        RequestDate = cardRequest.ReqDate,
                                        BranchId = cardRequest.BranchId,
                                    }
                             ).ToListAsync();

        corporateProfile.CorporateCards = corporateCards;

        return corporateProfile;
    }
    private async Task<corpCreditCardGlobalLimitDTO?> GetCorporateGlobalLimitAsync(string corporateCivilId)
    {
        var corporateLimit = (await _corporateCreditCardServiceClient.getCorporateGlobalLimitAsync(new() { corpCivilID = corporateCivilId }))?.getCorporateGlobalLimit ?? new();
        if (corporateLimit.corpCreditCardGlobalLimitDTO is null) return null;

        return corporateLimit.corpCreditCardGlobalLimitDTO[0];
    }
    private async Task<CorporateProfileDto> ValidateCorporateProfile(int cardType, string? corporateCivilId, decimal? approveLimit = 0)
    {
        var corporateCardTypeIds = (await _fdrDBContext.ConfigParameters.AsNoTracking().FirstOrDefaultAsync(x => x.ParamName == ConfigurationBase.CorporateCardTypeIds))?.ParamValue.Split(",") ?? Array.Empty<string>();

        if (!corporateCardTypeIds.Any(x => x == cardType.ToString()))
            return new();

        if (!long.TryParse(corporateCivilId, out long _corporateCivilId))
            throw new ApiException(errors: null, message: "Invalid Corporate Civil ID");

        var corporateProfile = await GetCorporateProfile(corporateCivilId)
            ?? throw new ApiException(errors: null, message: "Invalid Corporate Civil ID");

        var corporateLimit = await GetCorporateGlobalLimitAsync(corporateProfile.CorporateCivilId);

        if (string.IsNullOrEmpty(corporateLimit?.commitmentNo))
            throw new ApiException(errors: null, message: "No commitment for this corporate, kindly create one for it.");

        double remainingLimit = corporateLimit?.undisbursedAmount ?? 0;
        double newLimit = (double)(approveLimit ?? 0);

        if (remainingLimit < newLimit)
            throw new ApiException(errors: null, message: "The available corporate limit is less than card limit");

        if (!(corporateLimit?.maturityDateSpecified ?? false))
            throw new ApiException(errors: null, message: "Sorry, the company's account data is incomplete, there is no expiry date");

        if (corporateLimit?.maturityDate < DateTime.Now)
            throw new ApiException(errors: null, message: "Sorry, this Corporate Profile is Expired");

        return corporateProfile;
    }
    #endregion private methods
}
