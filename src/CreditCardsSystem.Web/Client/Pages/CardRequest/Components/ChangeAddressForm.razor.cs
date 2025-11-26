using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Interfaces;
using CreditCardsSystem.Domain.Models.Access;
using CreditCardsSystem.Domain.Models.CardIssuance;
using CreditCardsSystem.Domain.Models.CardOperation;
using CreditCardsSystem.Domain.Models.Reports;
using CreditCardsSystem.Domain.Models.Workflow;
using CreditCardsSystem.Utility.Extensions;
using CreditCardsSystem.Web.Client.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace CreditCardsSystem.Web.Client.Pages.CardRequest.Components;

public partial class ChangeAddressForm : IWorkflowMethods
{
    private bool IsAuthorized => IsAllowTo(TaskDetail is not null ? Permissions.ChangeBillingAddress.EnigmaApprove() : Permissions.ChangeBillingAddress.Request());
    [Inject] ILookupAppService LookupService { get; set; } = null!;
    [Inject] IChangeOfAddressAppService ChangeOfAddressAppService { get; set; } = null!;
    [Inject] IAddressAppService AddressService { get; set; } = null!;


    #region variables
    ChangeOfAddressRequest ChangeOfAddressRequest { get; set; } = new();
    bool IsPOBoxEnabled { get; set; } = true;
    bool IsFullAddressEnabled { get; set; } = false;
    bool IsViewOnly => RequestActivity?.RequestActivityStatus != RequestActivityStatus.Pending;

    List<AreaCodesDto> AreaCodes { get; set; } = new();
    BillingAddressModel CurrentBillingAddress { get; set; } = null!;
    BillingAddressModel RequestedBillingAddress { get; set; } = null!;

    bool HasPendingAddress { get; set; }
    record AddressTypeItem(AddressType Value, string Text);
    IEnumerable<AddressTypeItem> AddressTypes { get; set; } = [
                        new(AddressType.POBox, AddressType.POBox.GetDescription()),
        new(AddressType.FullAddress, AddressType.FullAddress.GetDescription())
                        ];
    AddressType SelectedAddressType { get; set; } = AddressType.POBox;
    #endregion

    protected override async Task OnInitializedAsync()
    {
        //Notification.Loading($"Loading data...");

        if (TaskDetail is not null)
            await BindTaskDetail();
        else
            await PrepareRequestForm();

        Notification.Hide();

    }

    async Task BindTaskDetail()
    {
        if (!IsAuthorized)
            await Listen.InvokeAsync(new() { IsAccessDenied = true });


        RequestedBillingAddress = new()
        {
            AreaId = TaskDetail!.Payload.GetValueOrDefault(WorkflowVariables.AreaId)?.ToString(),
            City = TaskDetail!.Payload.GetValueOrDefault(WorkflowVariables.City)?.ToString() ?? "",
            HomePhone = TaskDetail.Payload.GetValueOrDefault(WorkflowVariables.HomePhone)?.ToString().ToLong(),
            Mobile = TaskDetail.Payload.GetValueOrDefault(WorkflowVariables.MobilePhone)?.ToString().ToLong(),
            WorkPhone = TaskDetail.Payload.GetValueOrDefault(WorkflowVariables.WorkPhone)?.ToString().ToLong(),
            PostalCode = TaskDetail.Payload.GetValueOrDefault(WorkflowVariables.ZipCode)?.ToString().ToInt(),
            Street = TaskDetail.Payload.GetValueOrDefault(WorkflowVariables.Street)?.ToString() ?? "",
        };

        if (RequestedBillingAddress.Street.StartsWith("blk", StringComparison.InvariantCultureIgnoreCase))
            SelectedAddressType = AddressType.FullAddress;
        else
            SelectedAddressType = AddressType.POBox;


        RequestedBillingAddress?.splitAddress();

        CurrentBillingAddress = (await AddressService.GetRecentBillingAddress(requestId: RequestActivity.RequestId!))?.Data!;
        CurrentBillingAddress.splitAddress();

    }
    async Task PrepareRequestForm()
    {
        if (!IsAuthorized)
            await Listen.InvokeAsync(new() { IsAccessDenied = true });

        ChangeOfAddressRequest.RequestId = SelectedCard!.RequestId;

        #region Loading Billing Address
        await GetAreaCodes();
        ChangeOfAddressRequest.BillingAddress = (await AddressService.GetRecentBillingAddress(requestId: SelectedCard!.RequestId!))?.Data!;

        if (ChangeOfAddressRequest.BillingAddress.Street.StartsWith("blk", StringComparison.InvariantCultureIgnoreCase))
            SelectedAddressType = AddressType.FullAddress;

        #endregion

        IsPOBoxEnabled = !HasPendingAddress;

        BindFormEditContext(ChangeOfAddressRequest.BillingAddress ?? new());

        ChangeOfAddressRequest.BillingAddress?.splitAddress();

        await OnChangeAddressType();
        await ReadyForAction.InvokeAsync(true);
    }

    public async Task<bool> SubmitRequest(CancellationToken? cancellationToken = default)
    {
        if (!await IsFormValid()) return false;

        await Listen.NotifyStatus(DataStatus.Processing, Title: "Billing Address", Message: $"Requesting billing address change");
        var requestResponse = await ChangeOfAddressAppService.RequestChangeOfAddress(ChangeOfAddressRequest);
        await Listen.NotifyStatus(data: requestResponse);

        return requestResponse.IsSuccess;

      
    }
    public async Task ProcessAction(ActionType actionType, string ReasonForRejection)
    {
        if (TaskDetail is null)
            return;

        var processResponse = await ChangeOfAddressAppService.ProcessChangeOfAddressRequest(new()
        {
            ReasonForRejection = ReasonForRejection,
            ActionType = actionType,
            RequestActivityId = RequestActivity!.RequestActivityId,
            TaskId = TaskDetail?.Id,
            WorkFlowInstanceId = TaskDetail?.InstanceId
        });

        await Listen.NotifyStatus(data: processResponse);
    }

    public async Task PrintApplication()
    {
        if (ChangeOfAddressRequest.BillingAddress is not null)
        {
            await DownloadAfterSalesEForm.InvokeAsync(new AfterSalesForm()
            {
                HolderName = SelectedCard?.HolderEmbossName,
                Address = $"POBox:{ChangeOfAddressRequest.BillingAddress.PostOfficeBoxNumber} City:{ChangeOfAddressRequest.BillingAddress.City} Post Code:{ChangeOfAddressRequest.BillingAddress.PostalCode} Street:{ChangeOfAddressRequest.BillingAddress.Street}",
                MobileNo = ChangeOfAddressRequest.BillingAddress.Mobile.ToString(),
                Tel = $"Home Phone:{ChangeOfAddressRequest.BillingAddress.HomePhone} Work Phone:{ChangeOfAddressRequest.BillingAddress.WorkPhone}"
            });
        }
    }


    async Task OnChangeAddressType()
    {
        switch (SelectedAddressType)
        {
            case AddressType.POBox:
                IsPOBoxEnabled = !HasPendingAddress && true;
                IsFullAddressEnabled = false;
                await OnChangePOBox();
                break;
            case AddressType.FullAddress:
                IsPOBoxEnabled = false;
                IsFullAddressEnabled = !HasPendingAddress && true;

                if (ChangeOfAddressRequest.BillingAddress is not null)
                {
                    ChangeOfAddressRequest.BillingAddress.PostOfficeBoxNumber = 0;
                    await OnChangeFullAddress();
                }

                break;
        }
    }
    async Task OnChangePOBox()
    {
        ChangeOfAddressRequest.BillingAddress.Street = "P.O.BOX: " + ChangeOfAddressRequest.BillingAddress.PostOfficeBoxNumber;
        await Task.CompletedTask;
    }
    async Task OnChangeFullAddress()
    {
        //clearing error message on street due to modify
        formMessageStore?.Clear(formEditContext.Field(nameof(ChangeOfAddressRequest.BillingAddress.Street)));
        ChangeOfAddressRequest.BillingAddress.Street = "Blk " + ChangeOfAddressRequest.BillingAddress.Block + " st " + ChangeOfAddressRequest.BillingAddress.StreetNo_NM + " Jda " + ChangeOfAddressRequest.BillingAddress.Jada + " House " + ChangeOfAddressRequest.BillingAddress.House;

        await Task.CompletedTask;
    }
    async Task GetAreaCodes()
    {
        AreaCodes = (await LookupService.GetAreaCodes())?.Data!;
    }
    public Task Cancel()
    {
        throw new NotImplementedException();
    }

}
