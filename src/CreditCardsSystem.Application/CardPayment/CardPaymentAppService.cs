using CreditCardsSystem.Data;
using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Models.Access;
using CreditCardsSystem.Domain.Models.CardIssuance;
using CreditCardsSystem.Domain.Models.CardPayment;
using CreditCardsSystem.Domain.Models.CreditCards;
using CreditCardsSystem.Domain.Models.StandingOrder;
using CreditCardsSystem.Domain.Models.SupplementaryCard;
using CreditCardsSystem.Domain.Shared.Models.Account;
using CreditCardsSystem.Domain.Shared.Models.CardPayment;
using CreditCardsSystem.Utility.Crypto;
using CreditCardsSystem.Utility.Extensions;
using CreditCardTransactionInquiryServiceReference;
using CreditCardUpdateServiceReference;
using CustomerAccountsServiceReference;
using Kfh.Aurora.Auth;
using Kfh.Aurora.Common.Shared.Interfaces.Customer;
using Kfh.Aurora.Integration;
using Kfh.Aurora.Logging;
using Microsoft.EntityFrameworkCore;
using MonetaryTransferServiceReference;

namespace CreditCardsSystem.Application.CardPayment;
public class CardPaymentAppService(IIntegrationUtility integrationUtility,
                                   IOptions<IntegrationOptions> options,
                                   ICreditCardsAppService creditCardsAppService,
                                   FdrDBContext fdrDBContext,
                                   ICurrencyAppService currencyAppService,
                                   ICardDetailsAppService cardDetailsAppService,
                                   IAuditLogger<CardPaymentAppService> auditLogger,
                                   IAuthManager authManager,
                                   ICustomerProfileCommonApi customerProfileCommonApi) : BaseApiResponse, ICardPaymentAppService, IAppService
{
    private readonly CreditCardUpdateServicesServiceClient _creditCardUpdateServiceClient = integrationUtility.GetClient<CreditCardUpdateServicesServiceClient>(options.Value.Client, options.Value.Endpoints.CreditCardUpdate, options.Value.BypassSslValidation);
    private readonly CustomerAccountsServiceClient _customerAccountServiceClient = integrationUtility.GetClient<CustomerAccountsServiceClient>(options.Value.Client, options.Value.Endpoints.CustomerAccount, options.Value.BypassSslValidation);
    private readonly CreditCardInquiryServicesServiceClient _creditCardInquiryServiceClient = integrationUtility.GetClient<CreditCardInquiryServicesServiceClient>(options.Value.Client, options.Value.Endpoints.CreditCardTransactionInquiry, options.Value.BypassSslValidation);
    private readonly ICreditCardsAppService _creditCardsAppService = creditCardsAppService;
    private readonly ICurrencyAppService _currencyAppService = currencyAppService;
    private readonly ICardDetailsAppService _cardDetailsAppService = cardDetailsAppService;
    private readonly MonetaryTransferServiceClient _monetaryTransferServiceClient = integrationUtility.GetClient<MonetaryTransferServiceClient>(options.Value.Client, options.Value.Endpoints.MonetaryTransfer, options.Value.BypassSslValidation);
    private readonly IAuditLogger<CardPaymentAppService> auditLogger = auditLogger;
    private readonly IAuthManager authManager = authManager;
    private readonly ICustomerProfileCommonApi customerProfileCommonApi = customerProfileCommonApi;
    private readonly FdrDBContext _fdrDBContext = fdrDBContext;

    [HttpGet]
    public async Task<ApiResponseModel<List<OwnedCreditCardsResponse>>> GetOwnedCreditCards(string civilId)
    {
        var response = new ApiResponseModel<List<OwnedCreditCardsResponse>>() { IsSuccess = true };

        var result = await _creditCardsAppService.GetCreditCardsByCivilId(civilId);
        if (result.IsSuccess)
            response.Data = await FilterOwnCardForPayment(result?.Data!);
        else
            return response.Fail("Cards Not found");

        return response;
    }


    [HttpGet]
    public async Task<ApiResponseModel<List<SupplementaryCardDetail>>> GetSupplementaryCards(string civilId)
    {
        var response = new ApiResponseModel<List<SupplementaryCardDetail>>() { IsSuccess = true };
        var result = await _cardDetailsAppService.GetSupplementaryCardsByCivilId(civilId);
        if (result.IsSuccess)
            response.Data = await FilterSupplementaryForPayment(result?.Data!);
        else
            return response.Fail("Cards Not found");

        return response;
    }

    [HttpPost]
    public async Task<ApiResponseModel<CardPaymentResponse>> ExecuteCardPayment([FromBody] CardPaymentRequest request)
    {
        if (request.BeneficiaryCardNumber?.Length > 16)
            request.BeneficiaryCardNumber = request.BeneficiaryCardNumber.DeSaltThis();

        await ValidateBiometricStatus(request.CivilId);

        if (!authManager.HasPermission(Permissions.CardPayment.Create()))
            return Failure<CardPaymentResponse>(GlobalResources.NotAuthorized);

        var response = new ApiResponseModel<CardPaymentResponse>();

        if (request == null)
            return Failure<CardPaymentResponse>("Invalid request");

        await request.ModelValidationAsync(nameof(ExecuteCardPayment));

        var transactionLimit = await ValidateCardPaymentRequest(request, response.ValidationErrors);

        var cardRequest = await _fdrDBContext.Requests.AsNoTracking().FirstOrDefaultAsync(r => r.RequestId == request.RequestId);
        request.BeneficiaryCardNumber = cardRequest?.CardNo;


        if (transactionLimit?.CardCurrency?.IsForeignCurrency ?? false)
            await ExecuteForeignCardPayment(request, transactionLimit);
        else
            await ExecuteLocalCardPayment(request);

        return response.Success(new(), message: "Card payment completed successfully !");
    }

    [HttpPost]
    public async Task<ApiResponseModel<TransferMonetaryResponse>> TransferMonetary([FromBody] TransferMonetaryRequest request)
    {
        ApiResponseModel<TransferMonetaryResponse> response = new();

        List<TransferAccount> transferAccounts = BindTransferAccounts(request);

        var transferResponse = (await _monetaryTransferServiceClient.performMultiLegKDTransferAsync(new()
        {
            amount = transferAccounts.Select(x => x.Amount).ToArray(),
            description = transferAccounts.Select(x => $"{x.Description} for {request.ProductName}").ToArray(),
            isDebit = transferAccounts.Select(x => x.IsDebit).ToArray(),
            sevrviceName = transferAccounts.Select(x => x.ServiceName).ToArray(),
            transferAcct = transferAccounts.Select(x => x.AccountNumber).ToArray(),
        }))?.performMultiLegKDTransferResult;

        if (!(transferResponse?.isSuccessful ?? false))
            return response.Fail(message: transferResponse?.description ?? "Unable to transfer - internal error");

        return response.Success(new() { ReferenceNumber = transferResponse!.referenceNo });
    }


    [HttpPost]
    public async Task<ApiResponseModel<TransferMonetaryResponse>> ReverseMonetary([FromBody] TransferMonetaryRequest request)
    {
        ApiResponseModel<TransferMonetaryResponse> response = new();
        List<TransferAccount> accounts = BindTransferAccounts(request);
        var reverseResponse = (await _monetaryTransferServiceClient.reverseMultiLegKDTransferAsync(new()
        {
            amount = accounts.Select(x => x.Amount).ToArray(),
            description = accounts.Select(x => x.Description).ToArray(),
            isDebit = accounts.Select(x => x.IsDebit).ToArray(),
            referenceNo = request.ReferenceNumber,
            transferAcct = accounts.Select(x => x.AccountNumber).ToArray(),
        }))?.reverseMultiLegKDTransferResult;

        if (!(reverseResponse?.isSuccessful ?? false))
            response.Fail(message: reverseResponse?.description ?? "Unable to transfer - internal error");


        return response.Success(new() { ReferenceNumber = request.ReferenceNumber ?? "" });
    }


    private async Task SaveVoucher()
    {
    }

    #region Private Methods

    private async Task ValidateBiometricStatus(string civilId)
    {
        var bioStatus = await customerProfileCommonApi.GetBiometricStatus(civilId);
        if (bioStatus.ShouldStop)
            throw new ApiException(message: GlobalResources.BioMetricRestriction);
    }
    private static List<TransferAccount> BindTransferAccounts(TransferMonetaryRequest request)
    {
        string serviceName = "internal_transfer_margin";

        string creditAccountNumber = request.MarginAccount.AccountNumber;
        string marginAccountNumber = request.MarginAccount.DebitMarginAccountNumber;

        decimal debitAmount = request.MarginAccount.RemainingAmount;
        decimal marginAmount = request.MarginAccount.DebitMarginAmount;

        List<TransferAccount> accounts = new();

        if (debitAmount > 0)
        {
            accounts.Add(new(serviceName)
            {
                AccountNumber = request.DebitAccountNumber,
                Amount = (double)debitAmount,
                IsDebit = true
            });
        }

        if (marginAmount > 0)
        {
            accounts.Add(new(serviceName)
            {
                AccountNumber = marginAccountNumber,
                Amount = (double)marginAmount,
                IsDebit = true
            });
        }

        accounts.Add(new(serviceName)
        {
            AccountNumber = creditAccountNumber,
            Amount = accounts.Sum(x => x.Amount),
            IsDebit = false
        });
        return accounts;
    }
    private async Task ExecuteForeignCardPayment(CardPaymentRequest request, TransactionLimitRequest foreignCardPaymentRequest)
    {
        if (request.BeneficiaryCardNumber?.Length > 16)
            request.BeneficiaryCardNumber = request.BeneficiaryCardNumber.DeSaltThis();

        if (foreignCardPaymentRequest.CardCurrency is null)
            throw new ApiException(message: $"Unable to fetch card currency for request id {request.RequestId}");

        var currencyResponse = await _currencyAppService.ValidateCurrencyRate(new()
        {
            CivilId = request.CivilId,
            SourceAmount = request.Amount,
            SourceCurrencyCode = ConfigurationBase.KuwaitCurrency,
            ForeignCurrencyCode = foreignCardPaymentRequest.CardCurrency.CurrencyIsoCode,
            DestinationAmount = 0
        });
        if (currencyResponse == null || !currencyResponse.IsSuccess) throw new ApiException(message: currencyResponse?.Message ?? "");


        string logMessage = $"Foreign CardPayment to Beneficiary CardNumber {request.BeneficiaryCardNumber.Masked(6, 6)} from account number {request.DebitAccountNumber} for amount {request.Amount}";

        auditLogger.Log.Information("Started - {@message}", logMessage);

        var response = (await _creditCardUpdateServiceClient.performFCCreditCardPymtWithMQAsync(new()
        {
            paymentRequest = new()
            {
                cardAmount = Convert.ToDouble(currencyResponse.Data!.DestAmount),
                cardAmountSpecified = currencyResponse.Data.DestAmount > 0,
                cardNo = request.BeneficiaryCardNumber,
                custAcctNo = request.DebitAccountNumber,
                transRate = Convert.ToDouble(currencyResponse?.Data?.TransferRate),
                transRateSpecified = currencyResponse?.Data?.TransferRate > 0
            },
            TranSequence = Guid.NewGuid().ToString()
        })).performFCCreditCardPymtWithMQ;

        if (response.respCode != ConfigurationBase.IntegrationServiceSuccessCode)
        {
            logMessage = $"Failed - {logMessage} - Error {response.respMessage}";
            throw new ApiException(message: logMessage);
        }

        auditLogger.Log.Information("Success - {@message}", logMessage);

    }

    private async Task ExecuteLocalCardPayment(CardPaymentRequest request)
    {
        string logMessage = $"Foreign CardPayment to Beneficiary CardNumber {request.BeneficiaryCardNumber.Masked(6, 6)} from account number {request.DebitAccountNumber} for amount {request.Amount}";

        auditLogger.Log.Information("Started - {@message}", logMessage);

        var response =
            await _creditCardUpdateServiceClient.performCreditCardPaymentAsync(new()
            {
                amount = Convert.ToDouble(request.Amount),
                cardNo = request.BeneficiaryCardNumber,
                debitAcctNo = request.DebitAccountNumber,
                transferCurrency = request.Currency
            }
            ) ?? throw new ApiException(message: $"Failed - {logMessage}");

        auditLogger.Log.Information("Success - {@message}", logMessage);

    }

    private async Task<TransactionLimitRequest> ValidateCardPaymentRequest(CardPaymentRequest request, List<ValidationError> validationErrors)
    {

        TransactionLimitRequest transactionLimit = new();

        if (request.Amount > ConfigurationBase.MaximumCardPaymentAmount)
            validationErrors.Add(new(nameof(request.Amount), $"Payment amount must not exceed {ConfigurationBase.MaximumCardPaymentAmount} KD"));

        if (string.IsNullOrEmpty(request.BeneficiaryCardNumber) || string.IsNullOrEmpty(request.DebitAccountNumber))
            validationErrors.Add(new(nameof(request.BeneficiaryCardNumber), "Incorrect BeneficiaryCardNumber or DebitAccountNumber"));

        ThrowErrorIfAny(validationErrors);

        var debitAccount = (await _customerAccountServiceClient.viewAccountDetailsAsync(new() { acct = request.DebitAccountNumber }))?.viewAccountDetailsResult;
        if (debitAccount == null)
            validationErrors.Add(new(nameof(request.DebitAccountNumber), "Invalid Debit Account Number"));

        if (request.Amount > Convert.ToDecimal(debitAccount?.availableBalance))
            validationErrors.Add(new(nameof(request.DebitAccountNumber), "The account balance is insufficient"));

        var cardCurrency = await _currencyAppService.GetCardCurrencyByRequestId(request.RequestId!);
        string accountCurrency = debitAccount?.currency?.ToUpper() ?? "";

        if (cardCurrency is not null && !cardCurrency.IsForeignCurrency && accountCurrency != cardCurrency.CurrencyIsoCode)
            validationErrors.Add(new(nameof(request.DebitAccountNumber), "Selected card currency should match selected payment account currency"));

        var cardData = await _cardDetailsAppService.GetCardInfo(request.RequestId);
        if (!cardData.IsSuccess)
            validationErrors.Add(new(nameof(request.DebitAccountNumber), "Invalid card data"));

        bool isDebitAccountLocalCurrency = accountCurrency.Equals(ConfigurationBase.KuwaitCurrency);

        if ((cardData?.Data?.IsSupplementaryCard ?? false) && !isDebitAccountLocalCurrency)
            validationErrors.Add(new(nameof(request.DebitAccountNumber), $"Payment from {accountCurrency} accounts is not allowed for supplementary card"));

        await ClosedCardValidation();
        await PrepaidCardValidation();

        async Task ClosedCardValidation()
        {
            if (cardData?.Data?.CardStatus != CreditCardStatus.Closed) return;

            transactionLimit.AvailableLimit = cardData.Data.AvailableLimit;
            decimal dueAmount = CalculateDueAmount(cardData.Data.ApproveLimit, cardData.Data.AvailableLimit);

            if (dueAmount == 0 && cardData?.Data?.ExternalStatus?.ToUpper() == ConfigurationBase.CARD_STATUS_CLOSED)
                validationErrors.Add(new(nameof(request.Amount), "No Due Payment and Card is closed in MQ"));

        }
        async Task PrepaidCardValidation()
        {
            if (!Enum.TryParse(cardData?.Data?.Parameters?.Collateral, ignoreCase: true, out Collateral collateral))
                return;

            bool isPrepaidCard = collateral is Collateral.FOREIGN_CURRENCY_PREPAID_CARDS or Collateral.PREPAID_CARDS;
            if (!isPrepaidCard) return;

            var prepaidCardLimitResult = (await _creditCardInquiryServiceClient.getPrepaidCardsLimitAsync(new() { civilID = request.CivilId }))?.getPrepaidCardsLimit;

            if (prepaidCardLimitResult is null)
                validationErrors.Add(new("Failed to load prepaid cards limit"));

            ThrowErrorIfAny(validationErrors);

            transactionLimit.MaximumTransactionLimit = Convert.ToDecimal(prepaidCardLimitResult?.maxLimit - prepaidCardLimitResult?.availableLimit);
            if (request.Amount > transactionLimit.MaximumTransactionLimit)
                validationErrors.Add(new(nameof(request.Amount), $"You exceed the maximum allowed limit {transactionLimit.MaximumTransactionLimit}"));

            ThrowErrorIfAny(validationErrors);
        }

        ThrowErrorIfAny(validationErrors);

        return transactionLimit with { CardCurrency = cardCurrency };
    }

    private record TransactionLimitRequest
    {
        public decimal AvailableLimit { get; set; } = 0;
        public decimal MaximumTransactionLimit { get; set; } = 0;
        public CardCurrencyDto? CardCurrency { get; set; }
    }


    private void ThrowErrorIfAny(List<ValidationError> validationErrors)
    {
        if (validationErrors.Any()) throw new ApiException(validationErrors, nameof(ValidateCardPaymentRequest), "validation failed");
    }

    private decimal CalculateDueAmount(decimal approvedLimit, decimal availableLimit)
    {
        decimal dueAmount = approvedLimit - availableLimit;
        return dueAmount <= 0 ? 0 : dueAmount;
    }
    private async Task<bool> PerformForeignCurrencyPayment(string issuingOption, int cardType, string accountNumber, CreditCardStatus cardStatus, string cardNumber)
    {
        var currencyData = await ValidateSufficientFundForForeignCurrencyCards(issuingOption, cardType, accountNumber, cardStatus);
        if (currencyData is null) return false;

        var paymentResponse = (await _creditCardUpdateServiceClient.performFCCreditCardPymtWithMQAsync(new()
        {
            paymentRequest = new()
            {
                cardNo = cardNumber,
                cardAmount = Convert.ToDouble(currencyData.DestAmount),
                custAcctNo = accountNumber,
                transRate = Convert.ToDouble(currencyData.TransferRate)
            }
        }))?.performFCCreditCardPymtWithMQ;

        var paymentResponseCode = paymentResponse?.respCode ?? string.Empty;

        if (paymentResponseCode != "0000")
            throw new ApiException(errors: null, message: $"Application Successfully Updated, but an error happened when Card loaded with the minimum opening balance, please contact with system administrator");

        return true;
    }
    private async Task<ValidateCurrencyResponse?> ValidateSufficientFundForForeignCurrencyCards(string issuingOption, int cardType, string accountNumber, CreditCardStatus cardStatus)
    {
        var minBalanceFc = Convert.ToDecimal((await _fdrDBContext.ConfigParameters.AsNoTracking().FirstOrDefaultAsync(x => x.ParamName == ConfigurationBase.MinimumBalanceFC))?.ParamValue);

        if (!(cardStatus == CreditCardStatus.Approved && issuingOption == Collateral.FOREIGN_CURRENCY_PREPAID_CARDS.ToString() && minBalanceFc > 0))
            return null;

        CardCurrencyDto cardCurrency = await _currencyAppService.GetCardCurrency(cardType);
        ValidateCurrencyResponse? currencyData = await _currencyAppService.GetCurrencyRate(cardCurrency.CurrencyIsoCode);
        if (currencyData == null) return null;
        currencyData.AccountOpeningMinBalance = minBalanceFc + ((decimal)currencyData.KdTransProfit);

        AccountDetailsDTO account = ((await _customerAccountServiceClient.viewAccountDetailsAsync(new()
        {
            acct = accountNumber
        }))?.viewAccountDetailsResult) ?? throw new ApiException(errors: null, message: "Invalid Debit Account number");

        if (Convert.ToDecimal(account.availableBalance) < currencyData.AccountOpeningMinBalance)
            throw new ApiException(errors: null, message: $"Insufficient funds in the customer debit account - {currencyData.AccountOpeningMinBalance}");

        return currencyData;

    }

    private async Task<List<OwnedCreditCardsResponse>> FilterOwnCardForPayment(List<CreditCardResponse> creditCards)
    {
        var filteredCardPaymentCards = creditCards.AsQueryable()
            .Where(x => x.CardNo != null && x.CardStatus != (int)CreditCardStatus.Lost
            && x.CardStatus != (int)CreditCardStatus.Pending
            && x.CardStatus != (int)CreditCardStatus.PendingForCreditCheckingReview
            && x.CardStatus != (int)CreditCardStatus.CreditCheckingReviewed
            && x.CardStatus != (int)CreditCardStatus.Approved).ToList();

        if (filteredCardPaymentCards.First()?.CardNo == null)
            return new();

        //TODO: Show credit card number based on permission
        var OwnedCardPaymentCards = (from fc in filteredCardPaymentCards
                                     join ct in _fdrDBContext.CardDefs.AsNoTracking() on fc.CardType equals ct.CardType
                                     join def in _fdrDBContext.CardDefExts.AsNoTracking() on ct.CardType equals def.CardType
                                     where def.Attribute.ToUpper() == ConfigurationBase.CurrencyConfigCode
                                     join crncy in _fdrDBContext.CardCurrencies.AsNoTracking() on def.Value equals crncy.Org into _crncyDef
                                     from crncyDef in _crncyDef.DefaultIfEmpty()
                                     select new OwnedCreditCardsResponse()
                                     {
                                         CardNumber = fc.CardNo,
                                         CardStatus = (CreditCardStatus)fc.CardStatus,
                                         ProductName = ct.Name,
                                         CardType = fc.CardType,
                                         RequestId = fc.RequestID,
                                         ApprovedLimit = fc.ApprovedLimit,
                                         DebitAccountNumber = fc.BankAcctNo,
                                         CurrencyISOCode = crncyDef == null ? ConfigurationBase.KuwaitCurrency : crncyDef.CurrencyIsoCode
                                     }
                              ).ToList();

        return OwnedCardPaymentCards;
    }
    private async Task<List<SupplementaryCardDetail>> FilterSupplementaryForPayment(List<SupplementaryCardDetail> cards)
    {

        var filteredCardPaymentCards = cards.Where(x => x.TypeId != ConfigurationBase.PrimaryChargeCardPayeeTypeId);
        return filteredCardPaymentCards.ToList();
    }
    #endregion


}
