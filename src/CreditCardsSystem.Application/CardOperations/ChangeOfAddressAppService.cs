using CreditCardsSystem.Data;
using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Models.Access;
using CreditCardsSystem.Domain.Models.CardIssuance;
using CreditCardsSystem.Domain.Models.CardOperation;
using CreditCardsSystem.Domain.Models.RequestActivity;
using CreditCardsSystem.Domain.Models.Workflow;
using CreditCardsSystem.Domain.Shared.Models.Reports;
using CreditCardsSystem.Domain.Shared.Models.RequestActivity;
using CreditCardsSystem.Utility.Extensions;
using CreditCardUpdateServiceReference;
using Kfh.Aurora.Auth;
using Kfh.Aurora.Common.Shared.Interfaces.Customer;
using Kfh.Aurora.Integration;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace CreditCardsSystem.Application.CardOperations;

public class ChangeOfAddressAppService : BaseRequestActivity, IChangeOfAddressAppService, IAppService
{
    #region Private Fields
    private readonly CreditCardUpdateServicesServiceClient _creditCardUpdateServiceClient;

    private readonly ICardDetailsAppService _cardDetailsAppService;
    private readonly IEmployeeAppService _employeeAppService;
    private readonly IRequestActivityAppService _requestActivityAppService;
    private readonly ICustomerProfileAppService _customerProfileAppService;
    private readonly IAddressAppService _addressService;
    private readonly IAuthManager authManager;
    private readonly FdrDBContext _fdrDBContext;
    private readonly ICustomerProfileCommonApi customerProfileCommonApi;

    public ChangeOfAddressAppService(
        IIntegrationUtility integrationUtility,
        IOptions<IntegrationOptions> options,
        ICardDetailsAppService cardDetailsAppService,
        IEmployeeAppService employeeAppService,
        IRequestActivityAppService requestActivityAppService,
        ICustomerProfileAppService customerProfileAppService,
        IAddressAppService addressService,
        IAuthManager authManager,
        FdrDBContext fdrDBContext,
        ICustomerProfileCommonApi customerProfileCommonApi) : base(requestActivityAppService)
    {
        _creditCardUpdateServiceClient = integrationUtility.GetClient<CreditCardUpdateServicesServiceClient>(options.Value.Client, options.Value.Endpoints.CreditCardUpdate, options.Value.BypassSslValidation);
        _cardDetailsAppService = cardDetailsAppService;
        _employeeAppService = employeeAppService;
        _requestActivityAppService = requestActivityAppService;
        _customerProfileAppService = customerProfileAppService;
        _addressService = addressService;
        this.authManager = authManager;
        _fdrDBContext = fdrDBContext;
        this.customerProfileCommonApi = customerProfileCommonApi;
    }




    #endregion

    #region public methods
    [HttpPost]
    public async Task<ApiResponseModel<ChangeOfDetailResponse>> RequestChangeOfAddress([FromBody] ChangeOfAddressRequest request)
    {

        await ValidateBiometricStatus(request.RequestId);

        if (!authManager.HasPermission(Permissions.ChangeBillingAddress.Request()))
            return Failure<ChangeOfDetailResponse>(GlobalResources.NotAuthorized);


        await request.ModelValidationAsync();

        var cardInfo = (await _cardDetailsAppService.GetCardInfoMinimal(request.RequestId) ?? throw new ApiException(message: "Invalid request Id"))?.Data;

        var customerProfileTask = _customerProfileAppService.GetCustomerProfileMinimal(new() { CivilId = cardInfo.CivilId });
        var billingAddressTask = _addressService.GetRecentBillingAddress(cardInfo!.CivilId!, null);


        Task.WaitAll(customerProfileTask, billingAddressTask);

        var customerProfile = (await customerProfileTask ?? throw new ApiException(message: "Unable to fetch card info")).Data;
        var currentBillingAddress = (await billingAddressTask)?.Data!;

        bool IsYouthCard = ConfigurationBase.YouthCardTypes.Split(",").Any(x => x == cardInfo!.CardType.ToString());

        await ValidateAddressChangeRequest();

        var requestActivity = new RequestActivityDto()
        {
            CfuActivityId = (int)CFUActivity.CHANGE_BILLING_ADDRESS,
            IssuanceTypeId = (int)cardInfo.IssuanceType,
            CardType = cardInfo.CardType,
            CardNumber = cardInfo.CardNumber,
            CivilId = cardInfo.CivilId,
            RequestId = cardInfo.RequestId,
            CustomerName = $"{customerProfile!.FirstName} {customerProfile.LastName}",
            Details = new()
            {
            { ReportingConstants.KEY_CREDIT_CARD_NO, cardInfo.CardNumber ?? "" },
            { ReportingConstants.KEY_ADDRESSLINE1, request.BillingAddress.AddressLine1 },
            { ReportingConstants.KEY_ADDRESSLINE2, request.BillingAddress.AddressLine2 },
            { ReportingConstants.KEY_AREA, request.BillingAddress.City ?? "" },
            { ReportingConstants.KEY_HOME_PHONE, request.BillingAddress.HomePhone.ToString() ?? "" },
            { ReportingConstants.KEY_MOBILE_NO, request.BillingAddress.Mobile.ToString() ?? "" },
            { ReportingConstants.KEY_WORK_PHONE_NO, request.BillingAddress.WorkPhone.ToString() ?? "" },
            { ReportingConstants.KEY_ZIP_CODE, request.BillingAddress.PostalCode.ToString() ?? "" },
            { ReportingConstants.KEY_STREET, request.BillingAddress.Street },
            { ReportingConstants.KEY_PO_BOX, request.BillingAddress.Street.StartsWith("P.O.BOX") ? request.BillingAddress.PostOfficeBoxNumber.ToString() ?? "" : "0" },
            { ReportingConstants.KEY_CARD_HOLDER_NAME, request.NewCardHolderName }
            },
            WorkflowVariables = new() {
            { WorkflowVariables.IsYouthCard, IsYouthCard },
            { WorkflowVariables.Description, $"Request address change for {cardInfo.CivilId}" },
            { WorkflowVariables.CardNumber, cardInfo.CardNumberDto ?? "" },
            { WorkflowVariables.AddressLine1, request.BillingAddress.AddressLine1 },
            { WorkflowVariables.AddressLine2, request.BillingAddress.AddressLine2 },
            { WorkflowVariables.AreaId, request.BillingAddress.AreaId ?? "" },
            { WorkflowVariables.City, request.BillingAddress.City ?? "" },
            { WorkflowVariables.HomePhone, request.BillingAddress.HomePhone.ToString() ?? "" },
            { WorkflowVariables.MobilePhone, request.BillingAddress.Mobile.ToString() ?? "" },
            { WorkflowVariables.WorkPhone, request.BillingAddress.WorkPhone.ToString() ?? "" },
            { WorkflowVariables.ZipCode, request.BillingAddress.PostalCode.ToString() ?? "" },
            { WorkflowVariables.Street, request.BillingAddress.Street },
            { WorkflowVariables.PoBoxNumber, request.BillingAddress.Street.StartsWith("P.O.BOX") ? request.BillingAddress.PostOfficeBoxNumber.ToString() ?? "" : "0" },
            { WorkflowVariables.NewCardHolderName, request.NewCardHolderName }
            }
        };

        await _requestActivityAppService.LogRequestActivity(requestActivity, isNeedWorkflow: true);

        return Success<ChangeOfDetailResponse>(new(), message: GlobalResources.WaitingForApproval);
        #region local functions
        async Task ValidateAddressChangeRequest()
        {
            var currentBillingAddressHashCode = JsonConvert.SerializeObject(currentBillingAddress).GetHashCode();
            var requestedBillingAddressHashCode = JsonConvert.SerializeObject(request.BillingAddress).GetHashCode();

            if (currentBillingAddressHashCode == requestedBillingAddressHashCode)
                throw new ApiException(message: "No change found in input address."); ;

            #region Checking is there any pending request activity
            var pendingActivityRequest = new PendingActivityRequest(cardInfo.CardNumber, new[] { CFUActivity.CHANGE_BILLING_ADDRESS, CFUActivity.CHANGE_CARDHOLDERNAME, CFUActivity.CHANGE_CARD_LINKED_ACCT });

            var pendingActivities = (await _requestActivityAppService.GetPendingActivities(pendingActivityRequest))?.Data;
            bool HasPendingAddress = pendingActivities?.Any(x => x.CfuActivity == CFUActivity.CHANGE_BILLING_ADDRESS) ?? false;
            //bool HasPendingName = pendingActivities?.Any(x => x.CfuActivity == CFUActivity.CHANGE_CARDHOLDERNAME) ?? false;
            //bool HasPendingAccount = pendingActivities?.Any(x => x.CfuActivity == CFUActivity.CHANGE_CARD_LINKED_ACCT) ?? false;

            #endregion

            if (HasPendingAddress)
                throw new ApiException(message: "Change request is pending, please contact Checker.");

            //if (HasPendingName && !string.IsNullOrEmpty(request.NewCardHolderName))
            //    throw new ApiException(message: "There is a pending request on change holder name, please contact Checker.");

            //if (HasPendingAccount && !string.IsNullOrEmpty(request.NewLinkedAccountNumber))
            //    throw new ApiException(message: "There is a pending request on linked account number, please contact Checker.");
        }

        #endregion
    }



    [HttpPost]
    public async Task<ApiResponseModel<ChangeOfDetailResponse>> RequestChangeCardHolderName([FromBody] ChangeHolderNameRequest request)
    {

        await ValidateBiometricStatus(request.RequestId);


        if (!authManager.HasPermission(Permissions.ChangeHolderName.Request()))
            return Failure<ChangeOfDetailResponse>(GlobalResources.NotAuthorized);

        await request.ModelValidationAsync();
        var cardInfo = (await _cardDetailsAppService.GetCardInfo(request.RequestId, includeCardBalance: false) ?? throw new ApiException(message: "Invalid request Id"))?.Data;
        var customerProfile = (await _customerProfileAppService.GetCustomerProfileMinimal(new() { CivilId = cardInfo.CivilId }) ?? throw new ApiException(message: "Unable to fetch card info")).Data;

        await ValidateRequest();

        var requestActivity = new RequestActivityDto()
        {
            CfuActivityId = (int)CFUActivity.CHANGE_CARDHOLDERNAME,
            IssuanceTypeId = (int)cardInfo.IssuanceType,
            CardType = cardInfo.CardType,
            CardNumber = cardInfo.CardNumber,
            CivilId = cardInfo.CivilId,
            RequestId = cardInfo.RequestId,
            CustomerName = $"{customerProfile!.FirstName} {customerProfile.LastName}"
        };

        await UpdateCardHolderName();

        return Success<ChangeOfDetailResponse>(new(), message: GlobalResources.WaitingForApproval);
        #region local functions
        async Task ValidateRequest()
        {
            #region Checking is there any pending request activity
            var pendingActivityRequest = new PendingActivityRequest(cardInfo.CardNumber, new[] { CFUActivity.CHANGE_CARDHOLDERNAME });

            var pendingActivities = (await _requestActivityAppService.GetPendingActivities(pendingActivityRequest))?.Data;
            bool HasPendingName = pendingActivities?.Any(x => x.CfuActivity == CFUActivity.CHANGE_CARDHOLDERNAME) ?? false;

            #endregion


            if (HasPendingName && !string.IsNullOrEmpty(request.NewCardHolderName))
                throw new ApiException(message: "There is a pending request on change holder name, please contact Checker.");

            if (request.NewCardHolderName.Trim().Length > 27)
                throw new ApiException([new(nameof(request.NewCardHolderName), "Embossing card holder name should not exceed 27 letters")], "", "validation error");
        }

        async Task UpdateCardHolderName()
        {

            if (string.IsNullOrEmpty(request.NewCardHolderName) || cardInfo.HolderEmbossName == request.NewCardHolderName) return;

            requestActivity.Details = new()
        {
            { ReportingConstants.KEY_OLD_EMBOSSING_NAME, cardInfo.HolderEmbossName ?? "" },
            { ReportingConstants.KEY_CARD_HOLDER_NAME, request.NewCardHolderName }
        };

            requestActivity.WorkflowVariables = new() {
                { "Description", $"Request card holder name change for { cardInfo.CivilId}" },
                 { WorkflowVariables.OldCardHolderName, cardInfo.HolderEmbossName ?? "" },
                { WorkflowVariables.NewCardHolderName, request.NewCardHolderName },
                { WorkflowVariables.CardNumber, cardInfo.CardNumberDto??"" }

            };
            requestActivity.RequestActivityId = await _requestActivityAppService.LogRequestActivity(requestActivity, isNeedWorkflow: true);
        }


        #endregion
    }

    [HttpPost]
    public async Task<ApiResponseModel<ChangeOfDetailResponse>> RequestChangeLinkedAccount([FromBody] ChangeLinkedAccountRequest request)
    {
        await ValidateBiometricStatus(request.RequestId);

        if (!authManager.HasPermission(Permissions.ChangeLinkedAccount.Request()))
            return Failure<ChangeOfDetailResponse>(GlobalResources.NotAuthorized);

        await request.ModelValidationAsync();
        var cardInfo = (await _cardDetailsAppService.GetCardInfoMinimal(request.RequestId) ?? throw new ApiException(message: "Invalid request Id"))?.Data;
        var customerProfile = (await _customerProfileAppService.GetCustomerProfileMinimal(new() { CivilId = cardInfo.CivilId }) ?? throw new ApiException(message: "Unable to fetch card info")).Data;


        await ValidateRequest();

        var requestActivity = new RequestActivityDto()
        {
            CfuActivityId = (int)CFUActivity.CHANGE_CARD_LINKED_ACCT,
            IssuanceTypeId = (int)cardInfo.IssuanceType,
            CardType = cardInfo.CardType,
            CardNumber = cardInfo.CardNumber,
            CivilId = cardInfo.CivilId,
            RequestId = cardInfo.RequestId,
            CustomerName = $"{customerProfile!.FirstName} {customerProfile.LastName}"
        };

        await UpdateLinkedAccount();


        return Success<ChangeOfDetailResponse>(new(), message: GlobalResources.WaitingForApproval);
        #region local functions
        async Task ValidateRequest()
        {
            #region Checking is there any pending request activity
            var pendingActivityRequest = new PendingActivityRequest(cardInfo.CardNumber, new[] { CFUActivity.CHANGE_CARDHOLDERNAME });

            var pendingActivities = (await _requestActivityAppService.GetPendingActivities(pendingActivityRequest))?.Data;
            bool HasPendingAccount = pendingActivities?.Any(x => x.CfuActivity == CFUActivity.CHANGE_CARD_LINKED_ACCT) ?? false;

            #endregion


            if (HasPendingAccount && !string.IsNullOrEmpty(request.NewLinkedAccountNumber))
                throw new ApiException(message: "There is a pending request on linked account number, please contact Checker.");
        }



        async Task UpdateLinkedAccount()
        {
            if (string.IsNullOrEmpty(request.NewLinkedAccountNumber) || cardInfo.BankAccountNumber == request.NewLinkedAccountNumber) return;



            requestActivity.Details = new()
        {
            { ReportingConstants.KEY_BILLING_ACCOUNT_NO, request.NewLinkedAccountNumber }
        };
            requestActivity.WorkflowVariables = new()
        {
                { "Description", $"Request card link account change for {cardInfo.CivilId}" },
                { WorkflowVariables.PreviousBillingAccountNumber, cardInfo.BankAccountNumber??"" },
            { WorkflowVariables.BillingAccountNumber, request.NewLinkedAccountNumber },
                        { WorkflowVariables.CardNumber, cardInfo.CardNumberDto??"" }
            };

            requestActivity.RequestActivityId = await _requestActivityAppService.LogRequestActivity(requestActivity, isNeedWorkflow: true);
        }
        #endregion
    }

    [HttpPost]
    public async Task<ApiResponseModel<ProcessResponse>> ProcessChangeOfAddressRequest([FromBody] ProcessChangeOfAddressRequest request)
    {
        var requestActivity = (await _requestActivityAppService.GetRequestActivityById(request.RequestActivityId)).Data ?? throw new ApiException(message: "Unable to find Request");

        string permission = requestActivity.CfuActivity switch
        {
            CFUActivity.CHANGE_BILLING_ADDRESS => Permissions.ChangeBillingAddress.EnigmaApprove(),
            CFUActivity.CHANGE_CARDHOLDERNAME => Permissions.ChangeHolderName.EnigmaApprove(),
            CFUActivity.CHANGE_CARD_LINKED_ACCT => Permissions.ChangeLinkedAccount.EnigmaApprove(),
            _ => "anonymous"
        };

        if (!authManager.HasPermission(permission))
            return Failure<ProcessResponse>(GlobalResources.NotAuthorized);


        request.CardNumber = requestActivity.CardNumber.Masked(6, 6);
        request.Activity = requestActivity.CfuActivity;

        if (request.ActionType == ActionType.Rejected)
        {
            await _requestActivityAppService.CompleteActivity(request);
            return Success(new ProcessResponse(), message: "Successfully Rejected !");
        }

        return await Approve();

        #region local functions
        async Task<ApiResponseModel<ProcessResponse>> Approve()
        {

            await ApprovalValidation();


            if (requestActivity.CfuActivity == CFUActivity.CHANGE_BILLING_ADDRESS)
            {
                BillingAddressModel newBillingAddress = new()
                {
                    City = requestActivity.Details.GetValueOrDefault(ReportingConstants.KEY_AREA, ""),
                    HomePhone = Convert.ToInt64(requestActivity.Details.GetValueOrDefault(ReportingConstants.KEY_HOME_PHONE)),
                    Mobile = Convert.ToInt64(requestActivity.Details.GetValueOrDefault(ReportingConstants.KEY_MOBILE_NO)),
                    WorkPhone = Convert.ToInt64(requestActivity.Details.GetValueOrDefault(ReportingConstants.KEY_WORK_PHONE_NO)),
                    PostalCode = Convert.ToInt32(requestActivity.Details.GetValueOrDefault(ReportingConstants.KEY_ZIP_CODE)),
                    Street = requestActivity.Details.GetValueOrDefault(ReportingConstants.KEY_STREET)
                };
                await _addressService.UpdateBillingAddress(new(requestActivity.CardNumber, newBillingAddress));
            }

            if (requestActivity.CfuActivity == CFUActivity.CHANGE_CARDHOLDERNAME)
            {
                var changeEmbossNameResult = (await _creditCardUpdateServiceClient.updateCardEmbossedNameAsync(new()
                {
                    embosserUpdRequest = new()
                    {
                        embossedName1 = requestActivity.Details[ReportingConstants.KEY_CARD_HOLDER_NAME].ToUpper(),
                        requestID = (long)requestActivity.RequestId,
                        requestIDSpecified = true
                    }
                }))?.updateCardEmbossedName;

                if (!changeEmbossNameResult.isSuccessful)
                {
                    return Failure<ProcessResponse>(message: $"Unable to update card holder name :{changeEmbossNameResult.description}");
                }
            }

            if (requestActivity.CfuActivity == CFUActivity.CHANGE_CARD_LINKED_ACCT)
            {
                var changeAccountResult = (await _creditCardUpdateServiceClient.updateCardLinkedAccountAsync(new()
                {
                    accountNo = requestActivity.Details[ReportingConstants.KEY_BILLING_ACCOUNT_NO],
                    requestID = (long)requestActivity.RequestId
                })).updateCardLinkedAccount;

                if (!changeAccountResult.isSuccessful)
                {
                    return Failure<ProcessResponse>(message: $"Unable to change linked account number :{changeAccountResult.description}");
                }
            }

            await _requestActivityAppService.CompleteActivity(request);

            return Success(new ProcessResponse(), message: "Successfully Approved !");

            /// Filter non-closed supplementary cards
            #region local functions

            async Task ApprovalValidation()
            {
                //maker cannot approve his own request
                if (authManager.GetUser()?.KfhId == requestActivity.TellerId.ToString("0"))
                    throw new ApiException(message: GlobalResources.MakerCheckerAreSame);

                await Task.CompletedTask;
            }


            #endregion
        }
        #endregion
    }
    #endregion


    private async Task ValidateBiometricStatus(decimal requestId)
    {
        var request = _fdrDBContext.Requests.AsNoTracking().FirstOrDefault(x => x.RequestId == requestId) ?? throw new ApiException(message: "Invalid request Id ");
        var bioStatus = await customerProfileCommonApi.GetBiometricStatus(request!.CivilId);
        if (bioStatus.ShouldStop)
            throw new ApiException(message: GlobalResources.BioMetricRestriction);
    }
}
