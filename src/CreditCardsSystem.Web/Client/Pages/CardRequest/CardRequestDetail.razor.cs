using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Interfaces;
using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Models.Card;
using CreditCardsSystem.Domain.Models.Reports;
using CreditCardsSystem.Domain.Models.Workflow;
using CreditCardsSystem.Domain.Shared.Interfaces.Workflow;
using CreditCardsSystem.Domain.Shared.Models.RequestActivity;
using CreditCardsSystem.Utility.Extensions;
using CreditCardsSystem.Web.Client.Components;
using CreditCardsSystem.Web.Client.Pages.CardRequest.Components;
using CreditCardsSystem.Web.Client.Shared;
using Kfh.Aurora.Blazor.Components.UI;
using Kfh.Aurora.Workflow.Dto;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Telerik.Barcode;
using Telerik.DataSource.Extensions;

namespace CreditCardsSystem.Web.Client.Pages.CardRequest;

public partial class CardRequestDetail : ApplicationComponent, IDisposable
{
    [Inject] private IRequestActivityAppService ActivityService { get; set; } = null!;
    [Inject] private IReportAppService ReportService { get; set; } = null!;
    [Inject] private ICardDetailsAppService CardDetailsAppService { get; set; } = null!;
    [Inject] private IWorkflowAppService WorkflowAppService { get; set; } = null!;
    [Inject] IRequestAppService RequestAppService { get; set; } = null!;

    [Parameter]
    [SupplyParameterFromQuery]
    public Guid? Id { get; set; }

    #region Variables

    public DataItem<RequestActivityDto> RequestActivity { get; set; } = new();
    private GetCommentsResponse? Comments { get; set; }
    public TaskResult TaskDetail { get; private set; }
    public bool ReturnDialogVisible { get; set; }
    public bool ReSubmitDialogVisible { get; set; }
    public bool CancelDialogVisible { get; set; }
    public bool RejectDialogVisible { get; set; }
    public bool ApproveDialogVisible { get; set; }
    ActionType CurrentAction { get; set; } = default!;
    public bool ShowLoader => isReadyForAction && actionStatus.ProcessStatus != DataStatus.Loading;
    public bool IsRejected => RequestActivity?.Data?.RequestActivityStatus == RequestActivityStatus.Rejected || (TaskDetail?.Title?.Contains("Return") ?? false);
    public string Title => IsRejected ? $"Rejected By {TaskDetail?.Assignee}" : $"Requested By {RequestActivity?.Data?.TellerName} ({RequestActivity?.Data?.TellerId})";
    bool isReadyForAction;
    CFUActivity RequestActivityType { get; set; }
    string RejectionReason { get; set; } = string.Empty;
    //bool ShowRejectionConfirmation { get; set; }
    public List<Breadcrumb.BreadcrumbItem> BreadCrumbsItems { get; set; } = [];
    ActionStatus actionStatus { get; set; } = new();
    public bool isViewOnly { get; set; }
    string ViewOnlyReason { get; set; } = "View Only!";
    bool IsSupplementaryCard { get; set; }
    static Dictionary<string, object> DefaultParameters { get; set; } = new() { { "TaskDetail", null! }, { "RequestActivity", null! }, { "RejectionReason", null! } };
    Type? activityComponentType;
    DynamicComponent? dynamicComponent;
    IWorkflowMethods action => (dynamicComponent!.Instance as IWorkflowMethods)!;
    Dictionary<CFUActivity, ComponentMetadata> components = new()
    {
            { CFUActivity.Replace_On_Damage ,new(){ T=typeof(ReplacementDamageForm), Parameters= DefaultParameters} },
            { CFUActivity.Replace_On_Lost_Or_Stolen ,new(){ T=typeof(ReplacementLostForm), Parameters= DefaultParameters} },
            { CFUActivity.CARD_RE_ACTIVATION ,new(){ T= typeof(ReActivationForm), Parameters= DefaultParameters} },
            { CFUActivity.Card_Closure,new(){ T=typeof(ClosureForm), Parameters= DefaultParameters} },
            { CFUActivity.CHANGE_BILLING_ADDRESS,new(){ T= typeof(ChangeAddressForm), Parameters= DefaultParameters} },
            { CFUActivity.CHANGE_CARDHOLDERNAME ,new(){ T=typeof(ChangeHolderNameForm), Parameters= DefaultParameters} },
            { CFUActivity.CHANGE_CARD_LINKED_ACCT ,new(){ T=typeof(ChangeLinkedAccountForm), Parameters= DefaultParameters} },
            { CFUActivity.LIMIT_CHANGE_INCR ,new(){ T=typeof(ChangeLimitForm), Parameters= DefaultParameters} },
            { CFUActivity.MIGRATE_COLLATERAL_DEPOSIT ,new(){ T=typeof(MigrateCollateralForm), Parameters= DefaultParameters} },
            { CFUActivity.MIGRATE_COLLATERAL_MARGIN ,new(){ T=typeof(MigrateCollateralForm), Parameters= DefaultParameters} },
            { CFUActivity.CorporateProfileAdd ,new(){ T=typeof(CorporateProfileForm), Parameters= DefaultParameters} },
            { CFUActivity.CorporateProfileUpdate ,new(){ T=typeof(CorporateProfileForm), Parameters= DefaultParameters} },
            { CFUActivity.Card_Request ,new(){ T=typeof(ApprovalForm), Parameters= DefaultParameters} },
            { CFUActivity.HOLD_ADD ,new(){ T=typeof(ApprovalForm), Parameters= DefaultParameters} },
            { CFUActivity.MARGIN_ACCOUNT_CREATE ,new(){ T=typeof(ApprovalForm), Parameters= DefaultParameters} },
            { CFUActivity.MemberShipDeleteRequest ,new(){ T=typeof(MembershipRequestForm), Parameters= DefaultParameters} },
            { CFUActivity.CREDIT_REVERSE ,new(){ T=typeof(CreditReverseForm), Parameters= DefaultParameters} }


    };

    public CardDetailsResponse? SelectedCard { get; set; } = default!;
    #endregion

    protected override async Task OnParametersSetAsync()
    {
        if (Id is null)
        {
            Notification.Failure("Invalid Task!");
            return;
        }

        await LoadFormData();

        BreadCrumbsItems = [
              new() { Text = "Tasks", Url = "/cc-tasks" },
            new() { Text = RequestActivity.Data?.CfuActivity.GetDescription() ?? "", Url = "/" },
        ];
    }

    async Task LoadFormData()
    {
        try
        {
            isReadyForAction = false;

            Notification.Loading("Loading task detail");
            RequestActivity.Loading();

            TaskDetail = await WorkflowAppService.GetTaskById(Id!.Value);

            if (TaskDetail?.Id == default(Guid))
            {
                Notification.Hide();
                RequestActivity.Error(new(GlobalResources.InvalidTask));
                return;
            }


            BreadCrumbsItems = [new() { Text = "Tasks", Url = "/cc-tasks" },
               new() { Text = RequestActivity.Data != null ? RequestActivity.Data?.CfuActivity.GetDescription() ?? "" : TaskDetail.Title, Url = "/" }];

            Notification.Loading("Loading task comments");
            Comments = await WorkflowAppService.GetComments(TaskDetail.InstanceId);

            if (TaskDetail.Status.Equals("CANCELED", StringComparison.CurrentCultureIgnoreCase))
            {
                Notification.Hide();
                RequestActivity.SetData(new RequestActivityDto() { CivilId = "", IssuanceTypeId = 1 });
                StateHasChanged();
                return;
            }




            //_ = bool.TryParse(TaskDetail!.Payload.GetValueOrDefault(WorkflowVariables.NoActivityRequest)?.ToString(), out bool noActivityRequest);
            _ = Enum.TryParse(TaskDetail!.Payload.GetValueOrDefault(WorkflowVariables.RequestType)?.ToString(), out CFUActivity requestType);
            //_ = long.TryParse(TaskDetail!.Payload.GetValueOrDefault(WorkflowVariables.MemberShipDeleteRequestId)?.ToString(), out long memberShipDeleteRequestId);



            RequestActivityDto? requestActivity = new();



            if (requestType is not CFUActivity.MemberShipDeleteRequest)
            {
                _ = long.TryParse(TaskDetail!.Payload.GetValueOrDefault(WorkflowVariables.RequestActivityId)?.ToString(), out long requestActivityId);
                _ = long.TryParse(TaskDetail!.Payload.GetValueOrDefault(WorkflowVariables.RequestId)?.ToString(), out long requestId);

                Notification.Loading("Loading card information");

                SelectedCard = (await CardDetailsAppService.GetCardInfo(requestId))?.Data;

                Notification.Loading("Loading requestActivity detail");
                requestActivity = (await ActivityService.GetRequestActivityById(requestActivityId))?.Data;

                if (SelectedCard is null && requestType is not (CFUActivity.CorporateProfileAdd or CFUActivity.CorporateProfileUpdate))
                {
                    if (requestActivity is null)
                    {
                        await CancelWorkFlow();
                    }
                    else
                    {
                        Notification.Failure(GlobalResources.InvalidCardDetail);
                        RequestActivity.Error(new(GlobalResources.InvalidCardDetail));
                    }
                    return;
                }


            }

            if (SelectedCard is not null)
                IsSupplementaryCard = SelectedCard?.Parameters?.IsSupplementaryOrPrimaryChargeCard?.ToUpper()?.Equals("S", StringComparison.InvariantCultureIgnoreCase) == true;

            //this change only for new charge card
            #region change workflow for new charge card
            string[] approvalTasks = ["CardApproverTask", "BCDCardApproverTask"];
            if (approvalTasks.Contains(TaskDetail?.Type?.Trim()) && requestType is (CFUActivity.MARGIN_ACCOUNT_CREATE or CFUActivity.HOLD_ADD))
            {
                requestActivity.CfuActivityId = (int)WorkFlowKey.CardRequestWorkflow;
                requestType = CFUActivity.Card_Request;
            }
            #endregion
            Notification.Hide();
            RequestActivity.SetData(requestActivity ?? new());

            RequestActivityType = requestType;
            activityComponentType = components[RequestActivityType].T;
            components[RequestActivityType].Parameters[nameof(TaskDetail)] = TaskDetail;
            components[RequestActivityType].Parameters[nameof(RequestActivity)] = RequestActivity?.Data ?? new();
            components[RequestActivityType].Parameters[nameof(RejectionReason)] = RejectionReason;
            components[RequestActivityType].Parameters[nameof(SelectedCard)] = SelectedCard!;
            components[RequestActivityType].Parameters[nameof(ReadyForAction)] = EventCallback.Factory.Create<bool>(this, ReadyForAction);
            components[RequestActivityType].Parameters[nameof(Listen)] = EventCallback.Factory.Create<ActionStatus>(this, Listen);

            await CheckingAccess();

            async Task CancelWorkFlow()
            {
                Notification.Failure(GlobalResources.InvalidTask);
                RequestActivity.Error(new(GlobalResources.InvalidTask));
                await WorkflowAppService.CancelWorkFlow(TaskDetail.InstanceId, TaskDetail.Id);
                return;
            }

        }
        catch (Exception ex)
        {
            RequestActivity.Error(new($"Unable to fetch task detail! {ex.Message}", ex.InnerException));
        }
    }
    async Task CheckingAccess()
    {
        CFUActivity[] makerCheckerActivity = [CFUActivity.LIMIT_CHANGE_INCR,
            CFUActivity.Card_Closure,
            CFUActivity.HOLD_ADD,
            CFUActivity.MARGIN_ACCOUNT_CREATE,
            CFUActivity.Replace_On_Damage,
            CFUActivity.Replace_On_Lost_Or_Stolen,
            CFUActivity.CHANGE_CARD_LINKED_ACCT,
            CFUActivity.CHANGE_BILLING_ADDRESS,
            CFUActivity.CARD_RE_ACTIVATION,
            CFUActivity.Card_Request,
            CFUActivity.CREDIT_REVERSE];

        if (!AuthManager.IsAuthenticated())
        {
            await Listen(new() { IsAccessDenied = true });
            return;
        }

        //maker validation
        if (RequestActivity.Data is null)
        {
            await ReadyForAction(true);
            return;
        }


        //checker validation
        bool IsMakerCheckerAreSame = false;

        if (makerCheckerActivity.Contains(RequestActivity.Data!.CfuActivity))
        {
            if (makerCheckerActivity.IndexOf(RequestActivity.Data.CfuActivity) != -1)
            {
                var payloadTellerId = TaskDetail!.Payload.GetValueOrDefault(WorkflowVariables.RequestedBy)?.ToString();
                IsMakerCheckerAreSame = AuthManager.GetUser()!.KfhId.Equals(payloadTellerId ?? RequestActivity?.Data?.TellerId.ToString("0"));

                if (IsMakerCheckerAreSame)
                {
                    ViewOnlyReason = GlobalResources.MakerCheckerAreSame;
                }
            }
        }

        bool IsNotPendingActivity = RequestActivity?.Data?.RequestActivityStatus is not (RequestActivityStatus.Pending or RequestActivityStatus.New);
        if (IsNotPendingActivity && !ViewOnlyReason.Contains(GlobalResources.NoActionRequired))
            ViewOnlyReason += $"{(ViewOnlyReason.Length > 0 ? "," : "")} {GlobalResources.NoActionRequired}";


        if (IsMakerCheckerAreSame || IsNotPendingActivity)
        {
            await Listen(new() { IsAccessDenied = true, Message = ViewOnlyReason });
        }
        else
            await ReadyForAction(true);


    }
    async Task Listen(ActionStatus actionStatus)
    {
        this.actionStatus = actionStatus;
        Notification.Hide();
        if (actionStatus.IsAccessDenied)
        {
            ViewOnlyReason = string.IsNullOrEmpty(actionStatus.Message) ? "You do not have the necessary permission!" : actionStatus.Message;
            Notification.Failure(ViewOnlyReason);
            isViewOnly = true;
            await ReadyForAction(false);
            return;
        }

        if (actionStatus.ProcessStatus is DataStatus.Loading)
        {
            Notification.Loading(actionStatus.Message);
            CloseAllDialogs();
            return;
        }


        if (actionStatus.ProcessStatus is DataStatus.Processing)
        {
            Notification.Hide();
            Notification.Processing(actionStatus);
            CloseAllDialogs();
            return;
        }

        if (!actionStatus.IsSuccess)
        {
            Notification.Failure(actionStatus.Message);
            isViewOnly = true;
            ViewOnlyReason += actionStatus.Message;
            return;
        }


        Notification.Success(actionStatus.Message);
        await Task.Delay(2000);


        if (CurrentAction is ActionType.Canceled)
            await WorkflowAppService.CancelWorkFlow(taskId: TaskDetail!.Id, instanceId: TaskDetail.InstanceId);

        GoToMyTaskPage();
    }
    async Task ReadyForAction(bool isSuccess = false)
    {
        isReadyForAction = isSuccess;
        StateHasChanged();
        await Task.CompletedTask;
    }
    async Task ReprintRequestForm()
    {
        Notification.Loading("Downloading form...");


        var eFormResponse = RequestActivityType is CFUActivity.Card_Request ?
                 await ReportService.GenerateCardIssuanceEForm(RequestActivity.Data!.RequestId) :
                 await ReportService.GenerateAfterSalesForm(getAfterSalesFormData());

        if (!eFormResponse.IsSuccess)
        {
            Notification.Failure("Unable to generate Form, try again later from card list");
            return;
        }

        var formResponse = eFormResponse.Data!;
        var streamData = new MemoryStream(formResponse!.FileBytes!);
        using var streamRef = new DotNetStreamReference(stream: streamData);
        await Js.InvokeVoidAsync("downloadFileFromStream", $"{formResponse?.FileName}", streamRef);
        Notification.Success("Downloaded!");


        AfterSalesForm getAfterSalesFormData()
        {

            var payload = TaskDetail!.Payload;

            var afterSalesForm = new AfterSalesForm()
            {
                RequestId = RequestActivity.Data!.RequestId!,
                HolderName = SelectedCard?.HolderEmbossName,
                Address = $"POBox:{SelectedCard?.BillingAddress?.PostOfficeBoxNumber?.ToString()} " +
                $"City:{SelectedCard?.BillingAddress?.City?.ToString()} " +
                $"Post Code:{SelectedCard?.BillingAddress?.PostalCode?.ToString()} " +
                $"Street:{SelectedCard?.BillingAddress?.Street?.ToString()}",
                MobileNo = SelectedCard?.BillingAddress?.Mobile?.ToString(),
                Tel = $"Home Phone:{SelectedCard?.BillingAddress?.HomePhone?.ToString()} Work Phone:{SelectedCard?.BillingAddress?.WorkPhone?.ToString()}"
            };

            if (RequestActivityType is CFUActivity.CHANGE_BILLING_ADDRESS)
            {
                afterSalesForm.ActionType = RequestType.ChangeAddress.ToString();
                afterSalesForm.Address = $"POBox:{payload.GetValueOrDefault(WorkflowVariables.PoBoxNumber)?.ToString()} City:{payload.GetValueOrDefault(WorkflowVariables.City)?.ToString()} Post Code:{payload.GetValueOrDefault(WorkflowVariables.ZipCode)?.ToString()} Street:{payload.GetValueOrDefault(WorkflowVariables.Street)?.ToString()}";
                afterSalesForm.MobileNo = payload.GetValueOrDefault(WorkflowVariables.MobilePhone)?.ToString();
                afterSalesForm.Tel = $"Home Phone:{payload.GetValueOrDefault(WorkflowVariables.HomePhone)?.ToString()} Work Phone:{payload.GetValueOrDefault(WorkflowVariables.WorkPhone)?.ToString()}";
            }

            if (RequestActivityType is CFUActivity.Replace_On_Damage)
            {
                afterSalesForm.ActionType = RequestType.ReplacementForDamage.ToString();
                afterSalesForm.HolderName = payload.GetValueOrDefault(WorkflowVariables.NewCardHolderName)?.ToString();
            }


            if (RequestActivityType is CFUActivity.Card_Closure)
            {
                afterSalesForm.ActionType = RequestType.CardClosure.ToString();
                afterSalesForm.HolderName = payload.GetValueOrDefault(WorkflowVariables.NewCardHolderName)?.ToString();
            }


            if (RequestActivityType is CFUActivity.CHANGE_CARD_LINKED_ACCT)
            {
                afterSalesForm.ActionType = RequestType.ChangeLinkAccountNumber.ToString();
                afterSalesForm.AccountNo = payload.GetValueOrDefault(WorkflowVariables.AccountNumber)?.ToString();
                afterSalesForm.HolderName = payload.GetValueOrDefault(WorkflowVariables.NewCardHolderName)?.ToString();
                afterSalesForm.Address = $"POBox:{payload.GetValueOrDefault(WorkflowVariables.PoBoxNumber)?.ToString()} City:{payload.GetValueOrDefault(WorkflowVariables.City)?.ToString()} Post Code:{payload.GetValueOrDefault(WorkflowVariables.ZipCode)?.ToString()} Street:{payload.GetValueOrDefault(WorkflowVariables.Street)?.ToString()}";
                afterSalesForm.MobileNo = payload.GetValueOrDefault(WorkflowVariables.MobilePhone)?.ToString();
                afterSalesForm.Tel = $"Home Phone:{payload.GetValueOrDefault(WorkflowVariables.HomePhone)?.ToString()} Work Phone:{payload.GetValueOrDefault(WorkflowVariables.WorkPhone)?.ToString()}";
            }

            if (RequestActivityType is CFUActivity.LIMIT_CHANGE_INCR)
            {
                afterSalesForm.ActionType = RequestType.ChangeLimit.ToString();
                afterSalesForm.HolderName = SelectedCard?.HolderEmbossName;
                afterSalesForm.NewLimit = payload.GetValueOrDefault(WorkflowVariables.NewLimit)?.ToString();
                afterSalesForm.OldLimit = SelectedCard?.ApproveLimit.ToString();
                afterSalesForm.IsTemporaryLimitChange = payload.GetValueOrDefault(WorkflowVariables.IsTemporary)?.ToString()?.Equals("Temporary") ?? false;
            }

            if (RequestActivityType is CFUActivity.CHANGE_CARDHOLDERNAME)
            {
                afterSalesForm.ActionType = RequestType.ChangeCardHolderName.ToString();
                afterSalesForm.HolderName = payload.GetValueOrDefault(WorkflowVariables.NewCardHolderName)?.ToString();
                afterSalesForm.Address = $"POBox:{payload.GetValueOrDefault(WorkflowVariables.PoBoxNumber)?.ToString()} City:{payload.GetValueOrDefault(WorkflowVariables.City)?.ToString()} Post Code:{payload.GetValueOrDefault(WorkflowVariables.ZipCode)?.ToString()} Street:{payload.GetValueOrDefault(WorkflowVariables.Street)?.ToString()}";
                afterSalesForm.MobileNo = payload.GetValueOrDefault(WorkflowVariables.MobilePhone)?.ToString();
                afterSalesForm.Tel = $"Home Phone:{payload.GetValueOrDefault(WorkflowVariables.HomePhone)?.ToString()} Work Phone:{payload.GetValueOrDefault(WorkflowVariables.WorkPhone)?.ToString()}";
            }

            return afterSalesForm;
        }
    }

    private async Task RejectRequest()
    {
        RejectDialogVisible = false;
        CurrentAction = ActionType.Rejected;
        await Listen(new(true, ProcessStatus: DataStatus.Processing, Title: "Rejection", Message: $"Processing your request for rejection..."));
        await action.ProcessAction(CurrentAction, RejectionReason);
    }

    private async Task ApproveRequest()
    {
        if (TaskDetail is not null && TaskDetail?.Id != default)
        {
            CurrentAction = ActionType.Approved;
            await Listen(new(true, ProcessStatus: DataStatus.Processing, Title: "Approval", Message: "Approval in currently being processed."));
            await action.ProcessAction(CurrentAction);
        }
    }
    void CloseAllDialogs()
    {
        ApproveDialogVisible = false;
        RejectDialogVisible = false;
        CancelDialogVisible = false;
        ReSubmitDialogVisible = false;
        ReturnDialogVisible = false;
    }
    async Task Cancel()
    {
        CurrentAction = ActionType.Canceled;
        CancelDialogVisible = false;
        //if(RequestActivityType)
        //await action.Cancel();
        if (long.TryParse(TaskDetail!.Payload.GetValueOrDefault(WorkflowVariables.RequestId)?.ToString(), out long requestId))
        {
            var response = await RequestAppService.CancelRequest(requestId);
            DataStatus ProcessStatus = response is null ? DataStatus.Loading : (response.IsSuccess ? DataStatus.Success : DataStatus.Error);
            await Listen(new(response?.IsSuccess ?? false, ProcessStatus, Message: response?.Message ?? ""));
        }

    }
    void GoToMyTaskPage() => NavigateTo(Page.RequestList);

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
