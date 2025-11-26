using CreditCardsSystem.Data;
using CreditCardsSystem.Data.Models;
using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Models.CardIssuance;
using CreditCardsSystem.Domain.Models.CardOperation;
using CreditCardsSystem.Domain.Models.Request;
using CreditCardsSystem.Domain.Models.RequestActivity;
using CreditCardsSystem.Domain.Models.Workflow;
using CreditCardsSystem.Domain.Shared.Interfaces.Workflow;
using CreditCardsSystem.Domain.Shared.Models.Account;
using CreditCardsSystem.Domain.Shared.Models.RequestActivity;
using CreditCardsSystem.Utility.Crypto;
using CreditCardsSystem.Utility.Extensions;
using Dapper;
using Kfh.Aurora.Auth;
using Kfh.Aurora.Logging;
using Kfh.Aurora.Organization;
using Kfh.Aurora.Workflow.Dto;
using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using Telerik.DataSource.Extensions;

namespace CreditCardsSystem.Application.RequestActivityService;
public class RequestActivityAppService : BaseApiResponse, IRequestActivityAppService, IAppService
{
    private readonly FdrDBContext _fdrDBContext;
    private readonly ICardDetailsAppService _cardDetailsAppService;
    private readonly IEmployeeAppService _employeeService;
    private readonly IAuditLogger<RequestActivityAppService> _auditLogger;
    private readonly IUserPreferencesClient _userPreferencesClient;
    private readonly IOrganizationClient _organizationClient;
    private readonly IAuthManager _authManager;
    private readonly IRequestAppService _requestAppService;
    private readonly IWorkflowAppService _workflowAppService;
    private readonly IConfigurationAppService _configurationAppService;

    public RequestActivityAppService(FdrDBContext fdrDBContext, ICardDetailsAppService cardDetailsAppService, IEmployeeAppService employeeService, IAuditLogger<RequestActivityAppService> auditLogger, IUserPreferencesClient userPreferencesClient, IOrganizationClient organizationClient, IAuthManager authManager, IRequestAppService requestAppService, IWorkflowAppService workflowAppService, IConfigurationAppService configurationAppService)
    {
        _fdrDBContext = fdrDBContext;
        _cardDetailsAppService = cardDetailsAppService;
        _employeeService = employeeService;
        _auditLogger = auditLogger;
        _userPreferencesClient = userPreferencesClient;
        _organizationClient = organizationClient;
        _authManager = authManager;
        _requestAppService = requestAppService;
        _workflowAppService = workflowAppService;
        _configurationAppService = configurationAppService;
    }

    public UserDto? CurrentUser { get; set; } = null!;
    //public List<UserPreferences>? UserPreferences { get; set; } = null!;

    private async Task BindUserDetails(decimal? kfhId)
    {
        if (kfhId is null or 0)
            kfhId = Convert.ToDecimal(_authManager.GetUser()?.KfhId);

        CurrentUser = await _employeeService.GetCurrentLoggedInUser(kfhId);

        var userbranch = await _configurationAppService.GetUserBranch(kfhId);
        //UserPreferences = await _userPreferencesClient.GetUserPreferences(kfhId.ToString()!);

        //_ = int.TryParse(UserPreferences.FromUserPreferences().DefaultBranchIdValue, out int _defaultBranchId);
        //var defaultBranches = UserPreferences.FromUserPreferences().UserBranches;

        //if (defaultBranches.Count == 0)
        //    defaultBranches = await _organizationClient.GetUserBranches(kfhId.ToString()!);

        //var defaultBranch = defaultBranches?.FirstOrDefault(x => x.BranchId == _defaultBranchId);

        CurrentUser.DefaultBranchId = userbranch.BranchId;
        CurrentUser.DefaultBranchName = userbranch.Name;
    }


    [HttpGet]
    public async Task<ApiResponseModel<List<RequestActivityResult>>> GetNonEnigmaRequestActivities()
    {
        var requestActivityQuery = from RA in _fdrDBContext.RequestActivities.AsNoTracking().Include(x => x.Details)
                                   where RA.Details.Any(x => x.Paramter == "WorkFlowInstanceId") == false
                                   select new RequestActivityResult()
                                   {
                                       RequestActivityID = RA.RequestActivityId,
                                       RequestActivityStatusID = RA.RequestActivityStatusId,
                                       CreationDate = RA.CreationDate,
                                   };

        requestActivityQuery = requestActivityQuery.Where(x => x.CFUActivityID != null);
        requestActivityQuery = requestActivityQuery.Where(x => x.CreationDate >= DateTime.Now.AddDays(-10));
        return new ApiResponseModel<List<RequestActivityResult>>().Success(await requestActivityQuery.OrderByDescending(x => x.CreationDate).ToListAsync());
    }


    //[NonAction]
    //public async Task CompleteActivity(CompleteActivityRequest request)
    //{
    //    if (request.Status is RequestActivityStatus.Rejected && string.IsNullOrEmpty(request.ReasonForRejection))
    //        throw new ApiException(message: "please enter reason for rejection");

    //    await UpdateRequestActivityStatus(new()
    //    {
    //        ReasonForRejection = request.ReasonForRejection,
    //        IssuanceTypeId = 0, //No need to pass
    //        CivilId = string.Empty,//No need to pass
    //        RequestActivityId = request.RequestActivityId,
    //        RequestActivityStatusId = (int)request.Status
    //    });

    //    if (request.TaskId is null)
    //        return;

    //    var taskStatus = (request.Status == RequestActivityStatus.Approved ? ActionType.Approved : ActionType.Rejected).ToString();
    //    var description = request.Status == RequestActivityStatus.Approved ? "Approved" : "Rejected, please check the comment";
    //    //var task = await _workflowAppService.GetTaskById(request.TaskId!.Value);

    //    CompleteTaskRequest taskRequest = new()
    //    {
    //        Assignee = _authManager.GetUser()?.KfhId ?? "",
    //        TaskId = request.TaskId!.Value,
    //        InstanceId = request.InstanceId!.Value,
    //        Status = taskStatus,
    //        Payload = new()
    //        {
    //            { "Outcomes",new List<string>{ taskStatus } },
    //            { "Description", description }
    //        },
    //        Comments = string.IsNullOrEmpty(request.ReasonForRejection) ? null : new() { request.ReasonForRejection }
    //    };

    //    await _workflowAppService.CompleteTask(taskRequest);
    //    return;
    //}

    [NonAction]
    public async Task CompleteActivity(ActivityProcessRequest request, bool isFromSSO = false)
    {
        if (request.ActionType is ActionType.Rejected && string.IsNullOrEmpty(request.ReasonForRejection))
            throw new ApiException(message: "please enter reason for rejection");


        if (request.RequestActivityId > 0 && isFromSSO == false)
        {
            await UpdateRequestActivityStatus(new()
            {
                ReasonForRejection = request.ReasonForRejection,
                IssuanceTypeId = 0, //No need to pass
                CivilId = string.Empty,//No need to pass
                RequestActivityId = request.RequestActivityId,
                RequestActivityStatusId = (int)request.ActionType,
            }, request.KfhId);
        }

        if (request.TaskId is null)
            return;

        var taskStatus = request.ActionType.ToString();
        var description = request.ActionType == ActionType.Rejected ? "Rejected, please check the comment" : request.ActionType.GetDescription();

        CompleteTaskRequest taskRequest = new()
        {
            Assignee = _authManager.GetUser()?.KfhId ?? "",
            TaskId = request.TaskId!.Value,
            InstanceId = request.WorkFlowInstanceId!.Value,
            Status = taskStatus,
            Payload = new()
            {
                { "Outcomes",new List<string>{ taskStatus } },
                { "Description", description }
            },
            Comments = string.IsNullOrEmpty(request.ReasonForRejection) ? null : new() { request.ReasonForRejection }
        };

        await _workflowAppService.CompleteTask(taskRequest, request.KfhId);

        _auditLogger.Log.Information(GlobalResources.LogCardTemplate, "", request!.CardNumber, request.Activity.GetDescription(), request.ActionType.ToString());

        return;
    }

    [NonAction]

    public async Task ValidateActivityWithWorkflow(ActivityProcessRequest request)
    {
        if (request.ActionType is ActionType.Rejected && string.IsNullOrEmpty(request.ReasonForRejection))
            throw new ApiException(message: "please enter reason for rejection");

        var requestActivity = await _fdrDBContext.RequestActivities.AsNoTracking().Include(x => x.Details).AsNoTracking().FirstOrDefaultAsync(x => x.RequestActivityId == request.RequestActivityId) ?? throw new ApiException(insertSeriLog: true, message: $"Invalid request activity {request.RequestActivityId}");

        if (!requestActivity.Details.Any(x => x.Paramter == "TaskId" && x.Value == request.TaskId.ToString()))
        {
            throw new ApiException(message: $"Invalid TaskId {request.TaskId}");
        }

        if (!requestActivity.Details.Any(x => x.Paramter == "WorkFlowInstanceId" && x.Value == request.WorkFlowInstanceId.ToString()))
        {
            throw new ApiException(insertSeriLog: true, message: $"Invalid WorkFlowInstanceId {request.WorkFlowInstanceId}");
        }


        request.CardNumber = requestActivity.RequestActivityId.ToString();

        if (requestActivity.CfuActivityId is not null)
            request.Activity = (CFUActivity)requestActivity.CfuActivityId;

        return;
    }


    [NonAction]
    public async Task CompleteSupplementaryActivities(ProcessCardRequest request)
    {
        if (request.ActionType is ActionType.Rejected && string.IsNullOrEmpty(request.ReasonForRejection))
            throw new ApiException(message: "please enter reason for rejection");

        //var primaryRequestActivity = await _fdrDBContext.RequestActivities.FirstOrDefaultAsync(x => x.RequestActivityId == request.RequestActivityId);

        //if (primaryRequestActivity is null || primaryRequestActivity.RequestId is null) return;

        //decimal primaryRequestId = (decimal)primaryRequestActivity.RequestId;

        var primaryRequest = await _requestAppService.GetRequestDetail(request.RequestId);
        if (!primaryRequest.IsSuccessWithData)
            return;



        bool isApproved = request.ActionType is ActionType.Approved && primaryRequest.Data!.ReqStatus is not (int)CreditCardStatus.Approved;
        bool isRejected = request.ActionType is ActionType.Rejected;


        //No approved and not reject so no action 
        if (!isApproved && !isRejected)
            return;


        var supplementaryCards = await _cardDetailsAppService.GetSupplementaryCardsByRequestId(primaryRequest.Data.RequestId);

        if (!supplementaryCards.IsSuccessWithData)
            return;



        foreach (var supCard in supplementaryCards.Data!)
        {
            var supCardDetail = (await _requestAppService.GetRequestDetail((decimal)supCard.RequestId!))?.Data;

            if (string.IsNullOrEmpty(supCardDetail?.Parameters.WorkFlowInstanceId))
                continue;

            var requestActivity = await _fdrDBContext.RequestActivityDetails.FirstOrDefaultAsync(x => x.Paramter == nameof(RequestParameterDto.WorkFlowInstanceId) && x.Value == supCardDetail.Parameters.WorkFlowInstanceId);

            if (requestActivity is null)
                continue;

            await UpdateRequestActivityStatus(new()
            {
                ReasonForRejection = request.ReasonForRejection,
                IssuanceTypeId = 0, //No need to pass
                CivilId = string.Empty,//No need to pass
                RequestActivityId = (decimal)requestActivity.RequestActivityId!,
                RequestActivityStatusId = (int)request.ActionType
            });


            if (supCard.CardStatus is CreditCardStatus.AccountBoardingStarted or CreditCardStatus.CardUpgradeStarted)
            {
                //TODO: Create BCD Task
            }

            if (supCard.CardStatus is CreditCardStatus.Approved or CreditCardStatus.Rejected)
            {
                //TODOL Complete Task
            }

        }

    }


    [HttpGet]
    public async Task<ApiResponseModel<RequestActivityDto>> GetRequestActivityById(decimal requestActivityId, bool validateChecker = false)
    {
        var requestActivity = await _fdrDBContext.RequestActivities.AsNoTracking().Include(x => x.Details).FirstOrDefaultAsync(x => x.RequestActivityId == requestActivityId) ?? throw new ApiException(message: "Invalid request activity");


        string[] secureDetails = ["credit_card_no"];

        var requestActivityDto = requestActivity.Adapt<RequestActivityDto>();
        requestActivityDto.Details = requestActivity.Details.Where(x => x.Paramter != null && x.Value != null)
            .ToDictionary(x => x.Paramter!, x => secureDetails.Any(sd => sd == x.Paramter) ? x.Value!.SaltThis() : x.Value!);


        await BindProductDetails();

        return Success(requestActivityDto);


        async Task BindProductDetails()
        {
            var cardRequest = (await _requestAppService.GetRequestDetail((decimal)requestActivity.RequestId!))?.Data;
            if (cardRequest is null)
                return;

            //var cardRequest = await _fdrDBContext.Requests.FindAsync(requestActivity.RequestId);
            CardDefinition? cardDef = await _fdrDBContext.CardDefs.AsNoTracking().FirstOrDefaultAsync(x => x.CardType == cardRequest.CardType);
            if (cardDef is null) return;

            requestActivityDto.ProductType = Helpers.GetProductType(cardDef.Duality, Convert.ToDecimal(cardDef.MinLimit), Convert.ToDecimal(cardDef.MaxLimit));
            requestActivityDto.ProductName = cardDef.Name;
            requestActivityDto.CardNumber = cardRequest!.CardNo;
            requestActivityDto.CardNumberDto = cardRequest!.CardNo.SaltThis();
            requestActivityDto.CardType = cardRequest!.CardType;
            requestActivityDto.AccountNumber = cardRequest!.AcctNo;
            requestActivityDto.CardStatus = (CreditCardStatus)cardRequest!.ReqStatus;

            //if (validateChecker)
            //    requestActivityDto.IsAllowedToApprove = await IsValidChecker();
        }

        //async Task<bool> IsValidChecker()
        //{
        //    var cardInfo = (await _cardDetailsAppService.GetCardInfo(requestActivity.RequestId))?.Data;

        //    Collateral collateral = Collateral.PREPAID_CARDS;

        //    if (Enum.TryParse(cardInfo!.Parameters!.Collateral, out Collateral _collateral))
        //        collateral = _collateral;

        //    return requestActivityDto.CfuActivity switch
        //    {
        //        CFUActivity.LIMIT_CHANGE_INCR => await CanApproveToLimitChange(),
        //        _ => true
        //    };

        //    async Task<bool> CanApproveToLimitChange()
        //    {
        //        if (requestActivityDto.RequestActivityStatus != RequestActivityStatus.Pending)
        //            return false;

        //        if (cardInfo.CardStatus != CreditCardStatus.Active)
        //            return false;

        //        var changeHistory = _fdrDBContext.ChangeLimitHistories.Where(x => x.ReqId == requestActivity.RequestId!.ToString() && x.Status == ChangeLimitStatus.APPROVED.ToString());
        //        if (!changeHistory.Any())
        //            return false;


        //        bool isCardIsTayseerAndAgainstSalaryOrException = cardInfo.ProductType == ProductTypes.Tayseer && collateral is Collateral.AGAINST_SALARY or Collateral.AGAINST_SALARY_USD or Collateral.EXCEPTION;
        //        var lastChange = await _fdrDBContext.TayseerCreditCheckings.FirstOrDefaultAsync(x => x.RequestId == requestActivity.RequestId && x.EntryType == 2);
        //        bool mustCheck = isCardIsTayseerAndAgainstSalaryOrException && lastChange is not null;
        //        //if (_authManager.HasPermission(Permissions.BCDCreditApproval))
        //        //{
        //        //    if(mustCheck)
        //        //        lastChange.Status==
        //        //}


        //        return true;
        //    }
        //}
    }

    [NonAction]
    public async Task UpdateRequestActivityStatus(RequestActivityDto request, decimal? kfhId = null)
    {
        if (!Enum.IsDefined(typeof(RequestActivityStatus), request.RequestActivityStatusId))
            return;

        var requestActivity = await _fdrDBContext.RequestActivities.FirstOrDefaultAsync(x => x.RequestActivityId == request.RequestActivityId) ?? throw new ApiException(message: "Invalid request activity");

        await BindUserDetails(kfhId);

        requestActivity.RequestActivityStatusId = request.RequestActivityStatusId;
        requestActivity.ApproverId = Convert.ToDecimal(CurrentUser?.KfhId);
        requestActivity.ApproverName = CurrentUser!.Name;

        if (request.RequestActivityStatus == RequestActivityStatus.Rejected)
        {
            await LogRequestActivityDetail(new(){new RequestActivityDetailsDto()
            {
                RequestActivityId = requestActivity.RequestActivityId,
                Paramter = ConfigurationBase.REJECT_REASON,
                Value = request.ReasonForRejection
            }});
        }

        await _fdrDBContext.SaveChangesAsync();
    }

    [NonAction]
    public async Task<decimal> LogRequestActivity(RequestActivityDto requestActivity, bool searchExist = true, bool isNeedWorkflow = false, bool onlyWorkflow = false)
    {
        decimal requestActivityId = 0;

        using var transaction = await _fdrDBContext.Database.BeginTransactionAsync();

        if (onlyWorkflow == false)
        {
            RequestActivity newRequestActivity = new();

            try
            {
                string customerName = requestActivity.IssuanceTypeId == (int)IssuanceTypes.OTHERS
                ? (await _fdrDBContext.CorporateProfiles.AsNoTracking().FirstOrDefaultAsync(x => x.CorporateCivilId == requestActivity.CivilId))?.EmbossingName ?? ""
                : (await _fdrDBContext.Profiles.AsNoTracking().FirstOrDefaultAsync(x => x.CivilId == requestActivity.CivilId))?.FullName ?? "";

                newRequestActivity = new()
                {
                    RequestActivityStatusId = requestActivity.RequestActivityStatusId,
                    CivilId = requestActivity!.CivilId!,
                    RequestId = requestActivity.RequestId,
                    CustomerName = customerName ?? requestActivity.CustomerName,
                    CfuActivityId = requestActivity.CfuActivityId,
                    CreationDate = DateTime.Now,
                    LastUpdateDate = DateTime.Now,
                    IssuanceTypeId = requestActivity.IssuanceTypeId
                };

                await BindUserDetails(requestActivity.TellerId);

                if (requestActivity.OverrideTellerInfo)
                {
                    newRequestActivity.TellerId = Convert.ToDecimal(CurrentUser?.KfhId);
                    newRequestActivity.TellerName = CurrentUser?.Name ?? "";
                    newRequestActivity.BranchId = CurrentUser?.DefaultBranchId;//TODO take it from current user
                    newRequestActivity.BranchName = $"{CurrentUser?.DefaultBranchName} - {CurrentUser?.Gender}";
                }

                if (searchExist)
                {
                    var existingActivities = (await GetPendingActivities(new(requestActivity.CardNumber!, [requestActivity.CfuActivity])))?.Data;
                    if (existingActivities.AnyWithNull()) throw new ApiException(message: $"Request already sent for approval, please contact the approver {existingActivities[0].ApproverName}");
                }

                //TODO: Change the date format based on current language Arabic (yyyy/MM/dd), English (dd/MM/yyyy)
                await _fdrDBContext.AddAsync(newRequestActivity);
                await _fdrDBContext.SaveChangesAsync();

                if (requestActivity!.Details.Count != 0)
                {
                    //In case of AUB card we need to store AUB Card number as "credit_card_no" in request activity parameter to align with SSO recent changes.
                    if (requestActivity.Details.Any(ra => ra.Key == Domain.Shared.Models.Reports.ReportingConstants.KEY_CREDIT_CARD_NO))
                    {
                        var kfhCard = await _fdrDBContext.Requests.AsNoTracking().FirstOrDefaultAsync(r => r.RequestId == requestActivity.RequestId);// && r.IsAUB == 1);
                        if (kfhCard is not null && kfhCard.IsAUB == 1)
                        {
                            var aubMapping = await _fdrDBContext.AubCardMappings.AsNoTracking().FirstOrDefaultAsync(aub => aub.KfhCardNo == kfhCard.CardNo);
                            if (aubMapping is not null)
                            {
                                requestActivity.Details[Domain.Shared.Models.Reports.ReportingConstants.KEY_AUB_CREDIT_CARD_NO] = aubMapping.AubCardNo;
                                requestActivity.Details[Domain.Shared.Models.Reports.ReportingConstants.KEY_IS_AUB_CARD] = true.ToString();

                                if (requestActivity.WorkflowVariables?.Any(x => x.Key == WorkflowVariables.CardNumber.ToString()) ?? false)
                                {
                                    requestActivity.WorkflowVariables[WorkflowVariables.AUBCardNumber] = aubMapping.AubCardNo;
                                }
                            }
                        }
                    }

                    await LogRequestActivityDetail(requestActivity!.Details.Select(x => new RequestActivityDetailsDto()
                    {
                        RequestActivityId = newRequestActivity.RequestActivityId,
                        Paramter = x.Key,
                        Value = x.Value
                    }).ToList());
                }
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                _auditLogger.Log.Error(ex, "unable to create request activity for {request}-{cfuactivity}-{message}", requestActivity.RequestId, requestActivity.CfuActivity.ToString(), ex.Message);
                await transaction.RollbackAsync();
                throw;
            }

            //Please check the newRequestActivity details in this newRequestActivity object  
            requestActivity.RequestActivityId = newRequestActivity.RequestActivityId;
            requestActivityId = newRequestActivity.RequestActivityId;
        }

        _auditLogger.Log.Information(GlobalResources.LogReqActivityTemplate, requestActivity.RequestId, requestActivity.CivilId, requestActivity.RequestActivityId, requestActivity.ProductName, requestActivity.CardType, requestActivity.CardNumber.Masked(6, 6), requestActivity.CfuActivity.GetDescription(), requestActivity.RequestActivityStatus.ToString(), "");

        if (isNeedWorkflow)
        {
            await InitiateWorkFlow(requestActivity);
        }

        return requestActivityId;
    }

    [HttpPost]
    public async Task InitiateWorkFlow(RequestActivityDto requestActivity)
    {
        string trackingMessage = "";
        try
        {
            Dictionary<string, object>? variables = requestActivity.WorkflowVariables ??= [];

            variables.TryAdd(WorkflowVariables.RequestedBy, _authManager.GetUser()?.KfhId ?? "anonymous");
            variables.TryAdd(WorkflowVariables.RequestId, requestActivity.RequestId.ToString("0"));
            variables.TryAdd(WorkflowVariables.RequestType, requestActivity.CfuActivity);
            variables.TryAdd(WorkflowVariables.CustomerName, requestActivity.CustomerName);
            variables.TryAdd(WorkflowVariables.RequestActivityId, requestActivity.RequestActivityId.ToString("0"));

            if (requestActivity.CardNumberDto is not null)
            {
                variables["Description"] = $"{variables["Description"]} Card#{requestActivity.CardNumberDto.DeSaltThis().Masked(6, 6).SplitByFour()}";

                if (!variables.ContainsKey(WorkflowVariables.CardNumber))
                    variables.TryAdd(WorkflowVariables.CardNumber, requestActivity.CardNumberDto!);
            }

            var workFlowKey = (WorkFlowKey)requestActivity.CfuActivityId!;

            //resetting workflow for charge card with collateral
            if (requestActivity.CfuActivityId is ((int)CFUActivity.MARGIN_ACCOUNT_CREATE or (int)CFUActivity.HOLD_ADD))
            {
                workFlowKey = WorkFlowKey.CardRequestWorkflow;
            }

            if (workFlowKey is WorkFlowKey.CardRequestWorkflow && requestActivity.IsTayseerSalaryException)
            {
                workFlowKey = WorkFlowKey.TayseerCardRequestWorkflow;
            }

            trackingMessage = $"Creating workflow for request id {requestActivity.RequestId} and  request activity id {requestActivity.RequestActivityId}";



            var workflowResponse = await _workflowAppService.CreateWorkFlow(new()
            {
                WorkFlowKey = workFlowKey,
                RequestActivityId = requestActivity.RequestActivityId,
                Variables = variables
            });


            _auditLogger.Log.Information(GlobalResources.LogReqActivityTemplate, requestActivity.RequestId,
      requestActivity.CivilId, requestActivity.RequestActivityId, requestActivity.ProductName, requestActivity.CardType,
      requestActivity.CardNumberDto.Masked(6, 6).SplitByFour(),
      requestActivity.CfuActivity.GetDescription(),
      requestActivity.RequestActivityStatus.ToString(), "workflow created successfully ");

            if (requestActivity.RequestActivityId > 0)
            {
                if (Guid.TryParse(workflowResponse.InstanceId.ToString(), out Guid workFlowInstanceId))
                {
                    trackingMessage = $"Updating RequestActivity Detail with WorkFlowInstanceId :{workFlowInstanceId}";
                    _auditLogger.Log.Information(trackingMessage);
                    await UpdateRequestActivityDetails(requestActivity.RequestActivityId, new() { { "WorkFlowInstanceId", workFlowInstanceId.ToString() } });

                    //adding "WorkFlowInstanceId" in request parameter 
                    await _fdrDBContext.RequestParameters.AddAsync(new RequestParameter() { ReqId = requestActivity.RequestId, Parameter = "WorkFlowInstanceId", Value = workFlowInstanceId.ToString() });
                    await _fdrDBContext.SaveChangesAsync();
                }
            }
        }
        catch (Exception ex)
        {
            _auditLogger.Log.Error(ex, GlobalResources.LogReqActivityTemplate,
   requestActivity.CivilId, requestActivity.RequestId, requestActivity.RequestActivityId, requestActivity.ProductName, requestActivity.CardType,
   requestActivity.CardNumber.Masked(6, 6),
   requestActivity.CfuActivity.GetDescription(),
   requestActivity.RequestActivityStatus.ToString(), $"Failed to create workflow : {trackingMessage}");
        }
    }

    [NonAction]
    public async Task UpdateRequestActivityDetails(decimal requestActivityId, Dictionary<string, string> Details)
    {
        await LogRequestActivityDetail(Details.Select(x => new RequestActivityDetailsDto()
        {
            RequestActivityId = requestActivityId,
            Paramter = x.Key,
            Value = x.Value
        }).ToList());

    }

    [NonAction]
    public async Task UpdateSingleRequestActivityDetail(decimal requestActivityId, string parameter, string value)
    {

        var detail = await _fdrDBContext.RequestActivityDetails
            .FirstOrDefaultAsync(x => x.RequestActivityId == requestActivityId && x.Paramter == parameter);

        if (detail == null)
        {
            await _fdrDBContext.RequestActivityDetails.AddAsync(new RequestActivityDetail()
            {
                Paramter = parameter,
                Value = value,
                RequestActivityId = (long)requestActivityId
            });
        }
        else
        {
            detail.Value = value;
        }


        await _fdrDBContext.SaveChangesAsync();

    }



    [HttpPost]
    public async Task<ApiResponseModel<List<RequestActivityResult>>> GetAllRequestActivity([FromBody] RequestActivityFilter filter)
    {
        var requestActivityQuery = from RA in _fdrDBContext.RequestActivities.AsNoTracking()
                                   join R in _fdrDBContext.Requests.AsNoTracking() on RA.RequestId!.Value equals R.RequestId into gp
                                   from RAP in gp.DefaultIfEmpty()
                                   select new RequestActivityResult()
                                   {
                                       RequestActivityID = RA.RequestActivityId,
                                       RequestActivityStatusID = RA.RequestActivityStatusId,
                                       ArchiveDate = RA.ArchiveDate,
                                       CreationDate = RA.CreationDate,
                                       LastUpdateDate = RA.LastUpdateDate,
                                       BranchID = RA.BranchId,
                                       BranchName = RA.BranchName,
                                       CivilID = RA.CivilId,
                                       RequestID = RA.RequestId,
                                       CustomerName = RA.CustomerName,
                                       CFUActivityID = RA.CfuActivityId,
                                       IssuanceTypeID = RA.IssuanceTypeId,
                                       TellerID = RA.TellerId,
                                       ApproverID = RA.ApproverId,
                                       TellerName = RA.TellerName,
                                       ApproverName = RA.ApproverName,
                                       CardNo = RAP != null ? RAP.CardNo : "",
                                       ReqStatus = RAP != null ? RAP.ReqStatus : (int)CreditCardStatus.Pending,
                                       RequestStatusEn = ((RequestActivityStatus)RA.RequestActivityStatusId).GetDescription(),
                                       RequestStatusAr = "",
                                       CA_DESCRIPTION_EN = "",
                                       CA_DESCRIPTION_AR = "",
                                       IT_DESCRIPTION_EN = "",
                                       IT_DESCRIPTION_AR = ""
                                   };
        requestActivityQuery = requestActivityQuery.Where(x => x.CFUActivityID != null);

        if (filter.RequestActivityID is not null)
            requestActivityQuery = requestActivityQuery.Where(x => x.RequestActivityID == filter.RequestActivityID);

        if (filter.RequestId is not null)
            requestActivityQuery = requestActivityQuery.Where(x => x.RequestID == filter.RequestId);

        if (filter.CustomerCivilId is not null)
            requestActivityQuery = requestActivityQuery.Where(x => x.CivilID == filter.CustomerCivilId);

        if (!string.IsNullOrEmpty(filter.CardNumber))
            requestActivityQuery = requestActivityQuery.Where(x => x.CardNo == filter.CardNumber);

        if (filter.ApproverId is not null)
            requestActivityQuery = requestActivityQuery.Where(x => x.ApproverID == filter.ApproverId);

        if (filter.Status is not null)
            requestActivityQuery = requestActivityQuery.Where(x => x.RequestActivityStatusID == (int)filter.Status);
        else if (!filter.FilterAll)
        {
            decimal[] allowedStatus = new decimal[] { (decimal)RequestActivityStatus.Pending, (decimal)RequestActivityStatus.Rejected, (decimal)RequestActivityStatus.Approved };
            requestActivityQuery = requestActivityQuery.Where(x => allowedStatus.Any(als => als == x.RequestActivityStatusID));
        }

        if (filter.CFUActivity is not null)
            requestActivityQuery = requestActivityQuery.Where(x => x.CFUActivityID == (int)filter.CFUActivity);

        if (filter.CFUActivities.AnyWithNull())
        {
            decimal[] cfuActivities = filter.CFUActivities!.Select(x => (decimal)x).ToArray();
            requestActivityQuery = requestActivityQuery.Where(x => cfuActivities.Any(als => als == (decimal)x.CFUActivityID));
        }

        if (filter is { FromDate: { }, ToDate: { } })
            requestActivityQuery = requestActivityQuery.Where(x => x.CreationDate >= filter.FromDate && x.CreationDate <= filter.ToDate);

        return new ApiResponseModel<List<RequestActivityResult>>().Success(await requestActivityQuery.OrderByDescending(x => x.CreationDate).ToListAsync());
    }

    [HttpPost]
    public async Task<ApiResponseModel<List<RequestActivity>>> SearchActivity([FromBody] RequestActivityDto request)
    {

        if (request.CardNumber != null && request.CardNumber.Length > 14)
        {
            request.CardNumber = request.CardNumber.DeSaltThis();
        }

        if (string.IsNullOrEmpty(request.CardNumber) && request.CardNumberDto?.Length > 14)
        {
            request.CardNumber = request.CardNumberDto.DeSaltThis();
        }

        var parameters = new OracleDynamicParameters();
        parameters.Add("cur_SEARCH_REQUEST_ACTIVITY", dbType: OracleDbType.RefCursor, direction: ParameterDirection.Output);
        parameters.Add("prm_CIVIL_ID", value: !string.IsNullOrWhiteSpace(request.CivilId) ? request.CivilId : DBNull.Value);
        parameters.Add("prm_CARD_NUMBER", value: !string.IsNullOrWhiteSpace(request.CardNumber) ? request.CardNumber : DBNull.Value);
        parameters.Add("prm_REQUEST_ACTIVITY_ID", value: request.RequestActivityId != 0 ? request.RequestActivityId : DBNull.Value);
        parameters.Add("prm_CFU_ACTIVITY_ID", value: (request?.CfuActivityId ?? 0) != 0 ? request?.CfuActivityId ?? 0 : DBNull.Value);
        parameters.Add("prm_BRANCH_ID", value: request?.BranchId is not null ? request?.BranchId ?? 0 : DBNull.Value);
        parameters.Add("prm_STATUS_ID", value: request?.RequestActivityStatusId != 0 ? request?.RequestActivityStatusId ?? 0 : DBNull.Value);
        parameters.Add("prm_TELLER_ID", value: request?.TellerId != 0 ? request?.TellerId ?? 0 : DBNull.Value);
        parameters.Add("prm_APPROVER_ID", value: request?.ApproverId != 0 ? request?.ApproverId ?? 0 : DBNull.Value);
        parameters.Add("prm_ISSUANCE_TYPE_ID", value: request?.IssuanceTypeId != 0 ? request?.IssuanceTypeId ?? 0 : DBNull.Value);
        parameters.Add("prm_ENABLED", value: DBNull.Value);

        if (request.FromDate != null && request.ToDate != null)
        {
            if (request.FromDate != DateTime.MinValue && request.ToDate != DateTime.MinValue)
            {
                parameters.Add("prm_FROM_DATE", request.FromDate);
                parameters.Add("prm_TO_DATE", request.ToDate);
            }
            else
            {
                parameters.Add("prm_FROM_DATE", DBNull.Value);
                parameters.Add("prm_TO_DATE", DBNull.Value);
            }
        }
        else
        {
            parameters.Add("prm_FROM_DATE", DBNull.Value);
            parameters.Add("prm_TO_DATE", DBNull.Value);
        }


        using var conn = new OracleConnection(_fdrDBContext.Database.GetConnectionString());
        var activities = await conn.QueryAsync<dynamic>("FDR.SEARCH_REQUEST_ACTIVITY", param: parameters, commandType: CommandType.StoredProcedure);
        if (activities == null) return new();
        return Success(activities.AsQueryable().ProjectToType<RequestActivity>().ToList());
    }

    [HttpPost]
    public async Task<ApiResponseModel<List<RequestActivityDto>>> GetPendingActivities([FromBody] PendingActivityRequest request)
    {

        var pendingActivities = from ra in _fdrDBContext.RequestActivities.AsNoTracking()
                                join req in _fdrDBContext.Requests.AsNoTracking() on ra.RequestId equals req.RequestId
                                where ra.RequestActivityStatusId == (int)RequestActivityStatus.Pending && req.CardNo == request.CardNumber
                                select ra;

        if (request.activities.AnyWithNull())
        {
            var activityFilter = request.activities?.Select(x => (decimal?)x);
            pendingActivities = pendingActivities.Where(ra => activityFilter.Any(id => id == ra.CfuActivityId));
        }

        return Success(await pendingActivities.ProjectToType<RequestActivityDto>().ToListAsync());
    }



    private async Task LogRequestActivityDetail(List<RequestActivityDetailsDto> details)
    {
        await _fdrDBContext.RequestActivityDetails.AddRangeAsync(details.AsQueryable().ProjectToType<RequestActivityDetail>());
        await _fdrDBContext.SaveChangesAsync();
    }


}



