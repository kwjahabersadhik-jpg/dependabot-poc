using BankingCustomerProfileReference;
using CorporateCreditCardServiceReference;
using CreditCardsSystem.Data;
using CreditCardsSystem.Data.Models;
using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Interfaces.Workflow;
using CreditCardsSystem.Domain.Models.Access;
using CreditCardsSystem.Domain.Models.Card;
using CreditCardsSystem.Domain.Models.CardIssuance;
using CreditCardsSystem.Domain.Models.CardOperation;
using CreditCardsSystem.Domain.Models.Corporate;
using CreditCardsSystem.Domain.Models.SupplementaryCard;
using CreditCardsSystem.Domain.Shared.Interfaces.Workflow;
using CreditCardsSystem.Domain.Shared.Models.RequestActivity;
using CreditCardsSystem.Utility.Extensions;
using CreditCardTransactionInquiryServiceReference;
using CreditCardUpdateProfileService;
using CreditCardUpdateServiceReference;
using CustomerAccountsServiceReference;
using Kfh.Aurora.Auth;
using Kfh.Aurora.Integration;
using Kfh.Aurora.Logging;
using Kfh.Aurora.Workflow.Dto;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data;

namespace CreditCardsSystem.Application.CardOperations;

public class CardRequestApprovalAppService : BaseApiResponse, ICardRequestApprovalAppService, IAppService
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
    private readonly IWorkflowAppService _workflowAppService;
    private readonly IAuthManager _authManager;
    private readonly ICorporateProfileAppService _corporateProfileAppService;
    private readonly IAuditLogger<CardRequestApprovalAppService> auditLogger;
    private readonly ILogger<CardRequestApprovalAppService> logger;

    private readonly IRequestAppService _requestAppService;
    private readonly IChangeLimitAppService ChangeLimitAppService;
    private readonly IWorkflowCalculationService workflowCalculationService;
    public TaskResult TaskDetail { get; private set; }

    public CardRequestApprovalAppService(IIntegrationUtility integrationUtility, IOptions<IntegrationOptions> options,
        FdrDBContext fdrDBContext,
        ICurrencyAppService currencyAppService,
        ICardDetailsAppService cardDetailsAppService,
        IRequestActivityAppService requestActivityAppService,
        ICustomerProfileAppService customerProfileAppService, IWorkflowAppService workflowAppService, IAuthManager authManager,
        ICorporateProfileAppService corporateProfileAppService,
        IAuditLogger<CardRequestApprovalAppService> auditLogger,
        ILogger<CardRequestApprovalAppService> logger,
        IRequestAppService requestAppService, IChangeLimitAppService changeLimitAppService, IWorkflowCalculationService workflowCalculationService)
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
        _workflowAppService = workflowAppService;
        this._authManager = authManager;
        this._corporateProfileAppService = corporateProfileAppService;
        this.auditLogger = auditLogger;
        this.logger = logger;
        _requestAppService = requestAppService;
        ChangeLimitAppService = changeLimitAppService;
        this.workflowCalculationService = workflowCalculationService;
    }




    #endregion

    [HttpPost]
    public async Task<ApiResponseModel<CreditCardCustomerProfileResponse>> GetCustomerProfile(string civilId)
    {
        var customerProfile = ((await _creditCardUpdateProfileServiceClient.getCreditCardCustomerProfileAsync(new()
        {
            creditCardCustomerProfileCriteria = new()
            {
                searchCriteria = "Civil_ID",
                searchValue = civilId
            }
        }))?.getCreditCardCustomerProfileResult?.FirstOrDefault()) ?? throw new ApiException(message: "CustomerProfile not found!");


        return Success(customerProfile.Adapt<CreditCardCustomerProfileResponse>());
    }

    [HttpPost]
    public async Task<ApiResponseModel<ProcessResponse>> ProcessCardRequest([FromBody] ProcessCardRequest request)
    {

        if (!await IsAuthorizedToApprove())
        {
            return Failure<ProcessResponse>(message: "Sorry you don't have permission!");
        }

        RequestActivityDto requestActivity = (await _requestActivityAppService.GetRequestActivityById(request.RequestActivityId)).Data ?? throw new ApiException(message: "Unable to find Request");
        CardDetailsResponse cardInfo = (await _cardDetailsAppService.GetCardInfo(requestActivity.RequestId, includeCardBalance: true))?.Data ?? throw new ApiException(message: "Invalid request Id");
        CardDefinitionDto cardDef = await _cardDetailsAppService.GetCardWithExtension(cardInfo.CardType);
        bool isSupplementaryCard = cardInfo.Parameters?.IsSupplementaryOrPrimaryChargeCard == "S";
        _ = Enum.TryParse(cardInfo.Collateral, out Collateral _collateral);

        bool IsTayseerSalaryException = cardInfo.ProductType is ProductTypes.Tayseer && (_collateral is Collateral.EXCEPTION or Collateral.AGAINST_SALARY);
        bool IsBoardingOrUpgradeStarted = cardInfo.CardStatus is CreditCardStatus.AccountBoardingStarted or CreditCardStatus.CardUpgradeStarted;

        string seqLogPrefix = $"RequestId:{cardInfo.RequestId}";
        StringBuilder seqLog = new();
        request.CardNumber = cardInfo.CardNumber.Masked(6, 6);
        request.Activity = CFUActivity.Card_Request;

        if (request.ActionType == ActionType.Returned)
            return await Return();

        if (request.ActionType == ActionType.Rejected)
            return await Reject();

        return await Approve();

        async Task<ApiResponseModel<ProcessResponse>> Approve()
        {
            if (cardInfo!.CardStatus is CreditCardStatus.Approved && request.ActionType is ActionType.Approved)
                throw new ApiException(errors: [], message: "Card is Approved already and It cannot be approved again");

            if (IsBoardingOrUpgradeStarted)
            {
                await HandleBoardingOrUpgradeStatus(cardInfo, request);

                string message = GlobalResources.SuccessApproval;

                if (request.BCDParameters is { NewStatus: CreditCardStatus.Pending })
                {
                    request.ActionType = ActionType.Pending;
                    message = $"Successfully changed the card status to {ActionType.Pending}";
                }

                await _requestActivityAppService.CompleteActivity(request);

                return Success(new ProcessResponse(), message: message);
            }


            //this is temporary until integration getting fix in CreditCardUpdateServicesService.updateRequest method
            var cardRequest = await _fdrDBContext.Requests.FindAsync(cardInfo.RequestId);

            UpdateRequestDto updateRequestDto = new()
            {
                BranchID = cardRequest!.BranchId,
                HomePhone = cardRequest!.HomePhone,
                RequestID = request.RequestId,
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
                CardStatus = (int)CreditCardStatus.Approved,// From API Request
                ApprovedLimit = cardInfo.ApproveLimit
            };


            if (IsTayseerSalaryException)
                await HandleTayseerCardRequest(cardInfo.CardStatus, request, updateRequestDto);

            //validations
            if (updateRequestDto.CardStatus is (int)CreditCardStatus.Approved)
            {
                await PreApprovalValidation(cardInfo, updateRequestDto);

                if (!isSupplementaryCard)
                {
                    await ValidateAgainstDeposit(cardInfo, cardDef!);
                    await ValidateAgainstMargin(cardInfo);
                }
            }

            await UpdateCardStatusBeforeApproval(cardInfo.Parameters, updateRequestDto);

            if (request.ActionType is ActionType.Approved)
                await PostApprovalProcess(cardInfo, request);


            var cardRequestAfterApproval = await _fdrDBContext.Requests.AsNoTracking().FirstOrDefaultAsync(x => x.RequestId == request.RequestId);
            IsBoardingOrUpgradeStarted = (CreditCardStatus)cardRequestAfterApproval!.ReqStatus is CreditCardStatus.AccountBoardingStarted or CreditCardStatus.CardUpgradeStarted;
            if (IsBoardingOrUpgradeStarted)
            {
                await CreateBCDTask(CreditCardStatus.AccountBoardingStarted.GetDescription(), cardInfo.RequestId, request);
                return Failure<ProcessResponse>(message: "Created BCD Task");
            }


            await _requestActivityAppService.CompleteActivity(request);

            //TODO: Complete Supplementary Activities

            if (IsTayseerSalaryException)
            {
                var message = cardInfo.CardStatus switch
                {
                    CreditCardStatus.Pending => string.Format(GlobalResources.WaitingForTayseerApproval, "credit check review first"),
                    CreditCardStatus.PendingForCreditCheckingReview => string.Format(GlobalResources.WaitingForTayseerApproval, "credit check review final"),
                    CreditCardStatus.CreditCheckingReviewed => GlobalResources.SuccessApproval,
                    _ => GlobalResources.SuccessApproval
                };

                return Success(new ProcessResponse(), message: message);
            }


            return Success(new ProcessResponse(), message: GlobalResources.SuccessApproval);

            async Task HandleTayseerCardRequest(CreditCardStatus cardStatus, ProcessCardRequest request, UpdateRequestDto updateRequestDto)
            {
                //inserting credit check record for approval
                if (cardStatus is CreditCardStatus.PendingForCreditCheckingReview)
                {
                    request.CreditCheckModel.Status = (int)CreditCheckStatus.Approved;
                    await ChangeLimitAppService.InsertTayseerCreditCheckingRecord(request.CreditCheckModel);
                }

                updateRequestDto.CardStatus = cardStatus switch
                {
                    CreditCardStatus.Pending => (int)CreditCardStatus.PendingForCreditCheckingReview,
                    CreditCardStatus.PendingForCreditCheckingReview => (int)CreditCardStatus.CreditCheckingReviewed,
                    CreditCardStatus.CreditCheckingReviewed => (int)CreditCardStatus.Approved
                };

                if (cardStatus is CreditCardStatus.CreditCheckingReviewed)
                    request.ActionType = ActionType.Approved;
                else
                    request.ActionType = (ActionType)updateRequestDto.CardStatus;
            }
        }

        async Task<ApiResponseModel<ProcessResponse>> Reject()
        {
            if (IsTayseerSalaryException)
            {
                var newStatus = cardInfo!.CardStatus switch
                {
                    CreditCardStatus.Pending => CreditCardStatus.Rejected,
                    CreditCardStatus.PendingForCreditCheckingReview => CreditCardStatus.CreditCheckingReviewRejected,
                    CreditCardStatus.CreditCheckingReviewed => CreditCardStatus.CreditCheckingRejected,
                    CreditCardStatus.CardUpgradeStarted => CreditCardStatus.Rejected
                };

                if (cardInfo!.CardStatus is CreditCardStatus.PendingForCreditCheckingReview)
                {
                    request.CreditCheckModel.Status = (int)CreditCheckStatus.Rejected;
                    await ChangeLimitAppService.InsertTayseerCreditCheckingRecord(request.CreditCheckModel);
                }


                await UpdateLocalState(seqLog, seqLogPrefix, cardInfo.CardStatus, newStatus, cardInfo.RequestId);

                //if (cardInfo!.CardStatus is CreditCardStatus.CardUpgradeStarted && newStatus is CreditCardStatus.Rejected)
                //    await UpdateLocalState(seqLog, seqLogPrefix, cardInfo.CardStatus, newStatus, cardInfo.RequestId);
                //else
            }

            await _requestActivityAppService.CompleteActivity(request);

            //if(cardInfo.CardStatus is CreditCardStatus.Pending)
            //{
            //    await _requestAppService.CancelRequest(cardInfo.RequestId);
            //}

            return Success(new ProcessResponse() { CardNumber = "" }, message: "Successfully Rejected");
        }

        async Task<ApiResponseModel<ProcessResponse>> Return()
        {

            await _workflowAppService.ReturnToMaker(new Domain.Models.Workflow.ReturnToMakerRequest() { Comments = request.ReasonForRejection, TaskId = request.TaskId, InstanceId = request.WorkFlowInstanceId });
            return Success(new ProcessResponse());
        }

        async Task<bool> IsAuthorizedToApprove()
        {
            if (request.TaskId is null)
                throw new ApiException(message: "Invalid Task");

            TaskDetail = await _workflowAppService.GetTaskById((Guid)request.TaskId);

            _ = Enum.TryParse(TaskDetail.Payload["ProductType"]?.ToString(), out ProductTypes productType);


            if (TaskDetail.Payload.ContainsKey("Collateral"))
            {
                _ = Enum.TryParse(TaskDetail.Payload["Collateral"]?.ToString(), out Collateral collateral);
                if (collateral is Collateral.EXCEPTION)
                    return _authManager.HasPermission(Permissions.ExceptionCard.EnigmaApprove());

            }


            return _authManager.HasPermission(Permissions.NormalCard.EnigmaApprove());
        }

    }




    #region Private Methods

    private async Task PerformCardUpgrade(CardDetailsResponse newCardInfo, ProcessCardRequest? request)
    {
        string oldCardNumber = newCardInfo.Parameters?.OldCardNumberEncrypted!;

        if (string.IsNullOrEmpty(oldCardNumber)) return;

        var OlCardInfoTask = _cardDetailsAppService.GetCardInfo(requestId: null, cardNumber: oldCardNumber, includeCardBalance: true);
        var newCardExtensionTask = _cardDetailsAppService.GetCardDefinitionExtensionsByProductId(newCardInfo.CardType);
        await Task.WhenAll(OlCardInfoTask, newCardExtensionTask);

        var OlCardInfo = (await OlCardInfoTask)?.Data ?? throw new ApiException(message: "Unable to find old card number");
        var newCardExtension = (await newCardExtensionTask)?.Data ?? throw new ApiException(message: "Unable to find new card extension");

        string primaryLogo = ((newCardInfo.ProductType is ProductTypes.Tayseer) ? newCardExtension.LogoVisa : newCardExtension.Logo) ?? "";
        string secondaryLogo = ((newCardInfo.ProductType is ProductTypes.Tayseer) ? newCardExtension.LogoMC : null) ?? "";

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
            throw new ApiException(message: $"Card Upgrade Failed; Response Code: {respCode} ;  Response Message: {cardUpgradeResponse.respMessage}", insertSeriLog: true, returnBack: true);

        await CreateBCDTask(cardUpgradeResponse.respMessage, newCardInfo.RequestId, request);
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

        //TODO Refactor

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

        string nationality = "";
        if (customerProfile?.nationality != null)
            nationality = await _customerProfileAppService.GetCustomerNationality(customerProfile?.nationality.ToString()!) ?? "";

        bool havingCustomerNumber = !string.IsNullOrEmpty(customerProfile?.customerNumber);
        bool havingCorporateCivilId = !string.IsNullOrEmpty(requestParameter?.CorporateCivilId);
        var corporateCardTypeIds = (await _fdrDBContext.ConfigParameters.AsNoTracking().FirstOrDefaultAsync(x => x.ParamName == ConfigurationBase.CorporateCardTypeIds))?.ParamValue.Split(",") ?? Array.Empty<string>();
        bool isCorporateCard = corporateCardTypeIds.Any(x => x == cardType.ToString());

        bool isCobrand = await IsCardIsCoBrandCard(cardType);

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
            nationality = nationality,//TODO: fetch from screen
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

            string log = $@"Account Boarding Request from aurora has failed for {request.CardName} 
                                        Civil ID: {accountBoardingRequest.civilID}
                                        Debit Account No: {accountBoardingRequest.retailBankingACCNBR}";
            string message = $@"{log} 
                                Please check and take the proper action.
                                - In case the card has been created in FD side and card details has not been updated into KFH local BCD system, then use BCD admin from BCD Operations to update card Number.
                                - In case no card has been created in FD side , Please change request status to pending for this customer using BCD admin from BCD Operations.";

            auditLogger.Log.Error(ex, log);


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

    private async Task<bool> PostApprovalProcess(CardDetailsResponse cardInfo, ProcessCardRequest? request = null)
    {
        await PerformCardUpgrade(cardInfo, request);

        var onboardingResponse = await AccountOnBoarding(new()
        {
            CardType = cardInfo!.CardType,
            RequestId = Convert.ToInt64(cardInfo.RequestId),
        });

        if (!onboardingResponse.IsSuccess && request is not null)
        {
            //await CreateBCDTask(onboardingResponse.Message, cardInfo.RequestId, request);
            return false;
        }

        string newCardNumber = onboardingResponse.Data!;

        //if we are approving supplementary card we have to update supplementary card number by replacing requstId
        await UpdateSupplementaryCardNumber(cardInfo, newCardNumber);

        await OnBoardSupplementaryCards(cardInfo);

        await PerformForeignCurrencyPayment(cardInfo!.Collateral!, cardInfo.CardType, cardInfo!.BankAccountNumber!, cardInfo.CardStatus, newCardNumber);
        await UpdateParameters(cardInfo);

        return true;
    }

    async Task CreateBCDTask(string comments, decimal requestId, ProcessCardRequest request)
    {
        var cardStatus = (CreditCardStatus)(_fdrDBContext.Requests.AsNoTracking().First(x => x.RequestId == requestId)).ReqStatus;

        CreditCardStatus[] BCDCardStatus = new[] { CreditCardStatus.AccountBoardingStarted, CreditCardStatus.CardUpgradeStarted };

        if (!BCDCardStatus.Any(x => x == cardStatus))
            return;

        //Create workflow for BCD Admin
        CompleteTaskRequest taskRequest = new()
        {
            Assignee = _authManager.GetUser()?.KfhId ?? "",
            TaskId = request.TaskId.GetValueOrDefault(),
            InstanceId = request.WorkFlowInstanceId.GetValueOrDefault(),
            Status = ActionType.AssignToBCD.ToString(),
            Payload = new(){ { "Outcomes",new List<string>{ ActionType.AssignToBCD.ToString() } },
                             { "Description", cardStatus.ToString() },
            },
            Comments = string.IsNullOrEmpty(comments) ? null : new() { comments }
        };

        if (request.TaskId is not null)
            taskRequest.Payload.Add("ParentTaskId", request.TaskId!);

        await _workflowAppService.CompleteTask(taskRequest);

        string notificationMessage = $"{(_authManager.HasPermission(Permissions.AccountBoardingRequest) ? GlobalResources.RedirectToTaskListForBCDUser : GlobalResources.AssignToBCDTeam)}, {cardStatus}";

        throw new ApiException(message: notificationMessage);

    }
    async Task HandleBoardingOrUpgradeStatus(CardDetailsResponse cardInfo, ProcessCardRequest request)
    {
        string seqLogPrefix = $"RequestId:{cardInfo.RequestId}";
        StringBuilder seqLog = new();
        StringBuilder seqAllLog = new(seqLogPrefix);

        if (request.BCDParameters is { NewStatus: CreditCardStatus.Pending or CreditCardStatus.CreditCheckingReviewed })
        {
            //deleting onboarding log
            _fdrDBContext.RequestBoardingLogs.Where(x => x.ReqId == cardInfo.RequestId).ExecuteDelete();

            if (cardInfo.ProductType is ProductTypes.Tayseer)
                await _requestAppService.RemoveRequestParameters(new() { SecondaryCardNumber = "" }, cardInfo.RequestId);

        }
        else
        {
            //Do not change this below methods sequence
            await UpdatingCardNumber();
            await UpdateMasterCardNumber();
            await CloseOldCardNumber();
        }


        await UpdatingLocalState();

        #region local functions
        async Task CloseOldCardNumber()
        {
            if (request.BCDParameters is { CloseOldCardNumber: true })
            {
                string oldCardNumber = cardInfo.Parameters?.OldCardNumberEncrypted ?? string.Empty;
                if (string.IsNullOrEmpty(oldCardNumber)) return;

                seqLog.Clear().Append(seqLogPrefix).Append($" OldCardNumber:{cardInfo.Parameters?.OldCardNumberEncrypted}");

                try
                {
                    await _creditCardUpdateServiceClient.reportCreditCardStatusAsync(new()
                    {
                        cardNo = cardInfo.Parameters!.OldCardNumberEncrypted,
                        cardStatus = ConfigurationBase.MQ_CARD_STATUS_CLOSED
                    });

                    seqLog.Append($"Successfully closure old card number {seqLog}");
                }
                catch (System.Exception)
                {
                    seqLog.Append($"Failed on closure old card number {seqLog}");
                }

                seqAllLog.AppendLine(seqLog.ToString());
                auditLogger.Log.Information(seqLog.ToString());
            }
        }
        async Task UpdatingCardNumber()
        {
            if (request.BCDParameters is null)
                return;

            if (request.BCDParameters is { NewStatus: CreditCardStatus.Approved } && string.IsNullOrEmpty(request.BCDParameters.CardNumber))
                throw new ApiException(errors: new List<ValidationError>() { new(nameof(request.BCDParameters.CardNumber), "please enter valid card number!") });


            #region validating card number in bcd
            //var balanceDetail = await _customerProfileAppService.GetBalanceStatusCardDetail(request.BCDParameters.CardNumber);
            //if (!balanceDetail.IsSuccess)
            //{
            //    throw new ApiException(errors: new List<ValidationError>() { new(nameof(request.BCDParameters.CardNumber), balanceDetail.Message) });
            //}

            //if (balanceDetail.Data!.CivilId != cardInfo.CivilId)
            //{
            //    throw new ApiException(errors: new List<ValidationError>() { new(nameof(request.BCDParameters.CardNumber), "invalid card number! card number not mapped with civil id") });
            //}
            #endregion

            var customerProfile = ((await _creditCardUpdateProfileServiceClient.getCreditCardCustomerProfileAsync(new()
            {
                creditCardCustomerProfileCriteria = new()
                {
                    searchCriteria = "Civil_ID",
                    searchValue = cardInfo.CivilId
                }
            }))?.getCreditCardCustomerProfileResult?.FirstOrDefault()) ?? throw new ApiException(message: "CustomerProfile not found!");


            bool cardNumberModified = request.BCDParameters.CardNumber is not null && !request.BCDParameters.CardNumber.Equals(cardInfo.CardNumber, StringComparison.InvariantCultureIgnoreCase);
            bool FdrAccountNumberModified = request.BCDParameters.FdrAccountNumber is not null && !request.BCDParameters.FdrAccountNumber.Equals(cardInfo.FdrAccountNumber, StringComparison.InvariantCultureIgnoreCase);
            bool CustomerNumberModified = request.BCDParameters.CustomerNumber is not null && !request.BCDParameters.CustomerNumber.Equals(customerProfile?.customerNumber, StringComparison.InvariantCultureIgnoreCase);

            if (cardNumberModified || FdrAccountNumberModified || CustomerNumberModified)
            {

                request.BCDParameters.CardNumber ??= cardInfo.CardNumber ?? "";
                request.BCDParameters.FdrAccountNumber ??= cardInfo.FdrAccountNumber ?? "";
                request.BCDParameters.CustomerNumber ??= customerProfile?.customerNumber ?? "";

                seqLog.Clear().Append(seqLogPrefix).Append($" CardNumber:{request.BCDParameters.CardNumber} FdrAccountNumber:{request.BCDParameters.FdrAccountNumber}  CustomerNumber:{request.BCDParameters.CustomerNumber}");

                try
                {
                    bool cardNumberChanged = (await _creditCardUpdateProfileServiceClient.updateCreditCardAndProfileAsync(new()
                    {
                        requestId = cardInfo.RequestId.ToString(),
                        fdAccountNumber = request.BCDParameters.FdrAccountNumber,
                        cardNumber = request.BCDParameters.CardNumber,
                        customerNumber = request.BCDParameters.CustomerNumber
                    }))?.updateCreditCardAndProfileResult?.isSuccessful ?? false;

                    if (cardNumberChanged)
                        seqLog.Append($"Successfully changed card number {seqLog}");
                    else
                        throw new System.Exception();

                }
                catch (System.Exception)
                {
                    seqLog.Append("Failed on changing card number {seqLog}");
                }


                seqAllLog.AppendLine(seqLog.ToString());
                auditLogger.Log.Information(seqLog.ToString());
            }


            if (cardNumberModified && !string.IsNullOrEmpty(request.BCDParameters.CardNumber))
            {
                await PerformForeignCurrencyPayment(cardInfo!.Collateral!, cardInfo.CardType, cardInfo!.BankAccountNumber!, cardInfo.CardStatus, request.BCDParameters.CardNumber!);

                //Updating card number if this current card is supplementary
                await UpdateSupplementaryCardNumber(cardInfo, request.BCDParameters.CardNumber!);

                await OnBoardSupplementaryCards(cardInfo);
            }

        }
        async Task UpdatingLocalState()
        {
            if (request.BCDParameters is null) return;

            if (request.BCDParameters.NewStatus is not CreditCardStatus.All)
            {
                await UpdateLocalState(seqLog, seqLogPrefix, cardInfo.CardStatus, request.BCDParameters.NewStatus, cardInfo.RequestId);
                seqAllLog.AppendLine(seqLog.ToString());
                auditLogger.Log.Information(seqLog.ToString());
            }
        }
        async Task UpdateMasterCardNumber()
        {
            if (request.BCDParameters is null) return;

            bool masterCardNumberModified = request.BCDParameters.MasterCardNumber is not null && !request.BCDParameters.MasterCardNumber.Equals(cardInfo.Parameters?.SecondaryCardNumber, StringComparison.InvariantCultureIgnoreCase);
            if (masterCardNumberModified)
            {
                if (cardInfo.ProductType is ProductTypes.Tayseer)
                    await _requestAppService.RemoveRequestParameters(new() { SecondaryCardNumber = "" }, cardInfo.RequestId);

                seqLog.Clear().Append(seqLogPrefix).Append($" SecondaryCardNumber:{request.BCDParameters.MasterCardNumber}");
                try
                {
                    await _requestAppService.AddRequestParameters(new() { SecondaryCardNumber = request.BCDParameters.MasterCardNumber }, cardInfo.RequestId);
                    seqLog.Append($"Successfully inserted  SecondaryCardNumber {seqLog}");
                }
                catch (System.Exception)
                {
                    seqLog.Append($"Failed on inserting  SecondaryCardNumber {seqLog}");
                }

                seqAllLog.AppendLine(seqLog.ToString());
                auditLogger.Log.Information(seqLog.ToString());
            }
        }
        #endregion
    }

    async Task UpdateLocalState(StringBuilder seqLog, string seqLogPrefix, CreditCardStatus oldStatus, CreditCardStatus newStatus, decimal requestId)
    {
        seqLog.Clear().Append(seqLogPrefix).Append($" Card Old Status:{oldStatus} Card New Status:{newStatus}");
        try
        {
            await _creditCardUpdateServiceClient.reportCreditCardStatusAsync(new()
            {
                cardNo = "",
                cardStatus = ((int)newStatus).ToString(),
                updateType = "LOCAL_UPDATE",
                requestId = requestId.ToString()
            });
            seqLog.Append($"Successfully updated local card status {seqLog}");
            auditLogger.Log.Information(seqLog.ToString());
        }
        catch
        {
            seqLog.Append($"Failed on updating local card status {seqLog}");
            auditLogger.Log.Error(seqLog.ToString());
        }
    }

    async Task DeleteRequest(StringBuilder seqLog, string seqLogPrefix, CreditCardStatus oldStatus, CreditCardStatus newStatus, decimal requestId)
    {
        seqLog.Clear().Append(seqLogPrefix).Append($" Card Old Status:{oldStatus} Card New Status:{newStatus}");
        try
        {
            await _creditCardUpdateServiceClient.deletRequestAsync(new()
            {
                requestID = (long)requestId
            });
            seqLog.Append($"Successfully updated local card status {seqLog}");
            auditLogger.Log.Information(seqLog.ToString());
        }
        catch
        {
            seqLog.Append($"Failed on updating local card status {seqLog}");
            auditLogger.Log.Error(seqLog.ToString());
        }
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
                transRate = currencyData.Data?.TransferRate ?? 0,
                cardAmountSpecified = currencyData.Data?.DestAmount != null,
                transRateSpecified = currencyData.Data?.TransferRate != null
            }
        }))?.performFCCreditCardPymtWithMQ;

        var paymentResponseCode = paymentResponse?.respCode ?? string.Empty;

        if (paymentResponseCode != "0000")
            throw new ApiException(errors: null, insertSeriLog: true, message: $"Application Successfully Updated, but an error happened when Card loaded with the minimum opening balance, please contact with system administrator");

        return true;
    }

    private async Task<bool> OnBoardSupplementaryCards(CardDetailsResponse primaryCardInfo)// TODO: what to do for this flag
    {
        //not continue if its is non-primary card
        bool isNotActiveOrApprove = primaryCardInfo!.CardStatus is not (CreditCardStatus.Approved or CreditCardStatus.Active);
        bool isNotPrimary = !primaryCardInfo!.IsPrimaryCard;

        if (isNotActiveOrApprove || isNotPrimary)
            return false;


        var pendingSupplementaryCards = (await _cardDetailsAppService.GetSupplementaryCardsByRequestId(primaryCardInfo.RequestId))?.Data?.Where(x => (int)x.CardStatus < (int)CreditCardStatus.Approved);

        if (pendingSupplementaryCards?.Count() == 0) return false;

        List<Task> taskList = new();

        foreach (var supCard in pendingSupplementaryCards!)
        {
            //double check
            if ((int)supCard.CardStatus > (int)CreditCardStatus.Approved) continue;

            //Updating supplementary card status before onboarding to make status is AccountBoardingStarted
            taskList.Add(UpdateSupplementaryCardRequest(supCard));

            //Onboarding supplementary card to get card number
            taskList.Add(OnBoardSupplementaryCard(supCard));
        }

        await Task.WhenAll(taskList.ToArray());


        var onboardingResults = taskList.Where(x => x is Task<SupplementaryOnboardingStatus>)
            .Select(x => (x as Task<SupplementaryOnboardingStatus>)!.Result);

        var failedOnboarded = onboardingResults.Where(statusResponse => statusResponse.IsOnboarderd == false);
        var successOnboarded = onboardingResults.Where(statusResponse => statusResponse.IsOnboarderd == true);

        //Initiating workflow only for failed onboarding supplementary cards to update card number manually by BCD admin
        foreach (var item in failedOnboarded)
        {
            await InitiateBCDWorkFlowForSupplementary(primaryCardInfo, item.CardDetail);
        }


        foreach (var item in successOnboarded)
        {
            await UpdateSupplementaryCardNumber(new() { CivilId = item.CardDetail!.CivilId!, RequestId = item.RequestId!.Value }, item.SupplementaryCardNumber!);
        }


        return true;
    }

    async Task InitiateBCDWorkFlowForSupplementary(CardDetailsResponse primaryCardInfo, SupplementaryCardDetail supCard)
    {
        //var percentage = await workflowCalculationService.GetPercentage(primaryCardInfo.RequestId);
        //var subRequest = (await _requestAppService.GetRequestDetail((decimal)supCard.RequestId!))?.Data;

        //if (subRequest is null)
        //    return;

        //var maxPercentage = primaryCardInfo.Parameters?.MaxLimit;
        //if (primaryCardInfo.ProductType is ProductTypes.Tayseer)
        //    maxPercentage = string.IsNullOrEmpty(primaryCardInfo.Parameters?.T12MaxLimit) ? primaryCardInfo.Parameters?.T3MaxLimit : primaryCardInfo.Parameters?.T12MaxLimit;

        //var supCardHolderDOB = (await _fdrDBContext.Profiles.FirstOrDefaultAsync(x => x.CivilId == supCard.CivilId))?.Birth;
        //var IsMinorSupCardHolder = Helpers.GetAgeByDateOfBirth(supCardHolderDOB!.Value) < 21;


        RequestActivityDto requestActivity = new()
        {
            CardNumberDto = supCard.CardNumberDto,
            RequestId = (decimal)supCard.RequestId!,
            BranchId = supCard.CardData?.BranchID,
            CivilId = supCard.CivilId!,
            IssuanceTypeId = (int)IssuanceTypes.CHARGE,
            CfuActivityId = (int)CFUActivity.Supplementary_Card,
            WorkflowVariables = new()  {
            { "ProductType",  primaryCardInfo.ProductType},
            { "Description", $"{primaryCardInfo.ProductName} request for {supCard.FullName} {supCard.CivilId}" }
        }
        };

        await _requestActivityAppService.LogRequestActivity(requestActivity!, searchExist: false, isNeedWorkflow: true);
    }

    private async Task<SupplementaryCardUpdateStatus> UpdateSupplementaryCardRequest(SupplementaryCardDetail creditCard)//CreditCardUpdateServiceReference.creditCard creditCard)
    {
        var _supCard = creditCard.Adapt<CreditCardUpdateServiceReference.creditCard>();
        _supCard.ApproveDate = DateTime.Now.Date;
        _supCard.ApproveDateSpecified = true;

        if (_supCard.cardStatus == (int)CreditCardStatus.Approved)
            _supCard.cardStatus = (int)CreditCardStatus.AccountBoardingStarted;

        SupplementaryCardUpdateStatus response = new() { RequestId = _supCard.requestID };

        try
        {

            //this is temporary until integration getting fix in CreditCardUpdateServicesService.updateRequest method
            var cardRequest = await _fdrDBContext.Requests.AsNoTracking().FirstOrDefaultAsync(x => x.RequestId == _supCard.requestID);
            if (cardRequest is not null)
                _supCard.homePhone = cardRequest.HomePhone;

            var result = await _creditCardUpdateServiceClient.updateRequestAsync(new() { CreditCard = _supCard });
            response.IsUpdated = result?.updateRequestResult.isSuccessful ?? false;
            auditLogger.Log.Information($"SupplementaryCardRequest Updated Successful for {_supCard.requestID}");
        }
        catch (System.Exception ex)
        {
            response.Message = ex.Message;
        }

        return response;
    }

    private async Task<SupplementaryOnboardingStatus> OnBoardSupplementaryCard(SupplementaryCardDetail creditCard)
    {
        SupplementaryOnboardingStatus response = new() { RequestId = creditCard.RequestId, CardDetail = creditCard };

        try
        {
            var result = await AccountOnBoarding(new()
            {
                CardType = ConfigurationBase.AlOsraSupplementaryCardTypeId,
                RequestId = (long)(creditCard.RequestId ?? 0)
            });

            if (!result.IsSuccess)
                throw new ApiException(message: result.Message);

            response.IsOnboarderd = true;
            response.SupplementaryCardNumber = result?.Data;
            auditLogger.Log.Information($"SupplementaryCard AccountOnBoarding Successful for {creditCard.RequestId}");
        }
        catch (System.Exception ex)
        {
            response.Message = ex.Message;
        }

        return response;
    }

    private async Task<bool> UpdateSupplementaryCardNumber(CardDetailsResponse? cardInfo, string newCardNumber)
    {
        if (string.IsNullOrEmpty(newCardNumber)) return false;

        if (cardInfo?.Parameters?.CardType is not ConfigurationBase.SupplementaryChargeCard)
            return false;

        var supplementaryCard = await _fdrDBContext.PreregisteredPayees.AsNoTracking().FirstOrDefaultAsync(x => x.CivilId == cardInfo.PrimaryCardCivilId && x.CardNo == cardInfo.RequestId.ToString());
        if (supplementaryCard is null)
            return true;


        //we cannot update CardNo which is part of key so we are removing record and add it again
        _fdrDBContext.PreregisteredPayees.Remove(supplementaryCard);
        await _fdrDBContext.SaveChangesAsync();


        supplementaryCard.CardNo = newCardNumber;
        _fdrDBContext.PreregisteredPayees.Add(supplementaryCard);
        await _fdrDBContext.SaveChangesAsync();

        return true;
    }

    private async Task<ChangeStatusResponse> UpdateCardStatusBeforeApproval(RequestParameterDto? parameters, UpdateRequestDto request, RequestActionType actionType = RequestActionType.FullEdit)
    {
        var requestedStatus = (CreditCardStatus)request.CardStatus;

        var creditCard = request.Adapt<CreditCardUpdateServiceReference.creditCard>();
        creditCard.cardStatus = (int)await GetCardStatusBeforeApproval();
        creditCard.requestDateSpecified = creditCard.requestDate != DateTime.MinValue;
        creditCard.expirySpecified = creditCard.expiry != DateTime.MinValue;

        if (requestedStatus is CreditCardStatus.Approved)
            creditCard.ApproveDateSpecified = creditCard.ApproveDate != DateTime.MinValue;

        var updateResult = (await _creditCardUpdateServiceClient.updateRequestAsync(new()
        {
            CreditCard = creditCard
        }))?.updateRequestResult;

        if (updateResult == null || (updateResult != null && !updateResult.isSuccessful))
            throw new ApiException(errors: null, message: $"Unable to update credit card data {updateResult?.description}");

        return new()
        {
            CardStatus = requestedStatus,
            RequestID = request.RequestID,
            Description = updateResult?.status ?? string.Empty
        };

        #region local methods

        async Task<(bool isSupplementaryCard, bool isChargeCard)> GetCardType()
        {
            int productType = await _cardDetailsAppService.GetPayeeProductType(request?.CardType);
            bool isSupplementaryCard = parameters?.CardType == ConfigurationBase.SupplementaryChargeCard;
            bool isChargeCard = productType == ConfigurationBase.PrimaryChargeCardPayeeTypeId;
            return (isSupplementaryCard, isChargeCard);
        }

        async Task<CreditCardStatus> GetCardStatusBeforeApproval()
        {
            if (request is null)
                throw new ApiException(message: "Request value is null");

            var customerProfile = (await _fdrDBContext.Profiles.FirstOrDefaultAsync(x => x.CivilId == request.CivilID)) ?? throw new ApiException(message: "Customer profile not exist");
            //var customerProfile = (await _bankingCustomerProfileServiceClient.viewBankingCustomerProfileAsync(new() { civilID = request?.CivilID }))?.viewBankingCustomerProfileResult
            //?? throw new ApiException(message: "RIM is not active or does not exist");

            bool isMinor = Helpers.GetAgeByDateOfBirth(customerProfile!.Birth) < 21;
            string oldCardNumber = parameters?.OldCardNumberEncrypted ?? string.Empty;
            bool isBcdUser = _authManager.HasPermission(Permissions.AccountBoardingRequest);// true; //TODO : Should take from LoggedIn User
            (bool isSupplementaryCard, bool isChargeCard) = await GetCardType();

            //Changing status to pending for minor approval for minor customer
            //Rule 1: Approver is not BCD 
            //Rule 2: Should be charge card
            //Rule 3: Should not be SupplementaryCard
            if (isMinor)
                if (!isBcdUser && isChargeCard && !isSupplementaryCard && actionType != RequestActionType.MinorsChargeCardHoldersApproval)
                {
                    return CreditCardStatus.PendingForMinorsApproval;
                }

            if (request.CardStatus == (int)CreditCardStatus.Approved)
            {
                return (string.IsNullOrEmpty(oldCardNumber) ? CreditCardStatus.AccountBoardingStarted : CreditCardStatus.CardUpgradeStarted);
            }

            return (CreditCardStatus)request.CardStatus;
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
            throw new ApiException(message: "Cannot approve.  The limit is too low for the product.");
        if (approveLimit > Convert.ToInt32(cardDef.MaxLimit))
            throw new ApiException(message: "Cannot approve.  The limit is too high for the product.");

        await Task.CompletedTask;
    }


    private async Task PreApprovalValidation(CardDetailsResponse cardInfo, UpdateRequestDto updateRequestDto)
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

        string oldCardNumber = cardInfo.Parameters?.OldCardNumberEncrypted!;

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

        var corporateProfile = await _corporateProfileAppService.GetProfile(corporateCivilId)
            ?? throw new ApiException(errors: null, message: "Invalid Corporate Civil ID");


        //decimal remainingLimit = corporateProfile.Data!.RemainingLimit;
        //decimal newLimit = (decimal)(approveLimit ?? 0);

        //if (remainingLimit < newLimit)
        //    throw new ApiException(errors: null, message: "The available corporate limit is less than card limit");

        return corporateProfile.Data;
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
    #endregion

}
