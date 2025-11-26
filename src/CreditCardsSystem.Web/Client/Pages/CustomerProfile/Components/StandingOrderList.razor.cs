using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Models.Access;
using CreditCardsSystem.Domain.Models.StandingOrder;
using CreditCardsSystem.Web.Client.Components;
using CreditCardsSystem.Web.Client.Pages.CardRequest;
using Telerik.Blazor.Components;

namespace CreditCardsSystem.Web.Client.Pages.CustomerProfile.Components;

public partial class StandingOrderList
{

    RequestMakerPage? cardRequestFormRef { get; set; } = default!;
    List<StandingOrderDto>? StandingOrders => State?.StandingOrders.Data;
    List<StandingOrderDto>? FilteredStandingOrders { get; set; } = default;
    public TelerikGrid<StandingOrderDto>? StandingOrderGridRef { get; set; }

 

    private bool ShowDeleteDialog { get; set; } = false;
    public StandingOrderDto? SelectedStandingOrder { get; set; } = null!;


    public DateTime _zeroTime = new DateTime(1, 1, 1);

    protected override async Task OnParametersSetAsync()
    {
        if (State?.StandingOrders?.Status == DataStatus.Success)
        {
            await OnChangeTransferType(EStandingOrderTransferType.All.ToString());
        }
    }
    async Task OnChangeAction(int? standingOrderId)
    {
        if (CurrentState.GenericCustomerProfile.IsPendingBioMetric)
        {
            Notification.Failure(message: GlobalResources.BioMetricRestriction);
            return;
        }

        cardRequestFormRef!.SelectedCard = SelectedStandingOrder is not null ? new() { RequestId = SelectedStandingOrder.StandingOrderId } : null;
        cardRequestFormRef!.StandingOrderId = standingOrderId;
        cardRequestFormRef.EnableBeneficiarySelection = true;
        await cardRequestFormRef!.OnChangeAction(RequestType.StandingOrder);
    }

    public EStandingOrderTransferType selectedTransferType { get; set; }
    private async Task OnChangeTransferType(string transferType)
    {

        if (State.StandingOrders.Status != DataStatus.Success)
            return;

        var filter = StandingOrders?.ToList();

        _ = Enum.TryParse(transferType, out EStandingOrderTransferType _StandingOrderTransferType);

        selectedTransferType = _StandingOrderTransferType;

        if (_StandingOrderTransferType is not EStandingOrderTransferType.All)
        {
            filter = filter?.Where(x => x.TransferType != null && x.TransferType == _StandingOrderTransferType.ToString()).ToList();
        }

        FilteredStandingOrders = filter;

        await Task.CompletedTask;
    }

    protected override async Task OnInitializedAsync()
    {
        await Task.CompletedTask;

    }

    private async Task ActionSelectionChanged(ListItem<string> selectedAction)
    {
        if (selectedAction?.Text?.ToUpper() == "EDIT")
        {
            await OnChangeAction(SelectedStandingOrder?.StandingOrderId);
        }
        else if (selectedAction?.Text?.ToUpper() == "DELETE")
        {
            if (AuthManager.HasPermission(Permissions.StandingOrder.Delete()))
                ShowDeleteDialog = true;
            else
                Notification.Failure("You do not have permission!");
        }

        await Task.CompletedTask;
    }



    private async Task ConfirmDeleteStandingOrder()
    {
        ShowDeleteDialog = false;


        var deleteStandingOrderRequest = new StandingOrderRequest()
        {
            StandingOrderId = SelectedStandingOrder.StandingOrderId,
            DebitAccountNumber = SelectedStandingOrder.SourceAccount,
            ChargeAccountNumber = "",
            BranchNumber = 1//TODO : Should take from current user
        };

        if (deleteStandingOrderRequest is null)
        {
            Notification.Hide();
            return;
        }

        Notification.Processing(new ActionStatus(Title:"Closing Standing Order", Message: "Closing standing order is in process.."));
        var response = await _standingOrderAppService.CloseStandingOrders(deleteStandingOrderRequest);

        if (response.IsSuccess)
        {

            Notification?.Success(response.Message?? "");
            await GetStandingOrders();
        }
        else
        {
            Notification?.Failure(response.Message);
        }
    }


    private async Task GetStandingOrders()
    {
        try
        {
            if (!IsAllowTo(Permissions.StandingOrder.View()))
                return;


            State.StandingOrders.Loading();
            var response = await _standingOrderAppService.GetAllStandingOrders(CurrentState?.CurrentCivilId ?? "");
            if (response.IsSuccess)
            {
                State.StandingOrders.SetData(response!.Data!);
                await OnChangeTransferType(selectedTransferType.ToString());
                StandingOrderGridRef?.Rebind();

            }
            else
                State.StandingOrders.Error(new Exception(response.Message));
        }
        catch (Exception ex)
        {
            State.StandingOrders.Error(ex);
        }
        finally
        {
            StateHasChanged();

        }
    }









}
