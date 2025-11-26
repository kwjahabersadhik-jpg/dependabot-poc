using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Models.Access;
using CreditCardsSystem.Domain.Models.Card;
using CreditCardsSystem.Domain.Models.CardIssuance;
using CreditCardsSystem.Domain.Models.CardOperation;
using CreditCardsSystem.Domain.Models.SupplementaryCard;
using CreditCardsSystem.Domain.Models.Workflow;
using CreditCardsSystem.Domain.Shared.Interfaces;
using CreditCardsSystem.Domain.Shared.Models.Reports;
using CreditCardsSystem.Domain.Shared.Models.RequestActivity;
using CreditCardsSystem.Utility.Extensions;
using CreditCardUpdateServiceReference;
using Kfh.Aurora.Auth;
using Kfh.Aurora.Integration;
using Microsoft.Extensions.Logging;
using MonetaryTransferServiceReference;
using System.Data;
namespace CreditCardsSystem.Application.CardOperations;


public class ClosureAppService(
    IIntegrationUtility integrationUtility,
    IOptions<IntegrationOptions> options,
    IRequestActivityAppService _requestActivityAppService,
    IPreRegisteredPayeeAppService _preRegisteredPayeeAppService,
    ICustomerProfileAppService _customerProfileAppService,
    ICardDetailsAppService _cardDetailsAppService,
    IAccountsAppService _accountsAppService,
    IFeesAppService _feesAppService,
    IStandingOrderAppService _standingOrderAppService,
    IPctAppService _pctAppService,
    ICardPaymentAppService _cardPaymentAppService,
    IAuthManager authManager,
    IConfigurationAppService _configurationAppService,
    ILogger<ClosureAppService> _logger,
    ICreditReverseAppService _creditReverseAppService,
    ICurrencyAppService _currencyAppService) : BaseApiResponse, IClosureAppService, IAppService
{
    #region Private Fields
    private readonly MonetaryTransferServiceClient _monetaryTransferServiceClient = integrationUtility.GetClient<MonetaryTransferServiceClient>(options.Value.Client, options.Value.Endpoints.MonetaryTransfer, options.Value.BypassSslValidation);
    private readonly CreditCardUpdateServicesServiceClient _creditCardUpdateServiceClient = integrationUtility.GetClient<CreditCardUpdateServicesServiceClient>(options.Value.Client, options.Value.Endpoints.CreditCardUpdate, options.Value.BypassSslValidation);

    //private readonly IRequestActivityAppService _requestActivityAppService = requestActivityAppService;
    //private readonly IPreRegisteredPayeeAppService _preRegisteredPayeeAppService = preRegisteredPayeeAppService;
    //private readonly ICustomerProfileAppService _customerProfileAppService = customerProfileAppService;
    //private readonly ICardDetailsAppService _cardDetailsAppService = cardDetailsAppService;
    //private readonly IAccountsAppService _accountsAppService = accountsAppService;
    //private readonly IFeesAppService _feesAppService = feesAppService;
    //private readonly IStandingOrderAppService _standingOrderAppService = standingOrderAppService;
    //private readonly IPctAppService _pctAppService = pctAppService;
    //private readonly ICardPaymentAppService _cardPaymentAppService = cardPaymentAppService;
    //private readonly IAuthManager authManager = authManager;
    //private readonly IConfigurationAppService _configurationAppService = configurationAppService;
    //private readonly ILogger<ClosureAppService> _logger = logger;
    //private readonly ICreditReverseAppService _creditReverseAppService = creditReverseAppService;
    //private readonly ICurrencyAppService _currencyAppService = currencyAppService;

    #endregion


    #region public methods
    [HttpPost]
    public async Task<ApiResponseModel<ValidateCardClosureResponse>> GetCardClosureRequestFormData([FromBody] CardClosureRequest request, bool skipPendingRequetCheck = false)
    {
        var cardInfo = request.CardInfo ?? (await _cardDetailsAppService.GetCardInfo(request.RequestId, includeCardBalance: true))?.Data ?? throw new ApiException(message: "Invalid request Id");
        var userBranch = await _configurationAppService.GetUserBranch();
        request.BranchId = userBranch.BranchId;

        if (request.IncludeValidation)
        {
            if (skipPendingRequetCheck == false)
            {
                //Checking Pending Request Activity
                await CheckingPendingRequestActivity(request, cardInfo);
            }
            //Checking Standing Order
            await CheckingStandingOrder(cardInfo);
        }

        List<SupplementaryCardDetail>? supplementaryCards = null;
        List<AccountDetailsDto>? debitAccounts = null;
        #region Supplementary Card Validations
        if (cardInfo.IsPrimaryCard)
        {
            if (!await CanDeleteSupplementaryCards())
                throw new ApiException(message: "This supplementary card cannot be closed");

            //supplementaryCards = (await _cardDetailsAppService.GetSupplementaryCardsByRequestId(cardInfo.RequestId))?.Data;
            //if (supplementaryCards?.Count == 0 && cardInfo.IsSupplementaryCard)
            //{
            //    //if (cardInfo.ProductType == ProductTypes.ChargeCard)
            //    //    return Success(new ValidateCardClosureResponse() { CardCategory = CardCategoryType.Supplementary });

            //    if (!await CanDeleteSupplementaryCards())
            //        throw new ApiException(message: "This supplementary card cannot be closed");

            //    //return Success(new ValidateCardClosureResponse() { CardCategory = CardCategoryType.Supplementary });
            //}
        }
        #endregion

        //below logic for primary cards
        #region Primary Card Validations

        if (string.IsNullOrEmpty(request.AccountNumber))
            throw new ApiException(message: "Unable to close this cardas it is not linked to account!");

        var cardDef = await _cardDetailsAppService.GetCardWithExtension(cardInfo!.CardType) ?? throw new ApiException(message: "Unable to fetch card info");
        bool isKfhStaffId = cardInfo.KfhStaff?.Trim() != "";
        decimal feeAmount = await GetFeeAmount();
        decimal balance = cardInfo.ApproveLimit - cardInfo.AvailableLimit;

        decimal totalFee = 0;
        decimal totalAmount = 0;
        decimal originalFee = 0;
        decimal vatAmount = 0;
        bool isHavingInsufficientBalance = false;

        decimal? balanceInKWD = null;
        decimal? totalAmountInKWD=null;


        if (feeAmount > 0)
            (originalFee, totalFee) = await GetTotalFee();

        if (balance != 0 || totalFee != 0)
        {
            totalAmount = totalFee + balance;
            isHavingInsufficientBalance = true;
            debitAccounts = (await _accountsAppService.GetDebitAccounts(cardInfo.CivilId!))?.Data;
        }
        #endregion

        if (cardInfo.Currency.CurrencyIsoCode != ConfigurationBase.KuwaitCurrency)
        {
            balanceInKWD = await GetCurrencyRate(balance);
            totalAmountInKWD = await GetCurrencyRate(totalAmount);
        }


        return Success(new ValidateCardClosureResponse()
        {
            OriginalFee = originalFee,
            FeeAmount = feeAmount,
            VATAmount = vatAmount,
            Balance = balance,
            BalanceInKWD = balanceInKWD,
            SupplementaryCards = supplementaryCards,
            TotalFee = totalFee,
            TotalAmount = totalAmount,
            TotalAmountInKWD = totalAmountInKWD,
            DebitAccounts = debitAccounts,
            IsHavingInsufficientBalance = isHavingInsufficientBalance,
            CardCategory = cardInfo.IsPrimaryCard ? CardCategoryType.Primary : CardCategoryType.Normal
        });

        #region local functions
        async Task<decimal> GetCurrencyRate(decimal amount)
        {
            if (cardInfo.Currency.CurrencyIsoCode == ConfigurationBase.KuwaitCurrency)
                return 0;

            amount = amount < 0 ? amount * -1 : amount;
            var currencyRateResponse = await _currencyAppService.ValidateCurrencyRate(new()
            {
                CivilId = cardInfo?.CivilId!,
                ForeignCurrencyCode = cardInfo.Currency.CurrencyIsoCode,
                SourceAmount = amount,
                DestinationAmount = 0,
            });

            if (!currencyRateResponse.IsSuccess)
            {
                throw new ApiException(message: "Unable to fetch currency rate!");
            }

            var CurrencyTransferData = currencyRateResponse?.Data!;

            return CurrencyTransferData?.DestAmount ?? 0;
        }
        async Task<decimal> GetFeeAmount()
        {
            if (cardInfo.ProductType != ProductTypes.Tayseer)
                return await GetEarlyClosureFee();

            //this can be removed if the tayseer identified by duality
            if (decimal.TryParse(cardDef?.Extension?.NonFundableYearlyFees, out decimal _nonFundableYearlyFees) && _nonFundableYearlyFees > 0)
                return GetDeductedAmountForT12Cards(cardInfo.OpenDate, cardDef, isKfhStaffId);

            return 0;
        }

        async Task<(decimal OriginalFee, decimal TotalFee)> GetTotalFee()
        {
            string serviceName = GetServiceName(cardInfo!.CardNumber, cardInfo.ProductType);

            var feesDataResponse = await _feesAppService.GetServiceFee(new()
            {
                ServiceName = serviceName,
                DebitAccountNumber = request.AccountNumber
            });

            if (!feesDataResponse.IsSuccess)
                throw new ApiException(message: feesDataResponse.Message);

            var feesData = feesDataResponse?.Data!;
            vatAmount = feesData.IsVatApplicable ? feeAmount * feesData.VatPercentage / 100 : 0;
            vatAmount = Math.Round(vatAmount, 3, MidpointRounding.AwayFromZero);
            decimal totalFee = Math.Round((feeAmount + vatAmount), 3, MidpointRounding.AwayFromZero);
            return (feesData.Fees, totalFee);
        }
        async Task<bool> CanDeleteSupplementaryCards()
        {

            supplementaryCards = (await _cardDetailsAppService.GetSupplementaryCardsByRequestId(cardInfo.RequestId) ?? throw new ApiException(message: "Unable to fetch supplementary info"))?.Data;

            if (supplementaryCards.Any(card => card.CardType != ConfigurationBase.AlOsraSupplementaryCardTypeId.ToString()))
                return true;

            int totalCards = supplementaryCards?.Count ?? 0;


            if (totalCards == 0)
                return true;

            int totalRejectedCards = supplementaryCards?.Count(x => x.CardStatus == CreditCardStatus.Rejected) ?? 0;
            int remainingCards = totalCards - totalRejectedCards;//   Cards.Data.Any(x => x.CardStatus != CreditCardStatus.Closed);
            return remainingCards > 1;
        }

        async Task<decimal> GetEarlyClosureFee()
        {
            long remainingMonths = Helpers.GetRemainMonths(cardInfo.OpenDate);
            _ = decimal.TryParse(cardInfo.EarlyClosureFees, out decimal _earlyClosureFees);
            _ = decimal.TryParse(cardInfo.EarlyClosureMonths, out decimal _earlyClosureMonths);
            _ = decimal.TryParse(cardInfo.EarlyClosurePercentage, out decimal _earlyClosurePercentage);
            _ = int.TryParse(cardInfo.PCTId, out int _pctId);


            if (_earlyClosureMonths == 0 && _pctId != 0)
            {
                var pct = await _pctAppService.GetPctById(_pctId);
                if (pct.IsSuccessWithData)
                {
                    _earlyClosureMonths = pct.Data!.EarlyClosureMonths;
                    _earlyClosureFees = pct.Data!.EarlyClosureFees;
                    _earlyClosurePercentage = pct.Data!.EarlyClosurePercentage;
                }
            }

            if (remainingMonths != 0 && remainingMonths <= _earlyClosureMonths)
            {
                decimal earlyFeeAmount = _earlyClosurePercentage * _earlyClosureFees / 100;
                return Math.Round(earlyFeeAmount, 3, MidpointRounding.AwayFromZero);
            }

            return 0;
        }

        async Task CheckingPendingRequestActivity(CardClosureRequest request, CardDetailsResponse cardInfo)
        {
            var isHavingPendingActivity = (await _requestActivityAppService.SearchActivity(new()
            {
                RequestId = request.RequestId,
                CivilId = cardInfo.CivilId,
                CardNumber = cardInfo.CardNumber,
                CfuActivityId = (int)CFUActivity.Card_Closure,
                RequestActivityStatusId = (int)RequestActivityStatus.Pending,
                IssuanceTypeId = (int)cardInfo.IssuanceType
            }))?.Data?.Any() ?? false;

            if (isHavingPendingActivity)
                throw new ApiException(message: GlobalResources.RequestAlreadySent);
        }

        async Task CheckingStandingOrder(CardDetailsResponse cardInfo)
        {
            var standingOrders = (await _standingOrderAppService.GetAllStandingOrders(cardInfo.CivilId))?.Data;
            if (standingOrders?.Any(x => x.Description == cardInfo.CardNumber) ?? false)
                throw new ApiException(message: "Sorry, to close this card you have first to close the standing order");
        }
        #endregion
    }




    [HttpPost]
    public async Task<ApiResponseModel<List<CardActivationStatus>>> RequestCardClosure([FromBody] CardClosureRequest request)
    {
        bool isAuthorized = authManager.HasPermission(Permissions.PrimaryCardClosure.Request()) || authManager.HasPermission(Permissions.SecondaryCardClosure.Request());
        if (!isAuthorized)
            return Failure<List<CardActivationStatus>>(GlobalResources.NotAuthorized);


        var cardInfo = (await _cardDetailsAppService.GetCardInfo(request.RequestId, includeCardBalance: true))?.Data ?? throw new ApiException(message: "Invalid request Id");
        request.CardInfo = cardInfo;

        var validationResponse = await GetCardClosureRequestFormData(request) ?? throw new ApiException(message: "unable to validate your request");
        if (!validationResponse.IsSuccess)
            throw new ApiException(message: validationResponse.Message);

        bool IsInvalidAccountToClearBalanceAmount = validationResponse!.Data!.CardCategory == CardCategoryType.Primary && (validationResponse.Data.TotalAmount > 0 && !validationResponse!.Data!.DebitAccounts.AnyWithNull(x => x.Acct == request.AccountNumber));

        if (IsInvalidAccountToClearBalanceAmount)
            throw new ApiException(message: "invalid debit account");


        //var accountData = (await _accountsAppService.GetDebitAccountsByAccountNumber(request.AccountNumber) ?? throw new ApiException(message: "Unable to fetch account data"))?.Data?[0];
        var customerProfile = (await _customerProfileAppService.GetCustomerProfile(cardInfo!.CivilId) ?? throw new ApiException(message: "Unable to fetch card info")).Data;
        bool isKfhStaffId = customerProfile?.EmployeeNumber?.Trim() != "";


        string cardCategory = cardInfo.IsPrimaryCard ? "P" : "";
        cardCategory = (cardCategory != "" && cardInfo.IsSupplementaryCard) ? "S" : "";

        var userBranch = await _configurationAppService.GetUserBranch();

        var requestActivity = new RequestActivityDto()
        {
            IssuanceTypeId = (int)cardInfo.IssuanceType,
            CardType = cardInfo.CardType,
            CardNumber = cardInfo.CardNumber,
            BranchId = userBranch.BranchId,
            CivilId = cardInfo.CivilId,
            RequestId = cardInfo.RequestId,
            CustomerName = $"{customerProfile!.FirstName} {customerProfile.LastName}",
            CfuActivityId = (int)CFUActivity.Card_Closure,
            Details = new() {
            { ReportingConstants.KEY_CREDIT_CARD_NO,cardInfo.CardNumber!},
            { ReportingConstants.KEY_IS_KFH_STAFF,isKfhStaffId.ToString()},
            },
            WorkflowVariables = new() {
                { "Description", $"Request Card closure for {customerProfile!.FirstName} {customerProfile.LastName} " },
                { WorkflowVariables.CardNumber,cardInfo.CardNumberDto!},
                { WorkflowVariables.AccountNumber,request.AccountNumber},
                { WorkflowVariables.IsKFHStaff,isKfhStaffId?"Yes":"False"},
                { WorkflowVariables.CardCategory,cardCategory}
            }
        };
 

        if (validationResponse.Data.Balance <= 0)
        {
            requestActivity.WorkflowVariables.Add("Excess Amount", (validationResponse.Data.Balance * -1).ToMoney(cardInfo.Currency.CurrencyIsoCode));
            if (validationResponse.Data.BalanceInKWD != 0)
            {
                requestActivity.WorkflowVariables.Add("Excess Amount In KWD",  (validationResponse.Data.BalanceInKWD).ToMoney(cardInfo.Currency.CurrencyIsoCode));
            }

            //create credit reverse workflow for excess amount
            var creditReverseWorkflow = await _creditReverseAppService.RequestCreditReverse(new()
            {
                DebitAccountNumber = request.AccountNumber,
                CardInfo = cardInfo,
                CivilId = cardInfo.CivilId,
                BeneficiaryCardNumber = cardInfo.CardNumber,
                RequestId = cardInfo.RequestId,
                ExternalStatus = cardInfo.ExternalStatus,
                BranchNumber = cardInfo.BranchId,
                Amount = validationResponse.Data.Balance * -1
            });
        }
        else
        {
            requestActivity.WorkflowVariables.Add("Deduction Amount", validationResponse.Data.TotalAmount.ToMoney(cardInfo.Currency.CurrencyIsoCode));
            if (validationResponse.Data.BalanceInKWD != 0)
            {
                requestActivity.WorkflowVariables.Add("Deduction Amount In KWD", validationResponse.Data.TotalAmountInKWD * 1);
            }
        }

        requestActivity.RequestActivityId = await _requestActivityAppService.LogRequestActivity(requestActivity, isNeedWorkflow: true);
        return Success(new List<CardActivationStatus>() { new() {
            CardNumber = cardInfo!.CardNumber!,
            IsActivated = true,
            Message = GlobalResources.WaitingForApproval }
        }, message: $"{GlobalResources.WaitingForApproval} - Workflow created for Credit reverse for excess amount {validationResponse.Data.Balance * -1}");

    }


    [HttpPost]
    public async Task<ApiResponseModel<ProcessResponse>> ProcessCardClosureRequest([FromBody] ProcessCardClosureRequest request)
    {
        bool isAuthorized = authManager.HasPermission(Permissions.PrimaryCardClosure.EnigmaApprove()) || authManager.HasPermission(Permissions.SecondaryCardClosure.EnigmaApprove());
        if (!isAuthorized)
            return Failure<ProcessResponse>($"{GlobalResources.NotAuthorized} for {Permissions.PrimaryCardClosure} / {Permissions.SecondaryCardClosure} ");

        //var requestActivity = (await _requestActivityAppService.GetAllRequestActivity(new() { RequestActivityID = request.RequestActivityId })).Data?.Single() ?? throw new ApiException(message: "Unable to find Request");
        var requestActivity = (await _requestActivityAppService.GetRequestActivityById(request.RequestActivityId)).Data ?? throw new ApiException(message: "Unable to find Request");
        //var cardInfo = (await _cardDetailsAppService.GetCardInfoMinimal(requestActivity.RequestId))?.Data ?? throw new ApiException(message: "Invalid request Id");
        var cardInfo = (await _cardDetailsAppService.GetCardInfo(requestActivity.RequestId, includeCardBalance: true))?.Data ?? throw new ApiException(message: "Invalid request Id");

        request.CardNumber = cardInfo.CardNumber.Masked(6, 6);
        request.Activity = CFUActivity.Card_Closure;

        if (cardInfo.IsPrimaryCard && !authManager.HasPermission(Permissions.PrimaryCardClosure.EnigmaApprove()))
        {
            return Failure<ProcessResponse>($"{GlobalResources.NotAuthorized} for {Permissions.PrimaryCardClosure}");
        }

        if (cardInfo.IsSupplementaryCard && !authManager.HasPermission(Permissions.SecondaryCardClosure.EnigmaApprove()))
        {
            return Failure<ProcessResponse>($"{GlobalResources.NotAuthorized} for {Permissions.SecondaryCardClosure}");
        }


        if (request.ActionType == ActionType.Rejected)
        {
            await _requestActivityAppService.CompleteActivity(request);
            return Success(new ProcessResponse() { CardNumber = "" }, message: "Successfully Rejected");
        }


        return await Approve();

        #region local functions
        async Task<ApiResponseModel<ProcessResponse>> Approve()
        {

            //preparing approval data
            var preApprovableData = await ApprovalValidation();

            //Clearing card usage due amount
            await ClearDueAmountIfAny(preApprovableData!.Balance);


            //charging subscription fee due amount
            var transRefnumber = await ChargeSubscriptionFeeIfAny(preApprovableData!.FeeAmount, preApprovableData!.OriginalFee);

            if (cardInfo.IsPrimaryCard)
            {
                //Closing all supplementary cards first before closing primary card
                //Refund the subscription fee due amount if the primary card closure is failed
                var failedCards = await CloseWithSupplementaryIfAny();
                if (failedCards.Count != 0)
                {
                    return Failure<ProcessResponse>(message: "Failed", validationErrors: failedCards.Select(x => new ValidationError(x.CardNumber, x.Message)).ToList());
                }
            }
            else
            {
                var primaryCardResponse = await CloseCreditCard(cardInfo.CardNumber!, isSupplementary: false);
                if (!primaryCardResponse.IsSuccess)
                {
                    //Refund the subscription fee due amount if the normal card closure is failed
                    await ReverseSubscriptionFee(transRefnumber);
                    return Failure<ProcessResponse>(message: "Failed", validationErrors: new() { primaryCardResponse.ValidationErrors[0] });
                }
            }

            //TODO : SEQ Log

            //await _requestActivityAppService.UpdateRequestActivityStatus(new() { IssuanceTypeId = (int)cardInfo.ProductType, CivilId = cardInfo.CivilId, RequestActivityId = (int)request.RequestActivityId, RequestActivityStatusId = (int)RequestActivityStatus.Approved });


            await _requestActivityAppService.CompleteActivity(request);

            return Success<ProcessResponse>(new() { CardNumber = cardInfo!.CardNumber!, Message = "Approved and successfully closed cards!" });

            /// Filter non-closed supplementary cards
            #region local functions
            async Task<ValidateCardClosureResponse> ApprovalValidation()
            {
                if (cardInfo.CardStatus == CreditCardStatus.Closed)
                    throw new ApiException(message: "Card is already closed");

                var validationResponse = await GetCardClosureRequestFormData(new()
                {
                    CardInfo = cardInfo,
                    BranchId = (int)requestActivity!.BranchId!,
                    RequestId = requestActivity!.RequestId!,
                    AccountNumber = request.AccountNumber
                }, true) ?? throw new ApiException(message: "unable to validate your request");

                if (!validationResponse.IsSuccess)
                    throw new ApiException(message: validationResponse.Message);

                return validationResponse.Data!;
            }

            /// <summary>

            /// </summary>
            async Task<List<CardClosureResponse>> CloseWithSupplementaryIfAny()
            {
                if (preApprovableData.SupplementaryCards?.Count == 0) return [];

                string primaryCardNumber = cardInfo.CardNumber!;

                List<Task<ApiResponseModel<CardClosureResponse>>> tasks = [];

                await Task.Run(() =>
                {
                    foreach (var supplementaryCard in preApprovableData.SupplementaryCards!.Where(x => x.CardStatus != CreditCardStatus.Closed))
                        tasks.Add(CloseCreditCard(supplementaryCard.CardNumber!, isNew: true));
                });


                var result = await Task.WhenAll(tasks);
                var closedCards = result.Where(x => x.IsSuccessWithData && x.Data?.IsClosed == true).ToList();
                var failedCards = result.Where(x => x.IsSuccess == false).ToList();

                if (failedCards.Count == 0)
                {
                    //Primary Card
                    var primaryCardClosureResponse = await CloseCreditCard(primaryCardNumber, isSupplementary: false);
                    if (!primaryCardClosureResponse.IsSuccess)
                    {
                        //Refund the subscription fee due amount if the primary card closure is failed
                        await ReverseSubscriptionFee(transRefnumber);
                        failedCards.Add(primaryCardClosureResponse);
                    }
                }

                return failedCards.Select(x => new CardClosureResponse()
                {
                    IsClosed = false,
                    CardNumber = x.ValidationErrors[0].Property,
                    Message = x.ValidationErrors[0].Error
                }).ToList();
            }

            //Charging card closure yearly subscription fee in advance if they closing before 12 months
            async Task<string> ChargeSubscriptionFeeIfAny(decimal fee, decimal originalFee)
            {
                if (fee == 0)
                    return string.Empty;

                var postServiceResponse = await _feesAppService.PostServiceFee(new()
                {
                    DebitAccountNumber = request.AccountNumber,
                    ServiceName = GetServiceName(cardInfo.CardNumber, cardInfo.ProductType),
                    OverwriteFeesAmount = fee,
                    OriginalFeesAmount = originalFee,
                    OverwriteReason = "Credit Card Fee",//TODO: need to remove hardcoded value 
                    OverwriteFeesAmountSpecified = true,
                    OriginalFeesAmountSpecified = true
                });

                if (!postServiceResponse.IsSuccess)
                    throw new ApiException(errors: postServiceResponse.ValidationErrors, message: postServiceResponse.Message, insertSeriLog: true);

                return postServiceResponse.Data?.TransRefNumber ?? string.Empty;
            }
            async Task ClearDueAmountIfAny(decimal dueAmount)
            {
                if (dueAmount <= 0)
                {
                    return;
                }

                var paymentResponse = await _cardPaymentAppService.ExecuteCardPayment(new()
                {
                    CivilId = requestActivity.CivilId,
                    RequestId = requestActivity.RequestId,
                    BranchNumber = requestActivity?.BranchId ?? 0,
                    BeneficiaryCardNumber = requestActivity.CardNumber,
                    Amount = dueAmount,
                    DebitAccountNumber = request.AccountNumber,
                    Currency = ConfigurationBase.KuwaitCurrency
                });

                if (!paymentResponse.IsSuccess)
                    throw new ApiException(errors: paymentResponse.ValidationErrors, message: paymentResponse.Message, insertSeriLog: true);
            }
            #endregion
        }
        #endregion
    }

    [HttpGet]
    public async Task<ApiResponseModel<CardClosureResponse>> CloseCreditCard(string cardNumber, bool isSupplementary = true, bool isNew = false)
    {
        try
        {
            var cardInfo = (await _cardDetailsAppService.GetCardInfo(null, cardNumber: cardNumber) ?? throw new ApiException(message: "Unable to fetch card info"))?.Data;

            if (cardInfo!.CardStatus == CreditCardStatus.Closed)
                return Failure<CardClosureResponse>("Supplementary card number doesn't have valid request", new List<ValidationError>() { new(cardNumber.ToString(), "Already Closed") });


            var userBranch = await _configurationAppService.GetUserBranch();

            RequestActivityDto requestActivity = new()
            {
                IssuanceTypeId = (int)cardInfo.IssuanceType,
                CivilId = cardInfo.CivilId,
                BranchId = userBranch.BranchId,
                RequestId = cardInfo.RequestId,
                CfuActivityId = (int)CFUActivity.Card_Closure,
                RequestActivityStatusId = (int)(isNew ? RequestActivityStatus.New : RequestActivityStatus.Pending),
                CardNumberDto = cardInfo.CardNumberDto
            };

            await _requestActivityAppService.LogRequestActivity(requestActivity, true);

            await ApproveCardClosure(cardInfo, isSupplementary);
        }
        catch (System.Exception ex)
        {
            return Failure<CardClosureResponse>(ex.Message, new List<ValidationError>() { new(cardNumber.ToString(), ex.Message) });
        }

        return Success<CardClosureResponse>(new() { IsClosed = true, CardNumber = cardNumber });
    }
    #endregion


    #region private methods
    private decimal GetDeductedAmountForT12Cards(DateTime openDate, CardDefinitionDto oldCardDef, bool isKFHStaff = false)
    {
        long remainingMonths = Helpers.GetRemainMonths(openDate);

        decimal deductedAmount = 0;

        if (!isKFHStaff)
            deductedAmount = oldCardDef.Fees ?? 0;
        else if (Int32.TryParse(oldCardDef!.Extension?.NonFundableYearlyFees, out Int32 _nonFundableYearlyFees) && _nonFundableYearlyFees > 0)
            deductedAmount = _nonFundableYearlyFees;

        return (deductedAmount / 12) * remainingMonths;
    }

    private async Task ApproveCardClosure(CardDetailsResponse cardRequest, bool isSupplementary = true)
    {

        //TODO: handle in-complete updates
        var mqResponse = await _creditCardUpdateServiceClient.reportCreditCardStatusAsync(new()
        {
            cardNo = cardRequest.CardNumber,
            cardStatus = ConfigurationBase.MQ_CARD_STATUS_CLOSED,
            requestId = "",
            updateType = ""
        });


        if (!isSupplementary) return;

        var isSuccess = await _preRegisteredPayeeAppService.UpdatePreregisteredPayee(cardRequest);
        if (!isSuccess)
            throw new ApiException(message: "Unable to find payee detail to update status");
    }


    //Refund the subscription fee due amount
    private async Task<bool> ReverseSubscriptionFee(string originalRefNo)
    {
        if (string.IsNullOrEmpty(originalRefNo)) return true;

        var result = (await _monetaryTransferServiceClient.reverseGenericTransactionAsync(new() { originalRefNo = originalRefNo }))?.reverseGenericTransactionResult;
        //TODO : Log error
        if (result?.isSuccessful == false)
        {
            _logger.LogInformation(result.description);
        }

        return result?.isSuccessful ?? false;
    }

    private string GetServiceName(string cardNumber, ProductTypes productType)
    {
        bool isVisaCard = cardNumber!.StartsWith(ConfigurationBase.VisaCardStartingNumbers);

        string serviceName = productType == ProductTypes.Tayseer ? ConfigurationBase.TayseerCardFees : (isVisaCard ? ConfigurationBase.VisaCardFees : ConfigurationBase.MasterCardFees);

        return serviceName;
    }
    #endregion
}
