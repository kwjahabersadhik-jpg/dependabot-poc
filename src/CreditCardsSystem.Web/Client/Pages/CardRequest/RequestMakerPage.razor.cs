using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Interfaces;
using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Models.Access;
using CreditCardsSystem.Domain.Models.Card;
using CreditCardsSystem.Domain.Models.CardOperation;
using CreditCardsSystem.Domain.Models.Reports;
using CreditCardsSystem.Domain.Models.RequestActivity;
using CreditCardsSystem.Utility.Extensions;
using CreditCardsSystem.Web.Client.Components;
using CreditCardsSystem.Web.Client.Components.Dialog;
using CreditCardsSystem.Web.Client.Pages.CardRequest.Components;
using Kfh.Aurora.Blazor.Components.UI;
using Kfh.Aurora.Organization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using Telerik.Reporting;

namespace CreditCardsSystem.Web.Client.Pages.CardRequest;


public partial class RequestMakerPage : IDisposable
{
    [Inject] IActivationAppService ActivationService { get; set; } = null!;
    [Inject] IStopAndReportAppService StopAndReportService { get; set; } = null!;
    [Inject] IReportAppService ReportService { get; set; } = null!;
    [Inject] ICardDetailsAppService CardDetailsService { get; set; } = null!;
    [Inject] IRequestAppService RequestService { get; set; } = null!;
    [Inject] IConfigurationAppService ConfigurationService { get; set; } = null!;
    [Inject] IRequestActivityAppService RequestActivityService { get; set; } = null!;


    [Parameter]
    public bool IsFromCardSummary { get; set; } = false;


    [Parameter]
    public EventCallback<Task> ReloadData { get; set; }

    [Parameter]
    public EventCallback<Task> ReloadStandingOrders { get; set; }

    [Parameter]
    public EventCallback<decimal> ReloadCardData { get; set; }

    [Parameter]
    public EventCallback<decimal> RemoveFromList { get; set; }


    [Parameter]
    public EventCallback<NewCardStatus> ReflectCardStatus { get; set; }


    public string ActionMessage = "";

    public CancellationTokenSource? cancellationTokenSource;

    public string Title = "";
    public bool DialogFormVisible { get; set; } = false;
    public int? StandingOrderId { get; set; }
    public bool EnableBeneficiarySelection { get; set; }
    public CreditCardDto? SelectedCard { get; set; }
    public OffCanvas canvasRef { get; set; } = default!;
    private DialogBoxOptions options { get; set; } = default!;
    public ValidateCardClosureResponse? CardClosureFormData { get; set; }
    bool isReadyForAction { get; set; }
    private RequestType SelectedRequestType { get; set; }
    private ValidationMessageStore? validationMessageStore;
    public CardDetailsResponse? cardInfo => SelectedCard?.CardBalance?.Data;
    ActionStatus actionStatus { get; set; } = new();
    ActionMessageStatus actionMessageStatus { get; set; } = new();
    bool ConfirmDialogVisible { get; set; } = false;
    static Dictionary<string, object> DefaultParameters { get; set; } = new() { { "RequestActivity", null }, { "RejectionReason", null } };
    Type? activityComponentType;
    DynamicComponent? dynamicComponent;
    IWorkflowMethods componentAction => (dynamicComponent!.Instance as IWorkflowMethods)!;
    Dictionary<RequestType, ComponentMetadata> components = new()
    {
            { RequestType.ChangeAddress,new(){ T= typeof(ChangeAddressForm), Parameters= DefaultParameters} },
            { RequestType.ChangeLimit,new(){ T= typeof(ChangeLimitForm), Parameters= DefaultParameters} },
            { RequestType.ChangeCardHolderName,new(){ T= typeof(ChangeHolderNameForm), Parameters= DefaultParameters} },
            { RequestType.ChangeLinkAccountNumber,new(){ T= typeof(ChangeLinkedAccountForm), Parameters= DefaultParameters} },
            { RequestType.Detail,new(){ T= typeof(MoreDetail), Parameters= DefaultParameters} },
            { RequestType.StandingOrder,new(){ T= typeof(StandingOrderForm), Parameters= DefaultParameters} },
            { RequestType.CardPayment,new(){ T= typeof(PaymentForm), Parameters= DefaultParameters} },
            { RequestType.Migration,new(){ T= typeof(MigrateCollateralForm), Parameters= DefaultParameters} },
            { RequestType.ChangeStatus,new(){ T= typeof(ChangeStatusForm), Parameters= DefaultParameters} },
            { RequestType.Activate,new(){ T= typeof(ActivationForm), Parameters= DefaultParameters} },
            { RequestType.ReplacementForDamage,new(){ T= typeof(ReplacementDamageForm), Parameters= DefaultParameters} },
            { RequestType.ReplacementForLost,new(){ T= typeof(ReplacementLostForm), Parameters= DefaultParameters} },
            { RequestType.CardClosure,new(){ T= typeof(ClosureForm), Parameters= DefaultParameters} },
            { RequestType.CreditReverse,new(){ T= typeof(CreditReverseForm), Parameters= DefaultParameters} }


    };
    public bool ViewOnly { get; set; }

    protected override async Task OnInitializedAsync()
    {

        await Task.CompletedTask;
    }

    private IEnumerable<RequestType> dialogTypeForms = new List<RequestType>() {
        RequestType.Cancel,
        RequestType.StopCard,
        RequestType.ReportLostOrStolen,
        RequestType.ReActivate };

    public async Task OnChangeAction(RequestType selectedRequestType)
    {
        ResetData();

        SelectedRequestType = selectedRequestType;


        await IsHavingPendingRequest();

        if (SelectedRequestType is RequestType.DownloadEForm)
        {
            await CardIssuanceEForm();
            return;
        }

        if (SelectedRequestType is RequestType.ReplaceTrackReport)
        {
            NavigateTo("replacement-tracking-report", new() { { "RequestId", SelectedCard?.RequestId.ToString() ?? "" } });
            return;
        }

        SetActionState();

        if (dialogTypeForms.Any(x => x == SelectedRequestType))
            await OpenDialogForm();
        else
            await OpenCanvasForm();

        StateHasChanged();

        return;
    }

    private async Task CancelProcess()
    {
        if (cancellationTokenSource != null && IsProcessing)
        {
            cancellationTokenSource.Dispose();
            cancellationTokenSource = null;
            Notification.Failure("current process has been cancelled!");
        }

        await canvasRef.ToggleAsync();

    }
    private async Task SubmitRequest()
    {
        ConfirmDialogVisible = false;

        bool isSuccess = await componentAction.SubmitRequest((this.cancellationTokenSource ??= new()).Token);

        if (isSuccess)
        {
            if (IsFromCardSummary && SelectedCard?.RequestId != null)
            {
                if (SelectedRequestType is (RequestType.Activate or RequestType.CardPayment))
                {
                    if (SelectedRequestType is RequestType.Activate)
                        await ReflectCardStatus.InvokeAsync(new(SelectedCard!.RequestId, CreditCardStatus.Active));

                    await ReloadCardData.InvokeAsync(SelectedCard!.RequestId);
                }

                if (SelectedRequestType is RequestType.StandingOrder)
                {
                    await ReloadStandingOrders.InvokeAsync();
                }
            }
            else
            {
                await ReloadData.InvokeAsync();
            }
        }
    }

    void SetActionState(bool isSuccess = false)
    {
        actionStatus = new();
        isReadyForAction = isSuccess;
        Notification.Hide();
        SetTitle();
    }

    void ResetData()
    {
        CardClosureFormData = null;
        TayseerCardList = null;
        PendingRequests = null;
        Notification.Hide();
    }

    async Task SetActionMessage(ActionMessageStatus actionStatus)
    {
        this.actionMessageStatus = actionStatus;
        await Task.CompletedTask;
    }
    async Task ReadyForAction(bool isSuccess = false)
    {
        isReadyForAction = isSuccess;
        await Task.CompletedTask;
    }
    async Task Listen(ActionStatus actionStatus)
    {

        this.actionStatus = actionStatus;
        Notification.Hide();


        if (actionStatus.IsAccessDenied)
        {
            Notification.Failure(string.IsNullOrEmpty(actionStatus.Message) ? "You do not have permission!" : actionStatus.Message);
            await ReadyForAction(false);
            return;
        }

        if (actionStatus.ProcessStatus is DataStatus.Loading)
        {
            Notification.Loading(actionStatus.Message);
            return;
        }

        if (actionStatus.ProcessStatus is DataStatus.Processing)
        {
            Notification.Processing(actionStatus);
            return;
        }

        if (actionStatus.ShowConfirmation)
        {
            ConfirmDialogVisible = true;
            StateHasChanged();
            return;
        }

        if (actionStatus.ProcessStatus != DataStatus.Loading)
            Notification.Show(actionStatus.IsSuccess ? AlertType.Success : AlertType.Error, actionStatus.Message);

        if (actionStatus.CloseDialog)
            StateHasChanged();

        if (actionStatus.IsSuccess)
            await CloseRequestForm();
    }
    public bool ShowCardDetail { get; set; }
    async Task CardIssuanceEForm()
    {
        Notification.Loading("Generating E-Form is in process..");

        var eFormResponse = await ReportService.GenerateCardIssuanceEForm(SelectedCard?.RequestId ?? 0);
        // ResetProcessStatus();

        if (!eFormResponse.IsSuccess)
        {
            Notification.Failure("Unable to generate EForm, try again later from card list");
            return;
        }

        Notification.Success("Downloaded E-Form!");

        var streamData = new MemoryStream(eFormResponse.Data!.FileBytes!);
        using var streamRef = new DotNetStreamReference(stream: streamData);
        await Js.InvokeVoidAsync("downloadFileFromStream", $"{eFormResponse.Data?.FileName}", streamRef);
    }
    async Task OpenCanvasForm()
    {
        Notification.HasOffCanvas = true;

        components[SelectedRequestType].Parameters.Clear();
        activityComponentType = components[SelectedRequestType].T;
        if (cardInfo is not null)
        {
            cardInfo!.MemberShipId = SelectedCard?.MemberShipId;


            #region Checking is there any pending request activity
            if (PendingRequests.AnyWithNull())
            {
                components[SelectedRequestType].Parameters[nameof(PendingRequests)] = PendingRequests!;
            }
            #endregion
        }

        components[SelectedRequestType].Parameters[nameof(SelectedCard)] = cardInfo;
        components[SelectedRequestType].Parameters[nameof(ReadyForAction)] = EventCallback.Factory.Create<bool>(this, ReadyForAction);
        components[SelectedRequestType].Parameters[nameof(Listen)] = EventCallback.Factory.Create<ActionStatus>(this, Listen);
        components[SelectedRequestType].Parameters[nameof(SetActionMessage)] = EventCallback.Factory.Create<ActionMessageStatus>(this, SetActionMessage);
        components[SelectedRequestType].Parameters[nameof(DownloadAfterSalesEForm)] = EventCallback.Factory.Create<AfterSalesForm>(this, DownloadAfterSalesEForm);

        ShowCardDetail = true;

        if (SelectedRequestType is RequestType.StandingOrder)
        {
            components[SelectedRequestType].Parameters[nameof(StandingOrderForm.EnableBeneficiarySelection)] = EnableBeneficiarySelection;
            components[SelectedRequestType].Parameters[nameof(StandingOrderForm.BeneficiaryCardNumber)] = cardInfo?.CardNumberDto ?? "";
            components[SelectedRequestType].Parameters[nameof(StandingOrderForm.StandingOrderId)] = StandingOrderId ?? 0;
            ShowCardDetail = false;
        }

        ViewOnly = SelectedRequestType is RequestType.Detail;

        bool isEnablePrintApplication = SelectedRequestType is RequestType.ChangeAddress or RequestType.ChangeCardHolderName
            or RequestType.ChangeLinkAccountNumber or RequestType.CardClosure
            or RequestType.ChangeLimit or RequestType.ReplacementForDamage or RequestType.ReplacementForDamage
            or RequestType.StopCard;

        string extraButtonLabel = isEnablePrintApplication ? "Print Application" : string.Empty;

        options = new(SelectedCard)
        {
            ConfirmLabel = "Submit",
            ExtraButtonLabel = extraButtonLabel
        };

        await canvasRef.ToggleAsync();
    }
    async Task OpenDialogForm()
    {

        bool isAllowToRequest = IsAllowTo(SelectedRequestType switch
        {
            RequestType.Cancel => Permissions.Cancel.Request(),
            RequestType.StopCard => Permissions.StopCard.Request(),
            RequestType.ReportLostOrStolen => Permissions.ReportLostOrStolen.Request(),
            RequestType.ReActivate => Permissions.CardReActivate.Request(),
            _ => ""
        });


        if (!isAllowToRequest)
        {
            await Listen(new() { IsAccessDenied = true, Message = GlobalResources.NotAuthorized });
            return;
        }


        userBranch = await ConfigurationService.GetUserBranch();
        if (cardInfo?.ProductType is ProductTypes.Tayseer)
        {
            await LoadSecondaryCardsForTayseerCard();
        }

        DialogBoxOptions? option = SelectedRequestType switch
        {
            RequestType.Cancel => new(SelectedCard)
            {
                Message = "You’re about to Cancel the selected credit card",
                Title = "Cancel Credit Card",
                ConfirmCallback = EventCallback.Factory.Create<object>(this, CancelRequest),
            },

            RequestType.StopCard => new(SelectedCard)
            {
                Message = "You’re about to Stop the selected credit card",
                Title = "Stop Credit Card",
                ConfirmCallback = EventCallback.Factory.Create<object>(this, ConfirmStopCard),
            },

            RequestType.ReportLostOrStolen => new(SelectedCard)
            {
                Message = "Are you sure you want to report this card on lost / stolen?",
                Title = "Report Lost / Stolen",
                ConfirmCallback = EventCallback.Factory.Create<object>(this, ConfirmReportLostOrStolen),
            },

            RequestType.ReActivate => new(SelectedCard)
            {
                ConfirmCallback = EventCallback.Factory.Create<object>(this, ReActivateCard),
                CancelLabel = "Cancel",
                Message = "Are you sure you want to Re-Activate this card?",
                Title = "Card Re-Activation"
            },
            _ => null
        };

        if (option is null)
            return;

        this.options = option!;
        DialogFormVisible = true;
        await Task.CompletedTask;


    }

    async Task LoadSecondaryCardsForTayseerCard()
    {
        if (SelectedRequestType is RequestType.ReActivate && cardInfo?.ProductType is ProductTypes.Tayseer)
        {
            Notification.Loading("Loading secondary card list..");
            Notification.IsProcessing = true;
            //TODO: get secondary card detail and check secondaryActivationFlag
            //Activation seperate for tayseer
            var primaryCardStatus = cardInfo?.AccountActiviationFlag == "Y";

            var secondaryCardNumber = cardInfo.Parameters?.SecondaryCardNumber;
            var secondaryCardDetailResponse = await CardDetailsService.GetMasterSecondaryCardDetails(SelectedCard!.RequestId!);
            if (!secondaryCardDetailResponse.IsSuccessWithData)
            {
                Notification.Failure("Unable to find master card detail!");
                return;
            }

            var secondaryCardStatus = secondaryCardDetailResponse?.Data?.ActivationFlag != "Activated";
            TayseerCardList =
                [
                    new($"{DisplayCardNumber(SelectedCard?.CardNumberDto)} [{(primaryCardStatus ? "Pending Activation" : "Activated")}]", false), //false mean visa card
                    new($"{DisplayCardNumber(secondaryCardNumber)} [{(secondaryCardStatus ? "Pending Activation" : "Activated")}]", true)//true mean master card
                ];
            Notification.Hide();
        }
    }
    async Task CloseRequestForm()
    {
        if (!canvasRef.IsOpen)
            return;

        await canvasRef.ToggleAsync();
        this.StateHasChanged();
    }
    void SetTitle()
    {
        string action = StandingOrderId is not null ? "Edit " : "";

        Title = $"{action} {SelectedRequestType.GetDescription()}";
    }
    public Task ProcessAction(ActionType actionType, string ReasonForRejection = "")
    {
        throw new NotImplementedException();
    }

    #region Dialog form actions
    #region card activation
    bool IsMasterCard { get; set; }
    List<ListItem<bool>>? TayseerCardList { get; set; }
    public Branch userBranch { get; private set; } = null!;

    async Task ReActivateCard()
    {
        Notification.Processing(new ActionStatus(Title: "ReActivate Card", Message: "Requesting card re-activation is in process..."));

        var activateResponse = await ActivationService.RequestCardReActivation(new()
        {
            BranchId = userBranch.BranchId,
            RequestId = SelectedCard!.RequestId,
            IsMasterCard = cardInfo?.ProductType is ProductTypes.Tayseer ? IsMasterCard : false
        });

        // ResetProcessStatus();
        if (!activateResponse.IsSuccess)
        {
            Notification.Failure(activateResponse.Message);
            return;
        }

        var result = activateResponse.Data![0];

        Notification.Success(result.Message!);
        DialogFormVisible = false;

        if (result.IsActivated == true)
        {
            await ReflectCardStatus.InvokeAsync(new(SelectedCard!.RequestId, (result.IsActivated ?? false) ? CreditCardStatus.Active : cardInfo.CardStatus));
            await ReloadCardData.InvokeAsync(SelectedCard!.RequestId);
        }
    }
    #endregion

    #region ReportLostOrStolen
    async Task ConfirmReportLostOrStolen()
    {
        Notification.Processing(new ActionStatus(Title: "Lost/Stolen Card", Message: "Reporting Lost/Stolen card is in process..."));
        var reportLostOrStolen = await StopAndReportService.ReportLostOrStolen(new() { RequestId = SelectedCard!.RequestId });
        if (!reportLostOrStolen.IsSuccess)
        {
            Notification.Failure(reportLostOrStolen.Message);
            return;
        }

        Notification.Success(reportLostOrStolen.Message);
        DialogFormVisible = false;

        await DownloadAfterSalesEForm(new AfterSalesForm()
        {
            ActionType = RequestType.ReplacementForLost.ToString().ToUpper(),
            RequestId = SelectedCard!.RequestId
        });
        await ReflectCardStatus.InvokeAsync(new(SelectedCard!.RequestId, CreditCardStatus.TemporaryClosed));
        await ReloadCardData.InvokeAsync(SelectedCard!.RequestId);
    }
    #endregion

    #region stop card
    async Task ConfirmStopCard()
    {
        Notification.Processing(new ActionStatus(Title: "Stop Card", Message: "Stopping card is in process..."));
        var stopCardResponse = await StopAndReportService.RequestStopCard(new() { RequestId = SelectedCard!.RequestId });

        if (!stopCardResponse.IsSuccess)
        {
            Notification.Failure(stopCardResponse.Message);
            return;
        }

        Notification.Success(stopCardResponse.Message);
        DialogFormVisible = false;


        await DownloadAfterSalesEForm(new AfterSalesForm()
        {
            ActionType = RequestType.StopCard.ToString().ToUpper(),
            RequestId = SelectedCard!.RequestId
        });
        await ReflectCardStatus.InvokeAsync(new(SelectedCard!.RequestId, CreditCardStatus.TemporaryClosed));
        await ReloadCardData.InvokeAsync(SelectedCard!.RequestId);
    }
    #endregion

    #region cancel request
    async Task CancelRequest()
    {
        Notification.Processing(new ActionStatus(Title: "Cancel Request", Message: "Cancelling credit card request is in process.."));

        var cancelResponse = await RequestService.CancelRequest(SelectedCard!.RequestId);
        // ResetProcessStatus();

        if (!cancelResponse.IsSuccess)
        {
            Notification.Failure(cancelResponse.Message);
            return;
        }

        Notification.Success(cancelResponse.Message);
        DialogFormVisible = false;
        await RemoveFromList.InvokeAsync(SelectedCard!.RequestId);
        await ReloadData.InvokeAsync();
    }


    #endregion

    #endregion


    async Task<bool> IsHavingPendingRequest()
    {

        var cfuActivity = SelectedRequestType switch
        {
            RequestType.ChangeAddress => CFUActivity.CHANGE_BILLING_ADDRESS,
            RequestType.ChangeCardHolderName => CFUActivity.CHANGE_CARDHOLDERNAME,
            RequestType.ChangeLimit => CFUActivity.LIMIT_CHANGE_INCR,
            RequestType.ChangeLinkAccountNumber => CFUActivity.CHANGE_CARD_LINKED_ACCT,
            RequestType.CreditReverse => CFUActivity.CREDIT_REVERSE,
            RequestType.Migration => CFUActivity.MIGRATE_COLLATERAL_MARGIN,
            RequestType.ReActivate => CFUActivity.CARD_RE_ACTIVATION,
            RequestType.ReplacementForDamage => CFUActivity.Replace_On_Damage,
            RequestType.ReplacementForLost => CFUActivity.Replace_On_Lost_Or_Stolen,
            _ => CFUActivity.NoActivity
        };

        if (cfuActivity is CFUActivity.NoActivity)
            return false;

        //do doube check 
        //RequestType.Migration => CFUActivity.MIGRATE_COLLATERAL_DEPOSIT &&CFUActivity.MIGRATE_COLLATERAL_Margin ,

        if (await CheckActivity(cfuActivity))
            return true;

        if (SelectedRequestType is RequestType.Migration)
        {
            cfuActivity = CFUActivity.MIGRATE_COLLATERAL_DEPOSIT;
            return await CheckActivity(cfuActivity);
        }

        return false;

        async Task<bool> CheckActivity(CFUActivity cfuActivity)
        {

            PendingRequests = (await RequestActivityService.SearchActivity(new()
            {
                RequestId = SelectedCard.RequestId,
                CivilId = cardInfo.CivilId,
                CardNumber = cardInfo.CardNumberDto,
                CardNumberDto = cardInfo.CardNumberDto,
                CfuActivityId = (int)cfuActivity,
                RequestActivityStatusId = (int)RequestActivityStatus.Pending,
                IssuanceTypeId = (int)cardInfo.IssuanceType
            }))?.Data?.Select(x => new PendingRequest((RequestActivityStatus)x.RequestActivityStatusId, x.CreationDate ?? default, null));


            if (PendingRequests.AnyWithNull())
            {
                Notification.Info(message: GlobalResources.PendingRequest);
            }

            return PendingRequests.AnyWithNull();
        }

    }

    async Task PrintApplication() => await componentAction.PrintApplication();
    async Task DownloadAfterSalesEForm(AfterSalesForm salesFormData = null)
    {
        Notification.Processing(new ActionStatus() { Title = "AfterSales E-Form", Message = $"Generating E-Form to download" });
        salesFormData.ActionType = SelectedRequestType.ToString();
        salesFormData.RequestId = SelectedCard.RequestId;

        var eFormResponse = await ReportService.GenerateAfterSalesForm(salesFormData);

        if (!eFormResponse.IsSuccess)
        {
            Notification.Failure("Unable to generate AfterSales E-Form, try again later from card list");
            return;
        }

        Notification.Success("Downloaded AfterSales E-Form!");

        var streamData = new MemoryStream(eFormResponse.Data!.FileBytes!);
        using var streamRef = new DotNetStreamReference(stream: streamData);
        await Js.InvokeVoidAsync("downloadFileFromStream", $"{eFormResponse.Data?.FileName}", streamRef);
    }

    public void Dispose()
    {
        Notification.HasOffCanvas = false;
    }
}
