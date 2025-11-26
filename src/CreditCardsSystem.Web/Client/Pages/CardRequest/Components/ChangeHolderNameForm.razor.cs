using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Interfaces;
using CreditCardsSystem.Domain.Models.Access;
using CreditCardsSystem.Domain.Models.CardOperation;
using CreditCardsSystem.Domain.Models.Reports;
using CreditCardsSystem.Web.Client.Components;
using Microsoft.AspNetCore.Components;

namespace CreditCardsSystem.Web.Client.Pages.CardRequest.Components;

public partial class ChangeHolderNameForm : IWorkflowMethods
{
    [Inject] IChangeOfAddressAppService changeOfAddressAppService { get; set; } = null!;
    [Inject] IAddressAppService AddressService { get; set; } = null!;
    private bool IsAuthorized => IsAllowTo(TaskDetail is not null ? Permissions.ChangeHolderName.EnigmaApprove() : Permissions.ChangeHolderName.Request());

    [Parameter]
    public ChangeHolderNameRequest Model { get; set; } = new();

    [Parameter]
    public bool HasPendingName { get; set; }

    async Task PrepareRequestForm()
    {
        if (!IsAuthorized)
            await Listen.InvokeAsync(new() { IsAccessDenied = true });

        Model = new()
        {
            OldCardHolderName = SelectedCard?.HolderEmbossName ?? "",
            NewCardHolderName = SelectedCard?.HolderEmbossName ?? "",
            RequestId = SelectedCard!.RequestId
        };
        await ReadyForAction.InvokeAsync(true);

        BindFormEditContext(Model);
    }

    public async Task<bool> SubmitRequest(CancellationToken? cancellationToken = default)
    {
        if (!await IsValidForm())
        {
            return false;
        }

        await Listen.NotifyStatus(DataStatus.Processing, Title: "Card holder name", Message: $"Requesting card holder name change");

        var requestResponse = await changeOfAddressAppService.RequestChangeCardHolderName(Model);
        await Listen.NotifyStatus(data: requestResponse);


        return requestResponse.IsSuccess;

        async Task<bool> IsValidForm()
        {
            if (!await IsFormValid()) return false;

            if (string.IsNullOrEmpty(SelectedCard.HolderEmbossName))
                return await Task.FromResult(SelectedCard.HolderEmbossName != Model.NewCardHolderName);

            return await Task.FromResult(!string.IsNullOrEmpty(Model.NewCardHolderName));
        }
    }


    async Task BindTaskDetail()
    {
        if (!IsAuthorized)
            await Listen.InvokeAsync(new() { IsAccessDenied = true });

        Model = new();
    }

    public async Task ProcessAction(ActionType actionType, string ReasonForRejection)
    {

        if (TaskDetail is not null)
        {
            var processResponse = await changeOfAddressAppService.ProcessChangeOfAddressRequest(new()
            {
                ReasonForRejection = ReasonForRejection,
                ActionType = actionType,
                RequestActivityId = RequestActivity!.RequestActivityId,
                TaskId = TaskDetail?.Id,
                WorkFlowInstanceId = TaskDetail?.InstanceId
            });

            await Listen.NotifyStatus(data: processResponse);
        }
    }




    protected override async Task OnInitializedAsync()
    {
        //Notification.Loading($"Loading data...");

        if (TaskDetail is not null)
            await BindTaskDetail();
        else
            await PrepareRequestForm();

        Notification.Hide();

    }



    public async Task PrintApplication()
    {

        var billingAddress = (await AddressService.GetRecentBillingAddress(requestId: SelectedCard!.RequestId!))?.Data!;
        await DownloadAfterSalesEForm.InvokeAsync(new AfterSalesForm()
        {
            HolderName = Model.NewCardHolderName,
            Address = $"POBox:{billingAddress.PostOfficeBoxNumber} City:{billingAddress.City} Post Code:{billingAddress.PostalCode} Street:{billingAddress.Street}",
            MobileNo = billingAddress.Mobile.ToString(),
            Tel = $"Home Phone:{billingAddress.HomePhone} Work Phone:{billingAddress.WorkPhone}"
        });
    }



    public Task Cancel()
    {
        throw new NotImplementedException();
    }
}
