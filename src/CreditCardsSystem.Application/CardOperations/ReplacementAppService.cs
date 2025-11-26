using CreditCardsSystem.Data;
using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Models.Access;
using CreditCardsSystem.Domain.Models.CardIssuance;
using CreditCardsSystem.Domain.Models.CardOperation;
using CreditCardsSystem.Domain.Models.RequestDelivery;
using CreditCardsSystem.Domain.Models.Workflow;
using CreditCardsSystem.Domain.Shared.Interfaces;
using CreditCardsSystem.Domain.Shared.Models.Reports;
using CreditCardsSystem.Domain.Shared.Models.RequestActivity;
using CreditCardsSystem.Utility.Extensions;
using CreditCardUpdateServiceReference;
using Kfh.Aurora.Auth;
using Kfh.Aurora.Common.Shared.Interfaces.Customer;
using Kfh.Aurora.Integration;
using Kfh.Aurora.Logging;
using Kfh.Aurora.Organization;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using DeliveryStatus = CreditCardsSystem.Domain.Enums.DeliveryStatus;
using RequestDelivery = CreditCardsSystem.Data.Models.RequestDelivery;

namespace CreditCardsSystem.Application.CardOperations;

public partial class ReplacementAppService : BaseApiResponse, IReplacementAppService, IAppService
{
    #region Private Fields
    private readonly CreditCardUpdateServicesServiceClient _creditCardUpdateServiceClient;
    private readonly IAuthManager _authManager;
    private readonly FdrDBContext _fdrDBContext;
    private readonly ICardDetailsAppService _cardDetailsAppService;
    private readonly IEmployeeAppService _employeeAppService;
    private readonly IRequestActivityAppService _requestActivityAppService;
    private readonly IPreRegisteredPayeeAppService _preRegisteredPayeeAppService;
    private readonly ICustomerProfileAppService _customerProfileAppService;
    private readonly IRequestAppService _requestAppService;
    private readonly IAuditLogger<ReplacementAppService> _auditLogger;
    private readonly IOrganizationClient _organizationClient;
    private readonly IMemberShipAppService _memberShipService;
    private readonly ICustomerProfileCommonApi customerProfileCommonApi;
    private readonly ILookupAppService _lookupService;


    #endregion
    public ReplacementAppService(IAuthManager authManager, IIntegrationUtility integrationUtility, IOptions<IntegrationOptions> options,
        FdrDBContext fdrDBContext, ICardDetailsAppService cardDetailsAppService, IRequestActivityAppService requestActivityAppService,
        ICustomerProfileAppService customerProfileAppService, IEmployeeAppService employeeAppService, IPreRegisteredPayeeAppService preRegisteredPayeeAppService,
        IAuditLogger<ReplacementAppService> auditLogger, IRequestAppService requestAppService, IOrganizationClient organizationClient, ILookupAppService lookupService,
        IMemberShipAppService memberShipService, ICustomerProfileCommonApi customerProfileCommonApi)
    {
        _creditCardUpdateServiceClient = integrationUtility.GetClient<CreditCardUpdateServicesServiceClient>(options.Value.Client, options.Value.Endpoints.CreditCardUpdate, options.Value.BypassSslValidation);
        this._authManager = authManager;
        _fdrDBContext = fdrDBContext;
        _cardDetailsAppService = cardDetailsAppService;
        _employeeAppService = employeeAppService;
        _requestActivityAppService = requestActivityAppService;
        _customerProfileAppService = customerProfileAppService;
        _preRegisteredPayeeAppService = preRegisteredPayeeAppService;
        _auditLogger = auditLogger;
        _requestAppService = requestAppService;
        _organizationClient = organizationClient;
        _lookupService = lookupService;
        _memberShipService = memberShipService;
        this.customerProfileCommonApi = customerProfileCommonApi;
    }

    #region Card Replacement
    [HttpPost]
    public async Task<ApiResponseModel<CardReplacementResponse>> RequestCardReplacement([FromBody] CardReplacementRequest request)
    {

        if (request.ReplaceOn == ReplaceOn.Damage)
        {
            await ValidateBiometricStatus(request.RequestId);
        }

        bool isAuthorized = _authManager.HasPermission(request.ReplaceOn == ReplaceOn.Damage ? Permissions.ReplaceOnDamage.Request() : Permissions.ReplaceOnLostStolen.Request());

        if (!isAuthorized)
            return Failure<CardReplacementResponse>(GlobalResources.NotAuthorized);


        await request.ModelValidationAsync();

        var cardInfo = (await _cardDetailsAppService.GetCardInfo(request.RequestId, includeCardBalance: true) ?? throw new ApiException(message: "Invalid request Id"))?.Data;
        //var currentUser = await _employeeAppService.GetCurrentLoggedInUser();
        var cfuActivity = request.ReplaceOn == ReplaceOn.Damage ? CFUActivity.Replace_On_Damage : CFUActivity.Replace_On_Lost_Or_Stolen;
        var workflowKey = request.ReplaceOn == ReplaceOn.Damage ? WorkFlowKey.ReplaceDamagedCardWorkflow : WorkFlowKey.ReplaceLostCardWorkflow;

        await ValidateCardReplacementRequest();


        var customerProfile = (await _customerProfileAppService.GetCustomerProfile(cardInfo.CivilId) ?? throw new ApiException(message: "Unable to fetch card info")).Data;




        var requestActivity = new RequestActivityDto()
        {
            IssuanceTypeId = (int)cardInfo.IssuanceType,
            CardType = cardInfo.CardType,
            CardNumber = cardInfo.CardNumber,
            CivilId = cardInfo.CivilId,
            RequestId = cardInfo.RequestId,
            BranchId = request.BranchId,

            CustomerName = $"{customerProfile!.FirstName} {customerProfile.LastName}",
            CfuActivityId = (int)cfuActivity,
            WorkflowVariables = new() {
            {"Description",$"Request damaged card replacement for " },
            {WorkflowVariables.CardNumber,cardInfo.CardNumberDto??""},
            {WorkflowVariables.DeliveryOption,(request.DeliveryOption).ToString()},
            {WorkflowVariables.FeesWaiver, request.IsNoFees.ToString() },
            {WorkflowVariables.ReplacementReason, request.IsNoFees ? request.Reason??"N/A" : string.Empty },
            {WorkflowVariables.PinMailer,   request.IssueWithPinMailer?"Yes":"No"}
        },
            Details = new()
        {
            { ReportingConstants.KEY_CREDIT_CARD_NO,cardInfo.CardNumber!},
            { ReportingConstants.KEY_DELIVERY_OPTION,(request.DeliveryOption).ToString()},
            {ReportingConstants.KEY_Is_Waive_Fees, request.IsNoFees.ToString() },
            {ReportingConstants.KEY_Replacement_Reason, request.IsNoFees ? request.Reason??"N/A" : string.Empty },
            {ReportingConstants.KEY_ISSUE_PIN_MAILER,   request.IssueWithPinMailer?"1":"0"}
        }
        };


        if (request.KfhId is not null)
            requestActivity.TellerId = (decimal)request.KfhId;

        await AddDeliveryBranchDetail();
        await AddCardHolderNameIfChanged();
        await AddMemberShipIdIfChanged();

        await _requestActivityAppService.LogRequestActivity(requestActivity, isNeedWorkflow: true);

        return Success<CardReplacementResponse>(new(), message: request.ReplaceOn == ReplaceOn.Damage ? GlobalResources.ReplaceCardDamageRequest : GlobalResources.ReplaceCardLostOrStolenRequest);

        #region local functions
        async Task ValidateCardReplacementRequest()
        {

            //TODO: check the ncard bin and return false
            if (request.ReplaceOn == ReplaceOn.Damage && !string.IsNullOrEmpty(cardInfo.AUBCardNumber))
                throw new ApiException(message: GlobalResources.NotAuthorized);

            var isHavingPendingActivity = (await _requestActivityAppService.SearchActivity(new()
            {
                RequestId = request.RequestId,
                CivilId = cardInfo.CivilId,
                CardNumber = cardInfo.CardNumber,
                CfuActivityId = (int)cfuActivity,
                RequestActivityStatusId = (int)RequestActivityStatus.Pending,
                IssuanceTypeId = (int)cardInfo.IssuanceType
            }))?.Data?.Any() ?? false;

            if (isHavingPendingActivity)
                throw new ApiException(message: GlobalResources.RequestAlreadySent);
        }

        async Task AddDeliveryBranchDetail()
        {
            if (request.DeliveryOption != DeliveryOption.BRANCH) return;

            string deliveryBranchName = Regex.Replace((await _organizationClient.GetBranches())?.FirstOrDefault(x => x.BranchId == request.DeliveryBranchId)?.Name!, "@\"[^A-Za-z]\"", " ").Trim();
            requestActivity.Details.Add(ReportingConstants.KEY_BRANCH_ID, request.DeliveryBranchId.ToString());
            requestActivity.Details.Add(ReportingConstants.KEY_BRANCH, deliveryBranchName);

            requestActivity.WorkflowVariables.Add(WorkflowVariables.DeliveryBranchId, request.DeliveryBranchId.ToString());
            requestActivity.WorkflowVariables.Add(WorkflowVariables.DeliveryBranchName, deliveryBranchName);
        }

        async Task AddCardHolderNameIfChanged()
        {
            if (string.IsNullOrEmpty(cardInfo.HolderEmbossName) || cardInfo.HolderEmbossName == request.HolderName) return;

            requestActivity.Details.Add(ReportingConstants.KEY_CARD_HOLDER_NAME, request.HolderName.ToUpper());
            requestActivity.Details.Add(ReportingConstants.KEY_OLD_EMBOSSING_NAME, cardInfo.HolderEmbossName.ToUpper());

            requestActivity.WorkflowVariables.Add(WorkflowVariables.NewCardHolderName, request.HolderName.ToUpper());
            requestActivity.WorkflowVariables.Add(WorkflowVariables.OldCardHolderName, cardInfo.HolderEmbossName.ToUpper());
            await Task.CompletedTask;
        }

        async Task AddMemberShipIdIfChanged()
        {
            if (request.NewMemberShipId is null) return;

            if (cardInfo!.Parameters?.ClubMembershipId == request.NewMemberShipId.ToString()) return;

            int companyId = cardInfo.Parameters!.CompanyName == "KAC" ? 1 : 2;
            requestActivity!.Details.Add(ReportingConstants.KEY_COMPANY_ID, companyId.ToString());
            requestActivity.Details.Add(ReportingConstants.KEY_CLUB_NAME, cardInfo!.Parameters!.ClubName!);
            requestActivity.Details.Add(ReportingConstants.KEY_OLD_MEMBERSHIPID, cardInfo!.Parameters!.ClubMembershipId!);
            requestActivity.Details.Add(ReportingConstants.KEY_MEMBERSHIPID, request!.NewMemberShipId!.ToString()!);


            requestActivity!.WorkflowVariables.Add(WorkflowVariables.CompanyId, companyId.ToString());
            requestActivity.WorkflowVariables.Add(WorkflowVariables.ClubName, cardInfo!.Parameters!.ClubName!);
            requestActivity.WorkflowVariables.Add(WorkflowVariables.OldMemberShipId, cardInfo!.Parameters!.ClubMembershipId!);
            requestActivity.WorkflowVariables.Add(WorkflowVariables.NewMemberShipId, request!.NewMemberShipId!.ToString()!);

            var memberShipConflicts = (await _memberShipService.GetMemberShipIdConflicts(cardInfo!.CivilId, companyId, request.NewMemberShipId.ToString() ?? ""))?.Data;
            if (memberShipConflicts?.Any() ?? false)
            {
                var oldCivilId = memberShipConflicts[0].CivilId;
                var deleteMemberShipResponse = await _memberShipService.RequestDeleteMemberShip(new()
                {
                    CivilId = oldCivilId,
                    ClubMembershipId = request.NewMemberShipId.ToString(),
                    CompanyId = companyId,
                    RequestedBy = Convert.ToInt16(requestActivity.TellerId),
                    RequestorReason = "Card Replacement",
                    ReturnExistingId = true
                });

                requestActivity.Details.Add(ReportingConstants.KEY_DUPLICATE_MEMBERSHIPID, "1");
                requestActivity.WorkflowVariables.Add(WorkflowVariables.DuplicateMemberShipId, GlobalResources.Yes);


                if (deleteMemberShipResponse.IsSuccessWithData)
                {
                    requestActivity.Details.Add(ReportingConstants.KEY_MEMBERSHIP_DELETE_REQUEST_ID, deleteMemberShipResponse.Data!.Id.ToString());
                    requestActivity.WorkflowVariables.Add(WorkflowVariables.MemberShipDeleteRequestId, deleteMemberShipResponse.Data!.Id.ToString());
                }
            }
        }
        #endregion
    }


    [HttpPost]
    public async Task<ApiResponseModel<ProcessResponse>> ProcessCardReplacementRequest([FromBody] ProcessCardClosureRequest request)
    {
        var requestActivity = (await _requestActivityAppService.GetRequestActivityById(request.RequestActivityId)).Data ?? throw new ApiException(message: "Unable to find Request");
        CFUActivity _activity = requestActivity.CfuActivity;


        if (_activity is not (CFUActivity.Replace_On_Damage or CFUActivity.Replace_On_Lost_Or_Stolen))
            return Failure<ProcessResponse>(GlobalResources.InvalidRequest);

        bool isAuthorized = _authManager.HasPermission(_activity == CFUActivity.Replace_On_Damage ? Permissions.ReplaceOnDamage.EnigmaApprove() : Permissions.ReplaceOnLostStolen.EnigmaApprove());

        if (!isAuthorized)
            return Failure<ProcessResponse>(GlobalResources.NotAuthorized);


        var cardInfo = (await _cardDetailsAppService.GetCardInfo(requestActivity.RequestId))?.Data ?? throw new ApiException(message: "Invalid request Id");

        if (request.ActionType == ActionType.Rejected)
            return await Reject();

        return await Approve();

        #region local functions
        async Task<ApiResponseModel<ProcessResponse>> Approve()
        {
            _ = Enum.TryParse(requestActivity.Details[ReportingConstants.KEY_DELIVERY_OPTION], out DeliveryOption deliveryOption);
            _ = bool.TryParse(requestActivity.Details[ReportingConstants.KEY_Is_Waive_Fees], out bool IsChargeable);
            _ = bool.TryParse(requestActivity.Details[ReportingConstants.KEY_ISSUE_PIN_MAILER], out bool _pinMailerRequire);
            if (!decimal.TryParse(requestActivity.Details[ReportingConstants.KEY_CREDIT_CARD_NO], out decimal oldCardNumber))
                _ = decimal.TryParse(cardInfo.CardNumber, out oldCardNumber);

            var currentUser = _authManager.GetUser();

            string auditMessage = $"oldCardNumber:{oldCardNumber}";

            // this card number remains old untill we call replace lost/stolen
            string oldCardNumberString = oldCardNumber.ToString();

            decimal? oldToNewReqId = (await _fdrDBContext.Requests.FirstOrDefaultAsync(x => x.CardNo == oldCardNumberString))?.RequestId;

            var deliveryBranchId = requestActivity.BranchId;
            if (deliveryOption == DeliveryOption.BRANCH && int.TryParse(requestActivity.Details[ReportingConstants.KEY_BRANCH_ID], out int _requestedDeliveryBranchId))
                deliveryBranchId = _requestedDeliveryBranchId;

            await ApprovalValidation();

            if (requestActivity.CfuActivity == CFUActivity.Replace_On_Lost_Or_Stolen)
                await ApproveLostOrStolenCardReplacement();

            if (requestActivity.CfuActivity == CFUActivity.Replace_On_Damage)
                await ApproveDamagedCardReplacement();

            await UpdateEmbossingNameIfChanged();
            await UpdateMemberShipIdIfChanged();

            await _requestActivityAppService.UpdateRequestActivityStatus(new() { IssuanceTypeId = (int)cardInfo.IssuanceType, CivilId = cardInfo!.CivilId, RequestActivityId = (int)request.RequestActivityId, RequestActivityStatusId = (int)RequestActivityStatus.Approved });

            await CreateDeliveryRequest();


            return Success(new ProcessResponse());


            #region local functions

            async Task ApproveDamagedCardReplacement()
            {
                var replaceLostCardResponse = (await _creditCardUpdateServiceClient.replaceDamageCardAsync(new()
                {
                    cardNo = cardInfo.CardNumber,
                    charge = !IsChargeable,
                    deliveryMethod = $"{deliveryBranchId?.ToString("D3")}/{(deliveryOption == DeliveryOption.BRANCH ? "B" : "C")}",
                }))?.replaceDamageCardResult;

                //Failed response
                if (replaceLostCardResponse!.respCode != "0000")
                {
                    _auditLogger.Log.Error("ReplaceLost/Stolen card Failed :: {code} :: {message}", replaceLostCardResponse.respCode, replaceLostCardResponse.respMessage);

                    //PrimaryCard has been already replaced
                    if (replaceLostCardResponse.respCode == "1050")
                        await _requestActivityAppService.UpdateRequestActivityStatus(new() { IssuanceTypeId = (int)cardInfo.IssuanceType, CivilId = cardInfo.CivilId, RequestActivityId = (int)request.RequestActivityId, RequestActivityStatusId = (int)RequestActivityStatus.Approved });

                    //return
                    throw new ApiException(message: GlobalResources.CardReplacementFailed);
                }
            }

            async Task ApproveLostOrStolenCardReplacement()
            {
                // calling replace lost stolen will copy same record in memory then it will call VMX to replace
                // then it returns with the new Card No. which will update the local card No. record and then the
                // record in the memory will be inserted with status = 6
                var replaceLostCardResponse = (await _creditCardUpdateServiceClient.replaceLostStolenCardAsync(new()
                {
                    branchNo = requestActivity.BranchId.ToString(),
                    charge = !IsChargeable,
                    deliveryMethod = $"{deliveryBranchId?.ToString("D3")}/{(deliveryOption == DeliveryOption.BRANCH ? "B" : "C")}",
                    pinMailerRequire = _pinMailerRequire,
                    primaryCardNo = cardInfo.CardNumber,
                    secondaryCardNo = cardInfo.ProductType == ProductTypes.Tayseer ? cardInfo.SecondaryCardNo : ""
                }))?.replaceLostStolenCardResult;

                //Failed response
                if (replaceLostCardResponse!.respCode != "0000")
                {
                    _auditLogger.Log.Error("ReplaceLost/Stolen card Failed :: {code} :: {message}", replaceLostCardResponse.respCode, replaceLostCardResponse.respMessage);

                    //PrimaryCard has been already replaced
                    if (replaceLostCardResponse.respCode == "1050")
                        await _requestActivityAppService.UpdateRequestActivityStatus(new() { IssuanceTypeId = (int)cardInfo.IssuanceType, CivilId = cardInfo.CivilId, RequestActivityId = (int)request.RequestActivityId, RequestActivityStatusId = (int)RequestActivityStatus.Approved });

                    //return
                    throw new ApiException(message: GlobalResources.CardReplacementFailed);
                }
                auditMessage = $" SecondaryCardNumber:{(cardInfo.ProductType == ProductTypes.Tayseer ? cardInfo.SecondaryCardNo : "")}";

                await AddPreregisteredPayeeForLostOsraSupplementary();
            }

            /// Filter non-closed supplementary cards
            async Task AddPreregisteredPayeeForLostOsraSupplementary()
            {
                // Haitham: 22-05-2014 resolve the issue of Osra supplementary is not linked when the card is replaced for lost stolen
                var payee = await _preRegisteredPayeeAppService.GetPreregisteredPayeeByCardNumber(requestActivity.Details[ReportingConstants.KEY_CREDIT_CARD_NO]);

                // we will get preregistered payees in order to get the primary civil id, in this case at least we have
                // one record in preregistered payees table because we reported already existing card as lost
                if (payee.AnyWithNull())
                {
                    var randomPayee = payee.FirstOrDefault();
                    string primaryCivilId = randomPayee.CivilId;
                    string newCardNumber = (await _fdrDBContext.Requests.FirstOrDefaultAsync(x => x.RequestId == oldToNewReqId))?.CardNo ?? "";

                    string fullName = payee.FirstOrDefault(x => x.CardNo == oldCardNumberString)?.FullName ?? "";

                    // adding new card linkage in pre registered payees table.
                    try
                    {
                        auditMessage += $" primaryCivilId:{primaryCivilId} - newCardNumber:{newCardNumber} - fullName:{fullName}";
                        await _preRegisteredPayeeAppService.AddPreregisteredPayee(new()
                        {
                            CivilId = primaryCivilId,
                            CardNo = newCardNumber,
                            Description = "replaced lost/stolen",
                            FullName = fullName,
                            TypeId = cardInfo.ProductType == ProductTypes.ChargeCard ? 4 : 3,
                            StatusId = 3
                        });

                        _auditLogger.Log.Information(("Success") + " {event} - {auditMessage}", ReportingConstants.EVENT_ADD_PRE_REG_PAYEE_FOR_LOST_OSRA_SUPP, auditMessage);

                    }
                    catch (System.Exception)
                    {
                        _auditLogger.Log.Information(("Failed") + " {event} - {auditMessage}", ReportingConstants.EVENT_ADD_PRE_REG_PAYEE_FOR_LOST_OSRA_SUPP, auditMessage);
                    }
                }

                _auditLogger.Log.Information("Success {event} - {auditMessage}", ReportingConstants.EVENT_ADD_PRE_REG_PAYEE_FOR_LOST_OSRA_SUPP, auditMessage);
            }

            async Task UpdateEmbossingNameIfChanged()
            {
                if (!requestActivity.Details.ContainsKey(ReportingConstants.KEY_OLD_EMBOSSING_NAME))
                    return;

                if (requestActivity.Details[ReportingConstants.KEY_OLD_EMBOSSING_NAME] == requestActivity.Details[ReportingConstants.KEY_CARD_HOLDER_NAME])
                    return;

                string embossName1 = "";
                string embossName2 = "";


                embossName1 = requestActivity.Details[ReportingConstants.KEY_CARD_HOLDER_NAME];

                if (requestActivity.Details.ContainsKey(ReportingConstants.KEY_MEMBERSHIPID) && requestActivity.Details[ReportingConstants.KEY_MEMBERSHIPID] != requestActivity.Details[ReportingConstants.KEY_OLD_MEMBERSHIPID])
                {
                    //Co-Brand
                    embossName2 = $"{requestActivity.Details[ReportingConstants.KEY_CLUB_NAME]} ID {requestActivity.Details[ReportingConstants.KEY_MEMBERSHIPID]}";
                }

                await _creditCardUpdateServiceClient.updateCardEmbossedNameAsync(new()
                {
                    embosserUpdRequest = new()
                    {
                        requestID = (long)requestActivity.RequestId,
                        requestIDSpecified = true,
                        embossedName1 = embossName1,
                        embossedName2 = embossName2
                    }
                });

            }

            async Task UpdateMemberShipIdIfChanged()
            {
                if (!requestActivity.Details.ContainsKey(ReportingConstants.KEY_MEMBERSHIPID))
                    return;

                if (requestActivity.Details[ReportingConstants.KEY_MEMBERSHIPID] == requestActivity.Details[ReportingConstants.KEY_OLD_MEMBERSHIPID])
                    return;

                await _memberShipService.DeleteAndCreateMemberShipIfAnyById(cardInfo.CivilId, new CoBrand()
                {
                    MemberShipId = Convert.ToInt32(requestActivity.Details[ReportingConstants.KEY_OLD_MEMBERSHIPID]),
                    NewMemberShipId = Convert.ToInt32(requestActivity.Details[ReportingConstants.KEY_MEMBERSHIPID]),
                    Company = new(Convert.ToInt32(requestActivity.Details[ReportingConstants.KEY_COMPANY_ID]), "", 0, "")
                });

                //refactor to remove hardcoded
                await _requestAppService.UpdateRequestParameter(cardInfo.RequestId, "CLUB_MEMBERSHIP_ID", requestActivity.Details[ReportingConstants.KEY_MEMBERSHIPID]);
            }

            async Task ApprovalValidation()
            {
                //maker cannot approve his own request
                if (currentUser.KfhId == requestActivity.TellerId.ToString("0"))
                    throw new ApiException(message: GlobalResources.MakerCheckerAreSame);

                //Tayseer card validation with secondary number
                if (cardInfo.ProductType == ProductTypes.Tayseer && string.IsNullOrEmpty(cardInfo.SecondaryCardNo))
                    throw new ApiException(message: GlobalResources.NoMasterCardForTayseer);

                if (deliveryOption == DeliveryOption.BRANCH && deliveryBranchId is null)
                    throw new ApiException(message: GlobalResources.InvalidBrandId);

                await Task.CompletedTask;
            }

            /// <summary>
            /// This method will update new request id with the old delivery request id, then will create a new request delivery for new card
            /// </summary>
            async Task CreateDeliveryRequest()
            {
                if (requestActivity.CfuActivity == CFUActivity.Replace_On_Lost_Or_Stolen)
                {
                    var oldRequestDelivery = await _fdrDBContext.RequestDeliveries.SingleOrDefaultAsync(x => x.RequestId == oldToNewReqId);
                    if (oldRequestDelivery is not null)
                    {
                        oldRequestDelivery.RequestId = requestActivity.RequestId;
                        await _fdrDBContext.SaveChangesAsync();
                    }
                }

                await RequestDelivery(deliveryOption, oldToNewReqId, deliveryBranchId);
            }
            #endregion
        }
        async Task<ApiResponseModel<ProcessResponse>> Reject()
        {
            await _requestActivityAppService.UpdateRequestActivityStatus(new()
            {
                ReasonForRejection = request.ReasonForRejection,
                IssuanceTypeId = (int)cardInfo.IssuanceType,
                CivilId = cardInfo.CivilId,
                RequestActivityId = requestActivity.RequestActivityId,
                RequestActivityStatusId = (int)RequestActivityStatus.Rejected
            });
            return Success(new ProcessResponse() { CardNumber = "" }, message: "Successfully Rejected");
        }
        #endregion
    }

    private async Task RequestDelivery(DeliveryOption deliveryOption, decimal? oldToNewReqId, int? deliveryBranchId)
    {
        var newRequestDelivery = new RequestDeliveryDto()
        {
            CreateDate = DateTime.Now,
            RequestId = oldToNewReqId,
            DeliveryType = deliveryOption.ToString()
        };

        if (deliveryOption == DeliveryOption.BRANCH)
        {
            var deliveryBranch = (await _lookupService.GetAllBranches())?.Data.FirstOrDefault(x => x.BranchId == deliveryBranchId);
            newRequestDelivery.DeliveryBranchId = deliveryBranchId;
            newRequestDelivery.DeliveryBranchName = deliveryBranch.Name;
        }

        newRequestDelivery.RequestDeliveryStatusId = deliveryOption == DeliveryOption.BRANCH ? (int)DeliveryStatus.BRANCH_UNDER_DELIVERY_PROCESSING : (int)DeliveryStatus.COURIER_UNDER_DELIVERY_PROCESSING;

        await _fdrDBContext.RequestDeliveries.AddAsync(newRequestDelivery.Adapt<RequestDelivery>());
        await _fdrDBContext.SaveChangesAsync();
    }

    private async Task ValidateBiometricStatus(decimal requestId)
    {
        var request = _fdrDBContext.Requests.AsNoTracking().FirstOrDefault(x => x.RequestId == requestId) ?? throw new ApiException(message: "Invalid request Id ");
        var bioStatus = await customerProfileCommonApi.GetBiometricStatus(request!.CivilId);
        if (bioStatus.ShouldStop)
            throw new ApiException(message: GlobalResources.BioMetricRestriction);
    }

    #endregion



}
