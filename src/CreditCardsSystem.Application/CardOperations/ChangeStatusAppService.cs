using BankingCustomerProfileReference;
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
using CreditCardsSystem.Utility.Extensions;
using CreditCardTransactionInquiryServiceReference;
using CreditCardUpdateProfileService;
using CreditCardUpdateServiceReference;
using CustomerAccountsServiceReference;
using Kfh.Aurora.Auth;
using Kfh.Aurora.Integration;
using Kfh.Aurora.Logging;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace CreditCardsSystem.Application.CardOperations;

public class ChangeStatusAppService : BaseApiResponse, IChangeStatusAppService, IAppService
{
    #region Private Fields
    private readonly CreditCardUpdateServicesServiceClient _creditCardUpdateServiceClient;
    private readonly CustomerAccountsServiceClient _customerAccountServiceClient;
    private readonly CorporateCreditCardServiceClient _corporateCreditCardServiceClient;
    private readonly BankingCustomerProfileServiceClient _bankingCustomerProfileServiceClient;
    private readonly CreditCardUpdateProfileServicesServiceClient _creditCardUpdateProfileServiceClient;

    private readonly FdrDBContext _fdrDBContext;
    private readonly ICurrencyAppService _currencyAppService;
    private readonly ICardDetailsAppService _cardDetailsAppService;
    private readonly IRequestActivityAppService _requestActivityAppService;
    private readonly ICustomerProfileAppService _customerProfileAppService;
    private readonly IAuthManager _authManager;
    private readonly IAuditLogger<ChangeStatusAppService> auditLogger;
    private readonly IRequestAppService _requestAppService;



    public ChangeStatusAppService(IIntegrationUtility integrationUtility,
                                  IOptions<IntegrationOptions> options,
                                  FdrDBContext fdrDBContext,
                                  ICurrencyAppService currencyAppService,
                                  ICardDetailsAppService cardDetailsAppService,
                                  IRequestActivityAppService requestActivityAppService,
                                  ICustomerProfileAppService customerProfileAppService,
                                  IAuthManager authManager,
                                  IAuditLogger<ChangeStatusAppService> auditLogger,
                                  IRequestAppService requestAppService)
    {
        _creditCardUpdateServiceClient = integrationUtility.GetClient<CreditCardUpdateServicesServiceClient>(options.Value.Client, options.Value.Endpoints.CreditCardUpdate, options.Value.BypassSslValidation);
        _customerAccountServiceClient = integrationUtility.GetClient<CustomerAccountsServiceClient>(options.Value.Client, options.Value.Endpoints.CustomerAccount, options.Value.BypassSslValidation);
        _corporateCreditCardServiceClient = integrationUtility.GetClient<CorporateCreditCardServiceClient>(options.Value.Client, options.Value.Endpoints.CorporateCreditCard, options.Value.BypassSslValidation);
        _bankingCustomerProfileServiceClient = integrationUtility.GetClient<BankingCustomerProfileServiceClient>(options.Value.Client, options.Value.Endpoints.BankingCustomerProfile, options.Value.BypassSslValidation);
        _creditCardUpdateProfileServiceClient = integrationUtility.GetClient<CreditCardUpdateProfileServicesServiceClient>(options.Value.Client, options.Value.Endpoints.CreditCardUpdateProfile, options.Value.BypassSslValidation);

        _fdrDBContext = fdrDBContext;
        _currencyAppService = currencyAppService;
        _cardDetailsAppService = cardDetailsAppService;
        _requestActivityAppService = requestActivityAppService;
        _customerProfileAppService = customerProfileAppService;
        this._authManager = authManager;
        this.auditLogger = auditLogger;
        _requestAppService = requestAppService;
    }




    #endregion




    [HttpPost]
    public async Task<ApiResponseModel<ChangeStatusResponse>> ChangeStatus([FromBody] ChangeStatusRequest request)
    {
        var response = new ApiResponseModel<ChangeStatusResponse>();

        var cardInfoResponse = await _cardDetailsAppService.GetCardInfo(request.RequestID, includeCardBalance: true);

        if (cardInfoResponse == null || !cardInfoResponse.IsSuccess || cardInfoResponse.Data is null)
            return response.Fail($"Invalid RequestId {cardInfoResponse?.Message}");

        CardDetailsResponse cardInfo = cardInfoResponse.Data;
        CardDefinitionDto cardDef = await _cardDetailsAppService.GetCardWithExtension(cardInfo.CardType);



        UpdateRequestDto updateRequestDto = new()
        {
            RequestID = request.RequestID,
            BankAcctNo = cardInfo.BankAccountNumber!,
            CardNo = cardInfo.CardNumber!,
            CardType = cardInfo.CardType,
            City = cardInfo.HolderAddressCity!,
            CivilID = cardInfo.CivilId!,
            Street = cardInfo.HolderAddressLine1!,
            Continuation_1 = cardInfo.HolderAddressLine2!,
            Continuation_2 = string.IsNullOrEmpty(cardInfo.HolderAddressLine3) ? " " : cardInfo.HolderAddressLine3,
            Photo = cardInfo.Photo,
            RequestDate = cardInfo.ReqDate,
            RequestedLimit = cardInfo.RequestedLimit,
            ServicePeriod = cardInfo.ServicePeriod,
            TellerID = cardInfo.TellerId,
            OldCardStatus = (int)cardInfo.CardStatus,
            CardStatus = (int)request.NewStatus // From API Request
        };

        await ValidateCardNewStatus(cardInfo!.CardStatus, request.NewStatus);

        if (request.NewStatus == CreditCardStatus.Approved)
            await PreApprovalValidation(cardInfo, updateRequestDto, cardDef);

        //TODO : if new status is lost/stolen

        await ValidateAgainstDeposit(cardInfo, cardDef!);
        await ValidateAgainstMargin(cardInfo);


        var updateCardResponse = await UpdateCardRequest(cardInfo.Parameters, updateRequestDto);

        if (request.NewStatus != CreditCardStatus.Approved)
            return response.Success(updateCardResponse);



        await PostApprovalProcess(cardInfo);

        //TODO : LogRequestActivity
        //await _requestActivityAppService.LogRequestActivity(null,null,null);

        return response.Success(updateCardResponse);

    }
    private async Task UpgradeCardIfOldCardNumberIsAvailable(CardDetailsResponse newCardInfo)
    {

        string oldCardNumber = newCardInfo.Parameters?.OldCardNumberEncrypted!;

        if (string.IsNullOrEmpty(oldCardNumber)) return;

        //TODO: Card Upgrade Degrade 
        // Get Card Detail
        var cardDetailResponse = await _customerProfileAppService.GetBalanceStatusCardDetail(oldCardNumber);


        if (!cardDetailResponse.IsSuccess)
            throw new ApiException(errors: null, message: "Unable to fetch old card detail");

        bool isKfhStaffId = cardDetailResponse.Data.KfhStaff.Trim() != "";

        Request oldCardRequest = _fdrDBContext.Requests.First(x => x.CardNo == oldCardNumber);
        CardDefinitionDto oldCardExtension = await _cardDetailsAppService.GetCardWithExtension(oldCardRequest.CardType);
        CardDetailsResponse OlCardInfo = (await _cardDetailsAppService.GetCardInfo(oldCardRequest.RequestId))?.Data ?? throw new ApiException(errors: null, message: "Unable to fetch old card detail");

        CardDefinitionExtentionDto newCardExtension = (await _cardDetailsAppService.GetCardDefinitionExtensionsByProductId(newCardInfo.CardType))?.Data!;

        string primaryLogo = ((newCardInfo.ProductType is ProductTypes.Tayseer) ? newCardExtension.Logo : newCardExtension.LogoVisa) ?? "";
        string secondaryLogo = newCardExtension.LogoMC ?? "";

        _ = int.TryParse(newCardExtension.CashPlanNo, out int cashPlanNumber);
        _ = int.TryParse(newCardExtension.RetailPlanNo, out int retailPlanNumber);

        var cardUpgradeResponse = (await _creditCardUpdateServiceClient.performCardUpgradeAsync(new()
        {
            cardUpgradeReq = new()
            {
                requestID = (long)newCardInfo.RequestId,
                cardNo = OlCardInfo.CardNumber,
                fdrAccountNumber = OlCardInfo.FdrAccountNumber,
                primaryLogo = primaryLogo,
                secondaryLogo = secondaryLogo,
                pctID = newCardInfo.Parameters?.PCTFlag,
                creditLimit = (double)newCardInfo.ApproveLimit!,
                defaultCashPlan = cashPlanNumber,
                defaultRetailPlan = retailPlanNumber,
                deliveryMethod = await GetDeliveryMethod(newCardInfo)
            }
        })).performCardUpgradeResult;

        _ = int.TryParse(cardUpgradeResponse.respCode, out int respCode);
        if (respCode != 0)
            throw new ApiException(message: $"Card Upgrade Failed; Response Code: {respCode} ;  Response Message: {cardUpgradeResponse.respMessage}", insertSeriLog: true);

        return;
    }


    private async Task<string> GetDeliveryMethod(CardDetailsResponse cardDetails)
    {

        var delivery = await _fdrDBContext.RequestDeliveries.AsNoTracking().SingleOrDefaultAsync(x => x.RequestId == cardDetails.RequestId);

        if (delivery is null) return string.Empty;

        var deliveryBranchId = !string.IsNullOrEmpty(delivery.DeliveryBranchId) ? delivery.DeliveryBranchId : cardDetails.BranchId.ToString("D3");

        return delivery.DeliveryType switch
        {
            "BRANCH" => $"{deliveryBranchId}/B",
            "COURIER" => $"{deliveryBranchId}/C",
            _ => ""
        };
    }
    private static async Task<RequestParameterDto> GetRequestParameter(Request cardRequest)
    {
        var requestParameter = DictionaryExtension.ConvertKeyValueDataToObject<RequestParameterDto>(cardRequest.Parameters.Select(x => new KeyValueTable
        {
            ColumnName = x.Parameter,
            ColumnValue = x.Value!
        }));

        return await Task.FromResult(requestParameter);
    }
    private async Task<accountBoardingRequest> BuildAccountOnBoardingRequest(OnBoardingRequest onBoardingRequest)
    {
        var request = await _fdrDBContext.Requests.AsNoTracking().Include(x => x.Parameters).FirstOrDefaultAsync(x => x.RequestId == onBoardingRequest.RequestId) ?? new();
        int cardType = onBoardingRequest.CardType;
        var cardDef = await _fdrDBContext.CardDefs.AsNoTracking().FirstOrDefaultAsync(x => x.CardType == cardType);
        var requestParameter = await GetRequestParameter(request);

        int serviceNumber = Convert.ToInt32(requestParameter?.ServiceNumber);
        DateTime startDate = DateTime.Today;
        DateTime ExpiryDate = startDate.AddMonths(Convert.ToInt32(requestParameter?.ServiceNumberInMonths));
        string serviceDateFormat = ConfigurationBase.AccountOnBoardingDateFormat;
        string uniqueIdPrefix = ConfigurationBase.UniqueIdPrefix;


        var productType = Helpers.GetProductType(cardDef?.Duality, cardDef?.MinLimit.ToInt(), cardDef?.MaxLimit.ToInt());
        var extension = (await _cardDetailsAppService.GetCardDefinitionExtensionsByProductId(cardType))?.Data!;
        var customerProfileResult = (await _creditCardUpdateProfileServiceClient.getCreditCardCustomerProfileAsync(new()
        {
            creditCardCustomerProfileCriteria = new()
            {
                searchCriteria = "Civil_ID",
                searchValue = request.CivilId
            }
        }))?.getCreditCardCustomerProfileResult ?? default;

        var customerProfile = customerProfileResult?.FirstOrDefault();


        bool havingCustomerNumber = !string.IsNullOrEmpty(customerProfile?.customerNumber);
        bool havingCorporateCivilId = !string.IsNullOrEmpty(requestParameter?.CorporateCivilId);
        var corporateCardTypeIds = (await _fdrDBContext.ConfigParameters.AsNoTracking().FirstOrDefaultAsync(x => x.ParamName == ConfigurationBase.CorporateCardTypeIds))?.ParamValue.Split(",") ?? Array.Empty<string>();
        bool isCorporateCard = corporateCardTypeIds.Any(x => x == cardType.ToString());

        bool isCobrand = await IsCardIsCoBrandCard(cardType);

        //List<cardRequest> requestList = new();
        //if (productType == ProductTypes.Tayseer)
        //{
        //    requestList.Add(new()
        //    {
        //        cardLogo = extension.LogoMC.ToInt(),
        //        emblemID = extension.EmblemMC.ToInt()
        //    });
        //}

        onBoardingRequest.IsTayseerCard = productType == ProductTypes.Tayseer;
        onBoardingRequest.CardName = cardDef?.Name;

        var accountBoardingRequest = new accountBoardingRequest()
        {
            serviceNumber = serviceNumber,
            serviceStartDate = serviceNumber != 0 ? startDate.ToString(serviceDateFormat).ToInt() : 0,
            serviceExpiryDate = serviceNumber != 0 ? ExpiryDate.ToString(serviceDateFormat).ToInt() : 0,
            accountRecordAction = "A",
            embosserRecordAction = "A",
            cardRequestList = GetRequestList(),
            customerRecordAction = havingCustomerNumber ? "U" : "A",
            customerNumber = havingCustomerNumber ? customerProfile?.customerNumber : "",
            uniqueID = cardType == 25 ? $"{uniqueIdPrefix}{requestParameter?.PrimaryCardCivilId}" : "",
            vipStatus = requestParameter!.IsVIP.ToInt(),
            title = customerProfile?.gender == 0 ? "Mr" : "Ms",
            firstName = customerProfile?.firstName,
            middleName = customerProfile?.middleName,
            lastName = customerProfile?.lastName,
            dateOfBirth = customerProfile.birth.ToString(serviceDateFormat).ToInt(),
            genderCode = (customerProfile?.gender + 1).ToString(),
            civilID = customerProfile?.civilID,
            addressLine1 = request.Street,
            addressLine2 = request.AddressLine1,
            addressLine3 = request.AddressLine2, //TODO: do double check
            addressLine4 = request.PostOfficeBoxNumber.ToString(),
            requestID = onBoardingRequest.RequestId,
            city = await GetDeliveryMethod(new() { RequestId = onBoardingRequest.RequestId, BranchId = request.BranchId }),
            corpCivilID = (isCorporateCard && havingCorporateCivilId) ? requestParameter.CorporateCivilId : "",
            embossedName2 = await GetEmbossName2(),
            country = ConfigurationBase.Country,
            countryCode = ConfigurationBase.CountryCode,
            homePhoneNumber = request.HomePhone.ToString(),
            workPhoneNumber = request.WorkPhone.ToString(),
            mobilePhoneNumber = request.Mobile.ToString(),
            email = customerProfile?.email,
            nationality = "",//TODO: fetch from screen
            accountNumber = "",
            creditLMT = (int)(request.ApproveLimit ?? 0),
            issuanceID = requestParameter.PCTFlag,
            productType = (int)Helpers.GetProductType(cardDef?.Duality, cardDef?.MinLimit.ToInt(), cardDef?.MaxLimit.ToInt()),
            organization = productType == ProductTypes.Tayseer ? extension.OrgVisa.ToInt() : extension.Currency.ToInt(),
            accountLogo = productType == ProductTypes.Tayseer ? extension.LogoVisa.ToInt() : extension.Logo.ToInt(),
            accountEmblemID = productType == ProductTypes.Tayseer ? extension.EmblemVisa.ToInt() : extension.Emblem.ToInt(),
            retailBankingACCNBR = request.AcctNo,
            income = (int)(request.Salary ?? 0),//TODO: DO double check
            staffID = request.TellerId,
            cardType = cardType.ToString(),
            kfhCustomer = !string.IsNullOrEmpty(request.AcctNo) ? "KC" : "NC",
            embossedName1 = customerProfile?.holderName,
            issueBranch1ST = request.BranchId,
            sameDayPlastic = 0,
            kfhStaff = requestParameter.KFHStaffID != "0" ? "SS" : "",
            pinMailerRequire = requestParameter.IssuePinMailer == "1",
        };

        BindSupplementaryData();


        #region local methods

        void BindSupplementaryData()
        {
            bool isSupplementaryChargeCard = requestParameter?.CardType == ConfigurationBase.SupplementaryChargeCard;
            if (isSupplementaryChargeCard && onBoardingRequest != null)
            {
                accountBoardingRequest.accountRecordAction = string.Empty;
                accountBoardingRequest.primaryRequestID = requestParameter.PrimaryCardRequestId.ToLong();
                accountBoardingRequest.primaryRequestIDSpecified = true;
                accountBoardingRequest.uniqueID = requestParameter?.PrimaryCardCivilId;

                BindPrimaryCardData();
                BindLogoForMasterCardKACWorld();
            }
        }
        void BindLogoForMasterCardKACWorld()
        {
            if (onBoardingRequest.CardType == ConfigurationBase.MasterCardKACWorld)
            {
                if (accountBoardingRequest.accountNumber.Substring(6, 3) == "720")
                {
                    accountBoardingRequest.accountLogo = 720;
                    accountBoardingRequest.accountEmblemID = 72000;
                }
            }
        }
        void BindPrimaryCardData()
        {
            if (!decimal.TryParse(requestParameter?.PrimaryCardRequestId, out decimal primaryCardRequestId))
                return;

            var primaryCardRequest = _fdrDBContext.Requests.AsNoTracking().Where(x => x.RequestId == primaryCardRequestId);
            if (primaryCardRequest.Count() == 1)
            {
                accountBoardingRequest.accountNumber = primaryCardRequest.FirstOrDefault()?.FdAcctNo;
                accountBoardingRequest.creditLMT = (int)request.ApproveLimit;
            }
        }
        cardRequest[] GetRequestList()
        {
            if (productType == ProductTypes.Tayseer)
                return new cardRequest[] { new() { cardLogo = extension.LogoMC.ToInt(), emblemID = extension.EmblemMC.ToInt() } };
            else
                return Array.Empty<cardRequest>();
        }
        async Task<string> GetEmbossName2()
        {
            var corporateProfile = await _fdrDBContext.CorporateProfiles.AsNoTracking().FirstOrDefaultAsync(x => x.CorporateCivilId == requestParameter.CorporateCivilId);
            if (corporateProfile is not null && corporateProfile?.CorporateCivilId is not null)
                return corporateProfile.EmbossingName.ToUpper();

            if (requestParameter.Collateral == Collateral.FOREIGN_CURRENCY_PREPAID_CARDS.ToString())
            {
                CardCurrency? cardCurrency = await _fdrDBContext.CardCurrencies.AsNoTracking().FirstOrDefaultAsync(x => x.Org == extension.Currency);
                if (cardCurrency != null)
                    return cardCurrency?.CurrencyShortName?.ToUpper()!;
            }

            if (isCobrand)
                return $"{requestParameter.ClubName} ID {requestParameter.ClubMembershipId}";

            return string.Empty;
        }
        #endregion
        return accountBoardingRequest;
    }

    private async Task<ApiResponseModel<string>> AccountOnBoarding(OnBoardingRequest request)
    {
        var accountBoardingRequest = await BuildAccountOnBoardingRequest(request!);

        try
        {
            var accountOnBoardingResult = (await _creditCardUpdateProfileServiceClient.performAccountBoardAsync(new()
            {
                accountBoardingRequest = accountBoardingRequest
            }))?.performAccountBoardResult;

            return Success<string>(GetPrimaryCardNumberFromAccountOnBoardingResult(request.IsTayseerCard, accountOnBoardingResult));

        }
        catch (System.Exception ex)
        {

            //TODO: new(endpoint,methodName,request(json))

            string message = $@"Account Boarding Request from aurora has failed for {request.CardName} 
                                        Civil ID: {accountBoardingRequest.civilID}
                                        Debit Account No: {accountBoardingRequest.retailBankingACCNBR}
                                        Please check and take the proper action.
                                        - In case the card has been created in FD side and card details has not been updated into KFH local BCD system, then use BCD admin from BCD Operations to update card Number.
                                        - In case no card has been created in FD side , Please change request status to pending for this customer using BCD admin from BCD Operations.";

            auditLogger.Log.Error(message);

            if (ex.ToString().Contains("ORA-00001: unique constraint (VPBCD.REQ_BOARDING_LOG_PK) violated"))
                return Failure<string>($"AccountBoarding has been started and not able to complete! {message}");



            //TODO : SendEmail;

            return Failure<string>(message);

        }

        string GetPrimaryCardNumberFromAccountOnBoardingResult(bool isTayseerCard, accountBoardingResponse? accountOnBoardingResult)
        {
            string primaryCardNumber = string.Empty;
            int cardCount = isTayseerCard ? 2 : 1;

            for (int i = 0; i < cardCount; i++)
            {
                cardData cardData = accountOnBoardingResult?.cardData[i]!;
                if (string.IsNullOrEmpty(cardData.cardNumber))
                    break;

                var cardNo = cardData.cardNumber.Substring(3, 16);
                var cardFirstDigit = cardNo[0];
                bool isMasterCard = ConfigurationBase.MasterCardStartingNumbers.Any(x => x == cardFirstDigit);

                if (!(isMasterCard && isTayseerCard))
                    primaryCardNumber = cardNo;
            }

            return primaryCardNumber;
        }
    }
    private async Task PostApprovalProcess(CardDetailsResponse cardInfo, ProcessCardRequest? request = null)
    {
        await UpgradeCardIfOldCardNumberIsAvailable(cardInfo);

        var onboardingResponse = await AccountOnBoarding(new()
        {
            CardType = cardInfo!.CardType,
            RequestId = Convert.ToInt64(cardInfo.RequestId),
        });



        string newCardNumber = onboardingResponse.Data!;
        await UpdateSupplementaryCards(cardInfo, newCardNumber);

        await UpdateAlOusraSupplementaryCards(cardInfo, newCardNumber);

        await PerformForeignCurrencyPayment(cardInfo!.Collateral!, cardInfo.CardType, cardInfo!.BankAccountNumber!, cardInfo.CardStatus, newCardNumber);

        await UpdateParameters(cardInfo);


    }






    private async Task<bool> UpdateParameters(CardDetailsResponse? cardInfo)
    {
        //TODO: Here There is no update required on request parameter, since the changes only on card request status
        return true;
    }

    private async Task<bool> PerformForeignCurrencyPayment(string issuingOption, int cardType, string accountNumber, CreditCardStatus cardStatus, string cardNumber)
    {
        if (issuingOption != Collateral.FOREIGN_CURRENCY_PREPAID_CARDS.ToString())
            return false;

        var currencyData = await _currencyAppService.ValidateSufficientFundForForeignCurrencyCards(cardType, accountNumber);
        if (!currencyData.IsSuccess)
            throw new ApiException(errors: null, message: currencyData.Message);

        var paymentResponse = (await _creditCardUpdateServiceClient.performFCCreditCardPymtWithMQAsync(new()
        {
            paymentRequest = new()
            {
                cardNo = cardNumber,
                cardAmount = Convert.ToDouble(currencyData.Data?.DestAmount),
                custAcctNo = accountNumber,
                transRate = Convert.ToDouble(currencyData.Data?.TransferRate)
            }
        }))?.performFCCreditCardPymtWithMQ;

        var paymentResponseCode = paymentResponse?.respCode ?? string.Empty;

        if (paymentResponseCode != "0000")
            throw new ApiException(errors: null, message: $"Application Successfully Updated, but an error happened when Card loaded with the minimum opening balance, please contact with system administrator");

        return true;
    }

    private async Task<bool> UpdateAlOusraSupplementaryCards(CardDetailsResponse? cardInfo, string newCardNumber, bool approveAllSupplementary = true)// TODO: what to do for this flag
    {
        if (string.IsNullOrEmpty(newCardNumber)) return false;

        if (!(cardInfo!.CardType == ConfigurationBase.AlOsraPrimaryCardTypeId && approveAllSupplementary))
            return false;

        var supplementaryCard = (await _cardDetailsAppService.GetSupplementaryCardsByRequestId(cardInfo.RequestId))?.Data;

        if (supplementaryCard == null) return false;

        List<Task> taskList = new();

        foreach (var item in supplementaryCard)
        {
            if ((int)item.CardStatus > (int)CreditCardStatus.Approved) continue;

            var _creditCard = item.Adapt<CreditCardUpdateServiceReference.creditCard>();

            _creditCard.ApproveDate = DateTime.Now.Date;
            _creditCard.ApproveDateSpecified = true;

            if (_creditCard.cardStatus == (int)CreditCardStatus.Approved)
                _creditCard.cardStatus = (int)CreditCardStatus.AccountBoardingStarted;

            //this is temporary until integration getting fix in CreditCardUpdateServicesService.updateRequest method
            var cardRequest = await _fdrDBContext.Requests.AsNoTracking().FirstOrDefaultAsync(x => x.RequestId == _creditCard.requestID);
            if (cardRequest is not null)
                _creditCard.homePhone = cardRequest.HomePhone;

            taskList.Add(_creditCardUpdateServiceClient.updateRequestAsync(new()
            {
                CreditCard = _creditCard
            }));

            taskList.Add(AccountOnBoarding(new()
            {
                CardType = ConfigurationBase.AlOsraSupplementaryCardTypeId,
                RequestId = (long)(item.RequestId ?? 0)
            }));
        }

        await Task.WhenAll(taskList.ToArray());

        return true;
    }


    private async Task<bool> UpdateSupplementaryCards(CardDetailsResponse? cardInfo, string newCardNumber)
    {
        if (string.IsNullOrEmpty(newCardNumber)) return false;

        if (cardInfo?.Parameters?.CardType is not ConfigurationBase.SupplementaryChargeCard)
            return false;

        var supplementaryCard = await _fdrDBContext.PreregisteredPayees.FirstOrDefaultAsync(x => x.CivilId == cardInfo!.CivilId && x.CardNo == cardInfo.RequestId.ToString())
        ?? throw new ApiException(message: $"PreRegisterPayee not found for requestId {cardInfo!.RequestId}");

        supplementaryCard.CardNo = newCardNumber;
        await _fdrDBContext.SaveChangesAsync();

        return true;
    }
    private async Task<ChangeStatusResponse> UpdateCardRequest(RequestParameterDto? parameters, UpdateRequestDto request, RequestActionType actionType = RequestActionType.FullEdit)
    {

        var requestedCardStatus = (CreditCardStatus)request.CardStatus;

        await ModifyRequestedCardStatusByValidation();

        var creditCard = request.Adapt<CreditCardUpdateServiceReference.creditCard>();
        creditCard.requestDateSpecified = creditCard.requestDate != DateTime.MinValue;
        creditCard.ApproveDateSpecified = creditCard.ApproveDate != DateTime.MinValue;
        creditCard.expirySpecified = creditCard.expiry != DateTime.MinValue;

        //this is temporary until integration getting fix in CreditCardUpdateServicesService.updateRequest method
        var cardRequest = await _fdrDBContext.Requests.AsNoTracking().FirstOrDefaultAsync(x => x.RequestId == creditCard.requestID);
        if (cardRequest is not null)
            creditCard.homePhone = cardRequest.HomePhone;

        var updateResult = (await _creditCardUpdateServiceClient.updateRequestAsync(new()
        {
            CreditCard = creditCard
        }))?.updateRequestResult;

        if (updateResult == null || (updateResult != null && !updateResult.isSuccessful))
            throw new ApiException(errors: null, message: $"Unable to update credit card data {updateResult?.description}");

        RestoreRequestedCardStatus();

        return new()
        {
            CardStatus = (CreditCardStatus)request.CardStatus,
            RequestID = request.RequestID,
            Description = updateResult?.status ?? string.Empty
        };

        #region local methods

        void RestoreRequestedCardStatus()
        {
            if (requestedCardStatus is CreditCardStatus.AccountBoardingStarted or CreditCardStatus.CardUpgradeStarted)
                request.CardStatus = (int)CreditCardStatus.Approved;
        }

        async Task<(bool isSupplementaryCard, bool isChargeCard)> GetCardType()
        {
            int productType = await _cardDetailsAppService.GetPayeeProductType(request?.CardType);
            bool isSupplementaryCard = parameters?.CardType == ConfigurationBase.SupplementaryChargeCard;
            bool isChargeCard = productType == ConfigurationBase.PrimaryChargeCardPayeeTypeId;
            return (isSupplementaryCard, isChargeCard);
        }

        async Task ModifyRequestedCardStatusByValidation()
        {
            var customerProfile = (await _bankingCustomerProfileServiceClient.viewBankingCustomerProfileAsync(new() { civilID = request?.CivilID }))?.viewBankingCustomerProfileResult
            ?? throw new ApiException(message: "RIM is not active or does not exist");

            int customerAge = Helpers.GetAgeByDateOfBirth(customerProfile.birth_dt);
            string oldCardNumber = parameters?.OldCardNumberEncrypted ?? string.Empty;
            bool isBcdUser = true; //TODO : Should take from LoggedIn User
            (bool isSupplementaryCard, bool isChargeCard) = await GetCardType();

            if (request is null)
                throw new ApiException(message: "Request value is null");

            //check minor charger card
            if (!isSupplementaryCard && customerAge < 21 && isChargeCard && !isBcdUser && actionType != RequestActionType.MinorsChargeCardHoldersApproval)
            {
                request.CardStatus = (int)CreditCardStatus.PendingForMinorsApproval;
                return;
            }

            if (requestedCardStatus == CreditCardStatus.Approved)
            {
                request.CardStatus = (int)(string.IsNullOrEmpty(oldCardNumber) ? CreditCardStatus.AccountBoardingStarted : CreditCardStatus.CardUpgradeStarted);
                return;
            }

            request.CardStatus = (int)requestedCardStatus;
        }
        #endregion
    }



    private async Task ValidateAgainstMargin(CardDetailsResponse? cardInfo)
    {
        if (cardInfo?.Collateral != Collateral.AGAINST_MARGIN.ToString())
            return;

        var marginAccountNumber = cardInfo.Parameters!.MarginAccountNumber;
        _ = double.TryParse(cardInfo.Parameters.MarginAmount, out double marginAmount);

        var account = (await _customerAccountServiceClient.viewAccountDetailsAsync(new() { acct = marginAccountNumber })).viewAccountDetailsResult ?? throw new ApiException(message: "Account detail Not found");

        if (marginAmount > account.availableBalance)
            throw new ApiException(message: $"Margin Amount {marginAmount} greater than available balance {account.availableBalance}");

        if (marginAmount < (double)cardInfo.ApproveLimit!)
            throw new ApiException(message: $"Approve limit {cardInfo.ApproveLimit} is greater than Margin Amount {marginAmount}");
    }


    private static async Task ValidateAgainstDeposit(CardDetailsResponse cardInfo, CardDefinitionDto cardDef)
    {
        if (cardInfo?.Collateral != Collateral.AGAINST_DEPOSIT.ToString())
            return;

        if (cardInfo.ApproveLimit > cardInfo.DepositAmount)
            throw new ApiException(message: "Approve limit greater than Hold Amount");

        await CheckLimitWithProductRange((decimal)cardInfo.ApproveLimit!, cardDef);

    }

    private static async Task CheckLimitWithProductRange(decimal approveLimit, CardDefinitionDto cardDef)
    {
        if (approveLimit < Convert.ToInt32(cardDef.MinLimit))
            throw new ApiException(message: "Cannot approve.  The limit is too high for the product.");
        if (approveLimit > Convert.ToInt32(cardDef.MaxLimit))
            throw new ApiException(message: "Cannot approve.  The limit is too low for the product.");

        await Task.CompletedTask;
    }


    private async Task PreApprovalValidation(CardDetailsResponse cardInfo, UpdateRequestDto updateRequestDto, CardDefinitionDto cardDef)
    {
        updateRequestDto.ApproveDate = DateTime.Now;

        if (cardInfo.Collateral == Collateral.FOREIGN_CURRENCY_PREPAID_CARDS.ToString())
            await _currencyAppService.ValidateSufficientFundForForeignCurrencyCards(updateRequestDto.CardType, updateRequestDto.BankAcctNo);

        await ValidateCorporateProfile(cardInfo.CardType, cardInfo.Parameters?.CorporateCivilId, cardInfo.ApproveLimit);

        //TODO: Validate Old card number for "Close To Transfer"
        await ValidateCloseToTransfer(cardInfo);


        //TODO: ValidateNonPrepaidCardsApproval
        //TODO: ValidateApproval4OusraCards (Is the request for Supplementary)
        //TODO: ValidateApproval4SuplChargeCards (Is the request for Supplementary)
    }

    private async Task ValidateCloseToTransfer(CardDetailsResponse cardInfo)
    {
        if (cardInfo.CardType == ConfigurationBase.USDCard)
            return;

        string oldCardNumber = cardInfo.Parameters?.OldCardNumberEncrypted ?? "";

        if (string.IsNullOrEmpty(oldCardNumber))
            return;

        var cardDetailResponse = await _customerProfileAppService.GetBalanceStatusCardDetail(oldCardNumber);
        if (!cardDetailResponse.IsSuccess)
            throw new ApiException(errors: null, message: "Unable to fetch old card detail");

        if (cardDetailResponse.Data!.ExternalStatus == "C" || cardDetailResponse.Data.CardBlockStatus == "C")
            return;

        bool isKfhStaffId = cardDetailResponse.Data.KfhStaff.Trim() != "";

        Request oldCardRequest = _fdrDBContext.Requests.First(x => x.CardNo == oldCardNumber);
        CardDefinitionDto cardDef = await _cardDetailsAppService.GetCardWithExtension(oldCardRequest.CardType);

        decimal deductedAmount = 0;
        decimal balance = cardDetailResponse.Data.Balance;

        if (cardInfo.ProductType == ProductTypes.Tayseer)
            deductedAmount = GetDeductedAmountForT12Cards(cardDetailResponse.Data.OpenDate, cardDef, isKfhStaffId);


        if (deductedAmount + balance > cardInfo.ApproveLimit)
            throw new ApiException(errors: null,
                message: deductedAmount == 0 ? "Approve limit is less than balance of the Old Card" : $"Approve limit + Old Card Due Fees is less than balance of the Old Card; Due Fees: {deductedAmount}");
    }

    private decimal GetDeductedAmountForT12Cards(DateTime openDate, CardDefinitionDto oldCardDef, bool isKFHStaff = false)
    {
        long remainingMonths = Helpers.GetRemainMonths(openDate);

        decimal deductedAmount = 0;

        if (!isKFHStaff)
            deductedAmount = oldCardDef.Fees ?? 0;

        if (Int32.TryParse(oldCardDef!.Extension?.NonFundableYearlyFees, out Int32 _nonFundableYearlyFees) && _nonFundableYearlyFees > 0)
            deductedAmount = _nonFundableYearlyFees;


        return (deductedAmount / 12) * remainingMonths;
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
    private static async Task ValidateCardNewStatus(CreditCardStatus cardStatus, CreditCardStatus newStatus)
    {
        if (cardStatus is CreditCardStatus.Approved)
        {
            if (newStatus is CreditCardStatus.Pending)
                throw new ApiException(errors: null, message: "Card is Approved and It cannot be change to pending status again");

            if (newStatus is CreditCardStatus.Approved)
                throw new ApiException(errors: null, message: "Card is Approved already and It cannot be approved again");
        }


        if ((int)cardStatus > (int)CreditCardStatus.Approved && newStatus is CreditCardStatus.Pending)
            throw new ApiException(errors: null, message: "Card status cannot change to pending again");

        await Task.CompletedTask;
    }

    private static async Task ValidateCardNewStatus(CreditCardStatus cardStatus, ActionType newStatus)
    {
        if (cardStatus is CreditCardStatus.Approved && newStatus is ActionType.Approved)
            throw new ApiException(errors: null, message: "Card is Approved already and It cannot be approved again");



        await Task.CompletedTask;
    }





    private async Task<bool> IsCardIsCoBrandCard(int cardType)
    {
        var cobrandCardTypes = (await _fdrDBContext.ConfigParameters.FirstOrDefaultAsync(x => x.ParamName == "CoBrandCardTypes"))?.ParamValue?.Split(",");
        return cobrandCardTypes?.Any(x => x == cardType.ToString()) ?? false;
    }

}
