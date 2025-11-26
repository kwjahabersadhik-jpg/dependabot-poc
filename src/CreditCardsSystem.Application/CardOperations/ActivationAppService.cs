using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Models.Access;
using CreditCardsSystem.Domain.Models.Card;
using CreditCardsSystem.Domain.Models.CardOperation;
using CreditCardsSystem.Domain.Models.Workflow;
using CreditCardsSystem.Domain.Shared.Models.Reports;
using CreditCardsSystem.Domain.Shared.Models.RequestActivity;
using CreditCardsSystem.Utility.Crypto;
using CreditCardsSystem.Utility.Extensions;
using CreditCardUpdateServiceReference;
using Kfh.Aurora.Auth;
using Kfh.Aurora.Integration;
using Kfh.Aurora.Logging;
using Kfh.Aurora.Organization;
using System.Data;
using System.ServiceModel.Channels;

namespace CreditCardsSystem.Application.CardOperations;

public class ActivationAppService(IAuthManager authManager,
                                  IIntegrationUtility integrationUtility,
                                  IOptions<IntegrationOptions> options,
                                  ICardDetailsAppService cardDetailsAppService,
                                  IEmployeeAppService employeeAppService,
                                  IRequestActivityAppService requestActivityAppService,
                                  ICustomerProfileAppService customerProfileAppService,
                                  IRequestAppService requestAppService,
                                  IAuditLogger<ActivationAppService> auditLogger,
                                  IDeliveryAppService deliveryService,
                                  IConfigurationAppService configurationAppService) : BaseApiResponse, IActivationAppService, IAppService
{
    #region Private Fields
    private readonly CreditCardUpdateServicesServiceClient _creditCardUpdateServiceClient = integrationUtility.GetClient<CreditCardUpdateServicesServiceClient>(options.Value.Client, options.Value.Endpoints.CreditCardUpdate, options.Value.BypassSslValidation);
    private readonly IAuthManager _authManager = authManager;
    private readonly ICardDetailsAppService _cardDetailsAppService = cardDetailsAppService;
    private readonly IEmployeeAppService _employeeAppService = employeeAppService;
    private readonly IRequestActivityAppService _requestActivityAppService = requestActivityAppService;
    private readonly ICustomerProfileAppService _customerProfileAppService = customerProfileAppService;
    private readonly IRequestAppService _requestAppService = requestAppService;
    private readonly IAuditLogger<ActivationAppService> _auditLogger = auditLogger;
    private readonly IDeliveryAppService _deliveryService = deliveryService;
    private readonly IConfigurationAppService _configurationAppService = configurationAppService;
    #endregion

    #region Public methods
    [HttpPost]
    public async Task<ApiResponseModel<List<CardActivationStatus>>> RequestCardReActivation([FromBody] CardReActivationRequest request)
    {
        var cardInfo = (await _cardDetailsAppService.GetCardInfo(request.RequestId) ?? throw new ApiException(message: "Unable to fetch card info"))?.Data;

        await IsValidForReActivation(cardInfo);

        var customerProfile = (await _customerProfileAppService.GetCustomerProfile(cardInfo!.CivilId) ?? throw new ApiException(message: "Unable to fetch card info")).Data;
        var requestActivity = new RequestActivityDto()
        {
            IssuanceTypeId = (int)cardInfo.IssuanceType,
            CardType = cardInfo.CardType,
            CardNumber = cardInfo.CardNumber,
            BranchId = request.BranchId,
            CivilId = cardInfo.CivilId,
            RequestId = cardInfo.RequestId,
            CustomerName = $"{customerProfile!.FirstName} {customerProfile.LastName}"
        };

        //we are taking kfhid from api request. example like when AI & dashboard application using credit card api
        if (request.KfhId is not null)
            requestActivity.TellerId = request.KfhId.Value;

        //Creating request activity for card re-activation
        if (request.BranchId != 995)
            return await RequestCreateForReActivation();

        //Reactivate without approval
        return await ReActivateDirectly();

        #region local functions
        async Task<ApiResponseModel<List<CardActivationStatus>>> ReActivateDirectly()
        {
            try
            {
                await _creditCardUpdateServiceClient.reportCreditCardStatusAsync(new()
                {
                    cardNo = cardInfo.CardNumber,
                    cardStatus = ConfigurationBase.MQ_CARD_STATUS_ACTIVE,
                });
            }
            catch (System.Exception ex)
            {
                throw new System.Exception(message: $"Failed to re-active this card {cardInfo.CardNumber.Masked(6, 6)}", innerException: ex.InnerException);
            }

            requestActivity.CfuActivityId = (int)CFUActivity.CHANGE_CARD_STATUS;
            requestActivity.RequestActivityStatusId = (int)RequestActivityStatus.Approved;
            requestActivity.Details = new()
        {
            { ReportingConstants.KEY_OLD_CARD_STATUS,cardInfo.CardStatus.ToString()},
            { ReportingConstants.KEY_NEW_CARD_STATUS, CreditCardStatus.Active.ToString()}
        };
            requestActivity.WorkflowVariables = new()
        {
            { WorkflowVariables.PreviousStatus,cardInfo.CardStatus.ToString()},
            { WorkflowVariables.NewStatus, CreditCardStatus.Active.ToString()}
        };

            await _requestActivityAppService.LogRequestActivity(requestActivity);

            return Success(new List<CardActivationStatus>() { new()
            {
                CardNumber = cardInfo.CardNumber!, IsActivated = true, Message = "Successfully Re-Activated" }
            },
            message: "Successfully Re-Activated");
        }
        async Task<ApiResponseModel<List<CardActivationStatus>>> RequestCreateForReActivation()
        {
            requestActivity!.CfuActivityId = (int)CFUActivity.CARD_RE_ACTIVATION;
            requestActivity.IsCorporateActivity = false;

            requestActivity.Details = new()
        {
            { ReportingConstants.KEY_CREDIT_CARD_NO, cardInfo.CardNumber! },
            { ReportingConstants.KEY_IS_MASTER_CARD, request.IsMasterCard.ToString() }
        };
            requestActivity.WorkflowVariables = new() {
                { "Description", $"Re - Activation" } ,
                { WorkflowVariables.CardNumber, cardInfo.CardNumberDto! },
                { WorkflowVariables.IsMasterCard,  request.IsMasterCard.ToString() }
            };


            await _requestActivityAppService.LogRequestActivity(requestActivity, isNeedWorkflow: true);

            return Success(new List<CardActivationStatus>() { new() {
                CardNumber = cardInfo.CardNumber.Masked(6,6)!, IsActivated = false,
                Message = "Created new request for Re-Activation" } },
                message: GlobalResources.WaitingForApproval);
        }

        #endregion
    }

    [HttpPost]
    public async Task<ApiResponseModel<ProcessResponse>> ProcessCardReActivationRequest([FromBody] ActivityProcessRequest request)
    {


        if (!authManager.HasPermission(Permissions.CardReActivate.EnigmaApprove()))
            return Failure<ProcessResponse>(GlobalResources.NotAuthorized);

        //var requestActivity = (await _requestActivityAppService.GetAllRequestActivity(new() { RequestActivityID = request.RequestActivityId })).Data?.Single() ?? throw new ApiException(message: "Unable to find Request");
        var requestActivity = (await _requestActivityAppService.GetRequestActivityById(request.RequestActivityId)).Data ?? throw new ApiException(message: "Unable to find Request");

        var cardInfo = (await _cardDetailsAppService.GetCardInfo(requestActivity.RequestId) ?? throw new ApiException(message: "Unable to fetch card info"))?.Data;
        await IsValidForReActivation(cardInfo);

        var currentUser = _authManager.GetUser();
        request.CardNumber = cardInfo.CardNumber.Masked(6, 6);
        request.Activity = CFUActivity.CARD_RE_ACTIVATION;

        if (request.ActionType == ActionType.Rejected)
        {
            await _requestActivityAppService.CompleteActivity(request);

            return Success<ProcessResponse>(new()
            {
                CardNumber = cardInfo!.CardNumber!.Masked(6, 6),
                Message = "Successfully Rejected"
            }, message: "Successfully Rejected");
        }

        return await Approve();

        #region local functions
        async Task<ApiResponseModel<ProcessResponse>> Approve()
        {

            await IsValidForApproval();

            try
            {
                await _creditCardUpdateServiceClient.reportCreditCardStatusAsync(new()
                {
                    cardNo = cardInfo.CardNumber,
                    cardStatus = ConfigurationBase.MQ_CARD_STATUS_ACTIVE,
                });
            }
            catch (System.Exception ex)
            {
                throw new System.Exception(message: $"Unable to re-active this card, please try again later! {ex.Message}", innerException: ex.InnerException);
            }


            //TODO: pls check the RequestActivityId datatype
            //await _requestActivityAppService.UpdateRequestActivityStatus(new() { IssuanceTypeId = (int)cardInfo.IssuanceType, CivilId = cardInfo.CivilId, RequestActivityId = (int)request.RequestActivityId, RequestActivityStatusId = (int)RequestActivityStatus.Approved });

            await _requestActivityAppService.CompleteActivity(request);

            return Success<ProcessResponse>(new() { CardNumber = cardInfo!.CardNumber!, Message = GlobalResources.SuccessApproval }, message: GlobalResources.SuccessApproval);
        }


        async Task IsValidForApproval()
        {
            if (decimal.TryParse(currentUser!.KfhId, out decimal _userId) && _userId == requestActivity.TellerId)
                throw new ApiException(message: "The request cannot be approved by the request maker");

            if (requestActivity.WorkflowVariables?.ContainsKey(WorkflowVariables.IsMasterCard) ?? false)
            {
                if (bool.TryParse(requestActivity.WorkflowVariables[WorkflowVariables.IsMasterCard].ToString(), out bool _isMasterCard) && _isMasterCard)
                {
                    bool isSalted = cardInfo.Parameters!.SecondaryCardNumber?.Length > 16;
                    cardInfo!.CardNumber = isSalted ? cardInfo.Parameters!.SecondaryCardNumber.DeSaltThis() : cardInfo.Parameters!.SecondaryCardNumber;
                }
            }
            

            await Task.CompletedTask;
        }
        #endregion
    }

    //TODO: Authorize
    [HttpPost]
    public async Task<ApiResponseModel<List<CardActivationStatus>>> ActivateSingleCard([FromBody] CardActivationRequest request)
    {

        if (!_authManager.HasPermission(Permissions.CardActivation.Request()))
            return Failure<List<CardActivationStatus>>(GlobalResources.NotAuthorized);

        var response = new ApiResponseModel<List<CardActivationStatus>>();

        var requestDetail = (await _requestAppService.GetRequestDetail(request.RequestId) ?? throw new ApiException(message: "Unable to find request detail")).Data;
        var cardExtension = await _cardDetailsAppService.GetCardWithExtension(requestDetail!.CardType) ?? throw new ApiException(message: "Unable to find card info");
        var customerProfile = (await _customerProfileAppService.GetCustomerProfile(requestDetail.CivilId) ?? throw new ApiException(message: "Unable to find card info")).Data;
        bool isTayseer = cardExtension.ProductType == ProductTypes.Tayseer;
        var issuanceType = Helpers.GetIssuanceType(cardExtension.ProductType);

        if (isTayseer && string.IsNullOrEmpty(requestDetail.FdAcctNo))
            throw new ApiException(message: "card activation is pending as the FD account number is not available.");

        string? cardOrFDAccountNumber = isTayseer ? requestDetail.FdAcctNo : requestDetail.CardNo;

        if (string.IsNullOrEmpty(cardOrFDAccountNumber))
            throw new ApiException(message: "Invalid card or FD account number!");

        var activatedCards = await PerformCardActivation([cardOrFDAccountNumber], response.ValidationErrors);

        response.ValidationErrors.ThrowErrorsIfAny();

        var Approver = (await _employeeAppService.ValidateSellerId(_authManager.GetUser()?.KfhId) ?? throw new ApiException(message: "Invalid Approver Id")).Data;

        Branch userBranch = await _configurationAppService.GetUserBranch();

        var requestActivity = new RequestActivityDto()
        {
            IssuanceTypeId = (int)issuanceType,
            BranchId = userBranch.BranchId,
            CardType = requestDetail.CardType,
            CardNumber = requestDetail.CardNo,
            CivilId = requestDetail.CivilId,
            RequestId = requestDetail.RequestId,
            CustomerName = $"{customerProfile!.FirstName} {customerProfile.LastName}",
            CfuActivityId = (int)CFUActivity.CHANGE_CARD_STATUS,
            RequestActivityStatusId = (int)RequestActivityStatus.Approved,
            //IssuanceTypeId = (int)cardDef.ProductType,
            ApproverId = Convert.ToInt32(Approver!.EmpNo),
            ApproverName = Approver.NameEn,
            Details = new()
        {
            { ReportingConstants.KEY_OLD_CARD_STATUS,requestDetail.ReqStatus.ToString()},
            { ReportingConstants.KEY_NEW_CARD_STATUS, CreditCardStatus.Active.ToString()}
        }
        };
        await _requestActivityAppService.LogRequestActivity(requestActivity);


        await PerformDelivery(activatedCards, response.ValidationErrors);

        string message = response.ValidationErrors.Count != 0 ? "Delivery not yet initiated" : "The card has been successfully activated!";

        if (response.ValidationErrors.Count != 0)
            _auditLogger.Log.Error(GlobalResources.LogTemplate, requestDetail.RequestId, requestDetail.CivilId, "Card Activation", $"{message}-{requestActivity.LogString()}");
        else
            _auditLogger.Log.Information(GlobalResources.LogTemplate, requestDetail.RequestId, requestDetail.CivilId, "Card Activation", $"{message}-{requestActivity.LogString()}");

        return response.Success(activatedCards, message);
    }

    [HttpPost]
    public async Task<ApiResponseModel<List<CardActivationStatus>>> ActivateMultipleCards([FromBody] BulkCardActivationRequest request)
    {
        if (!_authManager.HasPermission(Permissions.CardActivation.Request()))
            return Failure<List<CardActivationStatus>>(GlobalResources.NotAuthorized);

        var response = new ApiResponseModel<List<CardActivationStatus>>();

        if (!request.CardNumbers.Any())
            return response.Fail("Invalid request");

        string[] carNumbers = request.CardNumbers.Select(cn => cn.DeSaltThis()).ToArray();

        var activatedCards = await PerformCardActivation(carNumbers, response.ValidationErrors);

        await PerformDelivery(activatedCards, response.ValidationErrors);

        if (response.ValidationErrors.Count != 0)
            _auditLogger.Log.Error(GlobalResources.LogCardTemplate, "", string.Join(",", carNumbers), "Card Activation", string.Join(",", response.ValidationErrors.Select(er => er.Error)));
        else
            _auditLogger.Log.Information(GlobalResources.LogTemplate, "", string.Join(",", carNumbers), "Card Activation", "Success");

        if (response.ValidationErrors.Any())
            return response.Fail("Please try again. Card activation was unsuccessful, please check the validation error list for detail", response.ValidationErrors);

        return response.Success(activatedCards);
    }
    #endregion

    #region Private methods
    private async Task PerformDelivery(List<CardActivationStatus> activatedCards, List<ValidationError> validationErrors)
    {
        List<string> failedDeliveryCards = new();

        foreach (var card in activatedCards)
        {
            if ((await _deliveryService.DeliverCard(card.CardNumber))?.IsSuccess == false)
                failedDeliveryCards.Add(card.CardNumber);
        }

        if (failedDeliveryCards.Any())
            validationErrors.Add(new ValidationError()
            {
                Error = $"Following Cards are failed to deliver {string.Join(", ", failedDeliveryCards)}"
            });
    }

    async Task IsValidForReActivation(CardDetailsResponse? cardInfo, bool isMasterCard = false)
    {
        bool isSupplementaryCard = cardInfo.Parameters.CardType == $"{ProductTypes.Supplementary}{ProductTypes.ChargeCard}";

        if (isSupplementaryCard)
        {
            if (!decimal.TryParse(cardInfo.Parameters.PrimaryCardRequestId, out decimal _primaryRequestId))
                throw new ApiException(message: "Invalid primary card request id");

            var primaryCard = (await _cardDetailsAppService.GetCardInfo(_primaryRequestId) ?? throw new ApiException(message: "Unable to primary card info"))?.Data;
            bool isPrimaryCardClosed = primaryCard!.ExternalStatus == ConfigurationBase.Status_CancelOrClose;
            if (isPrimaryCardClosed) //Close or Cancel
                throw new ApiException(message: $"Primary card (" + cardInfo.Parameters.PrimaryCardNumber + ") is closed, please reactivate primary first");
        }

        if (isMasterCard)
        {
            bool isSalted = cardInfo.Parameters!.SecondaryCardNumber?.Length > 16;
            cardInfo!.CardNumber = isSalted ? cardInfo.Parameters!.SecondaryCardNumber.DeSaltThis() : cardInfo.Parameters!.SecondaryCardNumber;
        }
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

    private async Task<List<CardActivationStatus>> PerformCardActivation(string[] cardNumbers, List<ValidationError> validationErrors)
    {
        List<Task<CardActivationStatus>> tasks = new();

        await Task.Run(() =>
        {
            foreach (var cardNumber in cardNumbers)
                tasks.Add(CardActivationIntegrationServiceCall(cardNumber));
        });


        var result = await Task.WhenAll(tasks);

        var activatedCards = result.Where(x => x.IsActivated == true).ToList();
        var failedCards = result.Where(x => x.IsActivated == false).Select(x => $"{x.CardNumber} - {x.Message}");

        if (failedCards.Any())
            validationErrors.Add(new ValidationError()
            {
                Error = $"Following Cards are failed to activate {string.Join(", ", failedCards)}"
            });

        return activatedCards;
    }

    private async Task<CardActivationStatus> CardActivationIntegrationServiceCall(string cardNumber)
    {
        CardActivationStatus response = new() { CardNumber = cardNumber, IsActivated = false };
        try
        {
            var result = (await _creditCardUpdateServiceClient.performCreditCardActivationAsync(new performCreditCardActivationRequest(cardNumber)))?.performCreditCardActivationResult;
            response.IsActivated = result;
            _auditLogger.Log.Information($"The card has been successfully activated for {cardNumber}");
        }
        catch (System.Exception ex)
        {
            response.Message = ex.Message;
        }

        return response;
    }
    #endregion
}
