using CreditCardsSystem.Data;
using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Models.Access;
using CreditCardsSystem.Domain.Models.Card;
using CreditCardsSystem.Domain.Models.CardOperation;
using CreditCardsSystem.Domain.Models.CreditReverse;
using CreditCardsSystem.Domain.Models.Workflow;
using CreditCardsSystem.Domain.Shared.Models.Reports;
using CreditCardsSystem.Utility.Crypto;
using CreditCardsSystem.Utility.Extensions;
using CreditCardTransactionInquiryServiceReference;
using CreditCardUpdateServiceReference;
using CustomerAccountsServiceReference;
using Kfh.Aurora.Auth;
using Kfh.Aurora.Integration;
using Kfh.Aurora.Logging;
using Microsoft.EntityFrameworkCore;
using MonetaryTransferServiceReference;
using Newtonsoft.Json;

namespace CreditCardsSystem.Application.CreditReverse;
public class CreditReverseAppService : BaseRequestActivity, ICreditReverseAppService, IAppService
{
    private readonly CreditCardUpdateServicesServiceClient _creditCardUpdateServiceClient;
    private readonly CustomerAccountsServiceClient _customerAccountServiceClient;
    private readonly CreditCardInquiryServicesServiceClient _creditCardInquiryServiceClient;
    private readonly ICreditCardsAppService _creditCardsAppService;
    private readonly ICurrencyAppService _currencyAppService;
    private readonly ICardDetailsAppService _cardDetailsAppService;
    private readonly MonetaryTransferServiceClient _monetaryTransferServiceClient;
    private readonly IAuditLogger<CreditReverseAppService> _auditLogger;
    private readonly IAuthManager authManager;
    private readonly IRequestActivityAppService _requestActivityAppService;
    private readonly FdrDBContext _fdrDBContext;

    public CreditReverseAppService(
    IIntegrationUtility integrationUtility,
    IOptions<IntegrationOptions> options,
    ICreditCardsAppService creditCardsAppService,
    FdrDBContext fdrDBContext,
    ICurrencyAppService currencyAppService,
    ICardDetailsAppService cardDetailsAppService,
    IAuditLogger<CreditReverseAppService> auditLogger,
    IAuthManager authManager, IRequestActivityAppService requestActivityAppService) : base(requestActivityAppService)
    {

        _monetaryTransferServiceClient = integrationUtility.GetClient<MonetaryTransferServiceClient>(options.Value.Client, options.Value.Endpoints.MonetaryTransfer, options.Value.BypassSslValidation);
        _creditCardUpdateServiceClient = integrationUtility.GetClient<CreditCardUpdateServicesServiceClient>(options.Value.Client, options.Value.Endpoints.CreditCardUpdate, options.Value.BypassSslValidation);
        _customerAccountServiceClient = integrationUtility.GetClient<CustomerAccountsServiceClient>(options.Value.Client, options.Value.Endpoints.CustomerAccount, options.Value.BypassSslValidation);
        _creditCardInquiryServiceClient = integrationUtility.GetClient<CreditCardInquiryServicesServiceClient>(options.Value.Client, options.Value.Endpoints.CreditCardTransactionInquiry, options.Value.BypassSslValidation);
        _creditCardsAppService = creditCardsAppService;
        _fdrDBContext = fdrDBContext;
        _currencyAppService = currencyAppService;
        _cardDetailsAppService = cardDetailsAppService;
        _auditLogger = auditLogger;
        this.authManager = authManager;
        _requestActivityAppService = requestActivityAppService;
    }
    [HttpGet]
    public async Task<ApiResponseModel<List<CreditReverseDto>>> GetPendingRequest(string cardNumber)
    {
        if (cardNumber?.Length > 14)
        {
            cardNumber = cardNumber.DeSaltThis();
        }

        var pendingRequest = await (from req in _fdrDBContext.Requests.AsNoTracking().Where(x => x.CardNo == cardNumber)
                                    join cr in _fdrDBContext.CreditReverses.AsNoTracking().Where(x => x.Status == 0) on req.RequestId equals cr.ReqId
                                    join pr in _fdrDBContext.Profiles.AsNoTracking() on req.CivilId equals pr.CivilId
                                    select new CreditReverseDto()
                                    {
                                        CardCurrency = cr.CardCurrency,
                                        ID = cr.Id,
                                        RequestID = cr.ReqId,
                                        Amount = cr.Amount,
                                        AmountKDMaker = cr.AmountKdMaker,
                                        AmountKDChecker = cr.AmountKdChecker ?? 0,
                                        RateChecker = cr.RateChecker,
                                        RateMaker = cr.RateMaker,
                                        RequestedBy = cr.RequestedBy,
                                        ApprovedBy = cr.ApprovedBy,
                                        RequestorReason = cr.RequestorReason,
                                        ApproverReason = cr.ApproverReason,
                                        RejectDate = cr.RequestDate,
                                        AppoveDate = cr.ApproveDate,
                                        RequestDate = cr.RejectDate,
                                        Status = cr.Status,
                                        AccountNo = cr.AcctNo,
                                        CustomerNameEn = pr.FullName,
                                        CardNo = req.CardNo,
                                        ApproveLimit = req.ApproveLimit,
                                        CivilID = req.CivilId,
                                        CardType = req.CardType,
                                        CustomerNameAr = pr.ArabicName,
                                        IsLocked = cr.Islocked ?? false
                                    }
                            ).ToListAsync();
        return Success(pendingRequest);
    }

    [HttpGet]
    public async Task<ApiResponseModel<CreditReverseResponse>> DeleteCreditReverseRequestById(long id)
    {

        var creditReverse = await _fdrDBContext.CreditReverses.FirstOrDefaultAsync(x => x.Id == id) ?? throw new ApiException(message: "Invalid credit reverse id");
        _fdrDBContext.CreditReverses.Remove(creditReverse);
        await _fdrDBContext.SaveChangesAsync();

        string crid = creditReverse.Id.ToString();
        var requestActivityId = (await _fdrDBContext.RequestActivityDetails.AsNoTracking().FirstOrDefaultAsync(x => x.Paramter == DetailKey.KEY_CreditReverseId.ToString() && x.Value == crid))?.RequestActivityId;
        if (requestActivityId is not null)
        {
            await _requestActivityAppService.UpdateRequestActivityStatus(new()
            {
                RequestActivityId = requestActivityId!.Value,
                RequestActivityStatusId = (int)RequestActivityStatus.Deleted,
                CivilId = "",
                IssuanceTypeId = 0
            });
        }
        return Success<CreditReverseResponse>(null, message: "Success!");
    }
    [HttpPost]
    public async Task<ApiResponseModel<CreditReverseResponse>> RequestCreditReverse([FromBody] CreditReverseRequest request)
    {
        if (!authManager.HasPermission(Permissions.CreditReverse.Request()))
            throw new ApiException(message: GlobalResources.NotAuthorized);

        CreditReverseAmount reverseAmount = await RequestValidatation(request);

        var crId = await _fdrDBContext.CreditReverses.MaxAsync(x => x.Id) + 1;
        _ = int.TryParse(authManager.GetUser()?.KfhId, out int _kfhId);

        await _fdrDBContext.CreditReverses.AddAsync(new()
        {
            Id = crId,
            ReqId = (long)request.RequestId,
            AcctNo = request.DebitAccountNumber,
            Amount = request.Amount,
            AmountKdMaker = reverseAmount.Amount,
            RateMaker = Convert.ToDecimal(reverseAmount.Rate),
            RequestedBy = _kfhId,
            RequestorReason = "",
            Status = 0,
            CardCurrency = reverseAmount.CardCurrency?.CurrencyIsoCode
        });

        await _fdrDBContext.SaveChangesAsync();

        await _requestActivityAppService.LogRequestActivity(new()
        {
            CivilId = request.CivilId,
            IssuanceTypeId = request.IssuanceTypeId ?? 0,
            CfuActivityId = (int)CFUActivity.CREDIT_REVERSE,
            //RequestActivityStatusId = (int)RequestActivityStatus.New,
            RequestId = request.RequestId,
            Details = new(){
            { DetailKey.KEY_CreditReverseId,crId.ToString() },
            { DetailKey.KEY_REQUEST_ID, request.RequestId.ToString() },
            { DetailKey.KFH_ACCOUNT_NO, request.DebitAccountNumber.ToString() },
            { DetailKey.KEY_AMOUNT, request.Amount.ToString() },
            { DetailKey.KEY_AMOUNT_KD_MAKER, reverseAmount.Amount.ToString() },
            { DetailKey.KEY_RATE_MAKER, reverseAmount.Rate.ToString() },
            { DetailKey.KEY_CARD_CURRENCY,  reverseAmount.CardCurrency?.CurrencyIsoCode?.ToString()??"" }
            },
            WorkflowVariables = new() {
                { "Description", $"Request Credit reverse for {request.CivilId} to DebitAccountNumber {request.DebitAccountNumber}" },
                { WorkflowVariables.CreditReverseId,crId.ToString() },
                { WorkflowVariables.KFHAccountNumber, request.DebitAccountNumber.ToString() },
                { WorkflowVariables.Amount, request.Amount.ToString() },
                { WorkflowVariables.AmountKDMaker, reverseAmount.Amount.ToString() },
                { WorkflowVariables.RateMaker, reverseAmount.Rate.ToString() },
                { WorkflowVariables.CardCurrency, reverseAmount.CardCurrency?.CurrencyIsoCode?.ToString()??"" },
                { WorkflowVariables.CardNumber, request.BeneficiaryCardNumber.SaltThis() }
            },
        }, isNeedWorkflow: true);

        return Success<CreditReverseResponse>(new(), message: GlobalResources.WaitingForApproval);

        async Task<CreditReverseAmount> RequestValidatation(CreditReverseRequest request)
        {
            await request.ModelValidationAsync(nameof(RequestCreditReverse));

            if (request.BeneficiaryCardNumber?.Length > 16)
                request.BeneficiaryCardNumber = request.BeneficiaryCardNumber.DeSaltThis();

            var reverseAmount = await ValidateCreditReverseRequest(request);
            return reverseAmount;
        }
    }


    [HttpPost]
    public async Task<ApiResponseModel<ProcessResponse>> ProcessCreditReverseRequest([FromBody] ProcessCreditReverseRequest request)
    {

        if (!authManager.HasPermission(Permissions.CreditReverse.EnigmaApprove()))
            throw new ApiException(message: GlobalResources.NotAuthorized);

        var requestActivity = (await _requestActivityAppService.GetRequestActivityById(request.RequestActivityId)).Data ?? throw new ApiException(message: "Unable to find Request");
        var cardInfo = (await _cardDetailsAppService.GetCardInfo(requestActivity.RequestId))?.Data ?? throw new ApiException(message: "Invalid request Id");
        _ = long.TryParse(requestActivity.Details[DetailKey.KEY_CreditReverseId], out long _cid);
        _ = int.TryParse(authManager.GetUser()?.KfhId, out int kfhId);

        var creditReverse = await _fdrDBContext.CreditReverses.FirstOrDefaultAsync(x => x.Id == _cid) ?? throw new ApiException(message: "invalid data");


        string log = $"{request.ActionType} {requestActivity.CfuActivity.GetDescription()} for request id civil id {cardInfo.RequestId}";
        StringBuilder logParameters = new();
        logParameters.Append(JsonConvert.SerializeObject(requestActivity.Details));

        request.CardNumber = cardInfo.CardNumber.Masked(6, 6);
        request.Activity = CFUActivity.CREDIT_REVERSE;

        if (request.ActionType == ActionType.Rejected)
            return await Reject();


        return await Approve();

        #region local functions
        async Task<ApiResponseModel<ProcessResponse>> Approve()
        {
            await ApprovalValidation();

            creditReverse.Lock();
            await _fdrDBContext.SaveChangesAsync();

            try
            {
                var refundResponse = await _creditCardUpdateServiceClient.performCCPymtRefundWithMQAsync(new()
                {
                    paymentRefundReq = new()
                    {
                        cardAmount = Convert.ToDouble(creditReverse.Amount),
                        cardAmountSpecified = true,
                        cardNo = cardInfo.CardNumber,
                        custAcctNo = creditReverse.AcctNo,
                        transRate = Convert.ToDouble(creditReverse.RateChecker),
                        transRateSpecified = true
                    },
                    TranSequence = Guid.NewGuid().ToString()
                });

                if (refundResponse?.performCCPymtRefundWithMQ.respCode != "0000")
                {
                    creditReverse.UnLock();
                    await _fdrDBContext.SaveChangesAsync();

                    _auditLogger.Log.Error("Failed to {message} {log} {parameters}", refundResponse?.performCCPymtRefundWithMQ.respMessage, log, logParameters.ToString());
                    return Failure<ProcessResponse>(message: "failed");
                }
            }
            catch (System.Exception)
            {

                creditReverse.UnLock();
                await _fdrDBContext.SaveChangesAsync();
                throw;
            }





            _auditLogger.Log.Error("Success to {log} {parameters}", log, logParameters.ToString());
            creditReverse.Status = 1;
            creditReverse.ApprovedBy = kfhId;
            creditReverse.ApproveDate = DateTime.Now.Date;
            creditReverse.UnLock();
            await _fdrDBContext.SaveChangesAsync();

            await _requestActivityAppService.CompleteActivity(request);

            return Success(new ProcessResponse(), message: "Successfully Approved !");

            #region local functions

            async Task ApprovalValidation()
            {
                var kfhId = authManager.GetUser()?.KfhId;
                //maker cannot approve his own request
                if (kfhId == requestActivity.TellerId.ToString("0"))
                    throw new ApiException(message: GlobalResources.MakerCheckerAreSame);


                if (creditReverse.Islocked ?? false)
                    throw new ApiException(message: "Record is Locked; Please Try again");


                if (cardInfo.Currency.IsForeignCurrency || cardInfo.Currency.CurrencyIsoCode != ConfigurationBase.KuwaitCurrency)
                {
                    var currencyRateResponse = await _currencyAppService.ValidateCurrencyRate(new()
                    {
                        CivilId = cardInfo!.CivilId,
                        SourceCurrencyCode = cardInfo.Currency.CurrencyIsoCode!,
                        ForeignCurrencyCode = ConfigurationBase.KuwaitCurrency,
                        SourceAmount = creditReverse.Amount,
                        DestinationAmount = 0
                    });

                    if (currencyRateResponse.IsSuccessWithData)
                    {

                        creditReverse.AmountKdChecker = currencyRateResponse!.Data!.DestAmount;
                        creditReverse.RateChecker = Convert.ToDecimal(currencyRateResponse!.Data!.TransferRate);
                    }
                }
                else
                {
                    creditReverse.AmountKdChecker = creditReverse.AmountKdMaker;
                    creditReverse.RateChecker = creditReverse.RateMaker;
                }

                await Task.CompletedTask;
            }


            #endregion
        }

        async Task<ApiResponseModel<ProcessResponse>> Reject()
        {
            creditReverse.Status = 2;
            creditReverse.ApprovedBy = kfhId;
            creditReverse.ApproverReason = request.ReasonForRejection;
            creditReverse.RejectDate = DateTime.Now.Date;
            await _fdrDBContext.SaveChangesAsync();
            _auditLogger.Log.Error("Success to {log} {parameters}", log, logParameters.ToString());

            await _requestActivityAppService.CompleteActivity(request);
            return Success(new ProcessResponse() { CardNumber = "" }, message: "Successfully Rejected");
        }
        #endregion
    }





    private async Task SaveVoucher()
    {
    }

    #region Private Methods


    private async Task<CreditReverseAmount> ValidateCreditReverseRequest(CreditReverseRequest request)
    {
        CreditReverseAmount reverseAmount = new(request.Amount);
        List<ValidationError> validationErrors = [];


        if (string.IsNullOrEmpty(request.BeneficiaryCardNumber) || string.IsNullOrEmpty(request.DebitAccountNumber))
            validationErrors.Add(new(nameof(request.BeneficiaryCardNumber), "Incorrect BeneficiaryCardNumber or DebitAccountNumber"));

        ThrowErrorIfAny(validationErrors);

        var debitAccount = (await _customerAccountServiceClient.viewAccountDetailsAsync(new() { acct = request.DebitAccountNumber }))?.viewAccountDetailsResult;
        if (debitAccount == null)
            validationErrors.Add(new(nameof(request.DebitAccountNumber), "Invalid Debit Account Number"));

        //if (request.Amount > Convert.ToDecimal(debitAccount?.availableBalance))
        //    validationErrors.Add(new(nameof(request.DebitAccountNumber), "The account balance is insufficient"));

        var cardCurrency = await _currencyAppService.GetCardCurrencyByRequestId(request.RequestId!);
        reverseAmount = reverseAmount with { CardCurrency = cardCurrency };

        string accountCurrency = debitAccount?.currency?.ToUpper() ?? "";

        if (cardCurrency is not null && !cardCurrency.IsForeignCurrency && accountCurrency != cardCurrency.CurrencyIsoCode)
            validationErrors.Add(new(nameof(request.DebitAccountNumber), "Selected card currency should match selected payment account currency"));

        CardDetailsResponse? cardInfo = request.CardInfo;

        if (cardInfo is null)
        {
            var cardInfoResponse = await _cardDetailsAppService.GetCardInfo(request.RequestId);
            if (!cardInfoResponse.IsSuccess)
                validationErrors.Add(new(nameof(request.DebitAccountNumber), "Invalid card data"));
            else
                cardInfo = cardInfoResponse.Data;
        }

        request.IssuanceTypeId = (int)cardInfo!.IssuanceType;
        var pendingRequest = await GetPendingRequest(request.BeneficiaryCardNumber!);


        if (pendingRequest.IsSuccessWithData && pendingRequest.Data?.Count != 0)
        {
            decimal RemainingRefundAmount = (cardInfo?.AvailableLimit - cardInfo?.Limit) - pendingRequest.Data?.Sum(x => x.Amount) ?? 0;

            if (reverseAmount.Amount > RemainingRefundAmount)
                validationErrors.Add(new(nameof(request.Amount), $"The entered TransferAmount is invalid, we have {RemainingRefundAmount} amount as a Remining Refund Amount"));
        }

        bool isDebitAccountLocalCurrency = accountCurrency.Equals(ConfigurationBase.KuwaitCurrency);

        if ((cardInfo?.IsSupplementaryCard ?? false) && !isDebitAccountLocalCurrency)
            validationErrors.Add(new(nameof(request.DebitAccountNumber), $"Payment from {accountCurrency} accounts is not allowed for supplementary card"));

        ThrowErrorIfAny(validationErrors);

        if (cardCurrency is not null && cardCurrency.IsForeignCurrency)
        {
            var currencyRateResponse = await _currencyAppService.ValidateCurrencyRate(new()
            {
                CivilId = cardInfo!.CivilId,
                SourceCurrencyCode = cardCurrency?.CurrencyIsoCode!,
                ForeignCurrencyCode = ConfigurationBase.KuwaitCurrency,
                SourceAmount = request.Amount,
                DestinationAmount = -1
            });

            if (currencyRateResponse.IsSuccessWithData)
                reverseAmount = new(currencyRateResponse!.Data!.DestAmount, currencyRateResponse.Data.TransferRate, CardCurrency: cardCurrency);
        }

        //validating final reverse amount with maximum valid amount
    

        ThrowErrorIfAny(validationErrors);
        return reverseAmount;
    }

    private record CreditReverseAmount(decimal Amount = 0, double Rate = 1, CardCurrencyDto? CardCurrency = null);


    private void ThrowErrorIfAny(List<ValidationError> validationErrors)
    {
        if (validationErrors.Count != 0) throw new ApiException(validationErrors, nameof(ValidateCreditReverseRequest), "validation failed");
    }







    #endregion


}
