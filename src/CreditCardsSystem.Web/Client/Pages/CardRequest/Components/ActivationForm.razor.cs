using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Interfaces;
using CreditCardsSystem.Domain.Models.Access;
using CreditCardsSystem.Utility.Extensions;
using CreditCardsSystem.Web.Client.Components;
using Microsoft.AspNetCore.Components;

namespace CreditCardsSystem.Web.Client.Pages.CardRequest.Components;

public partial class ActivationForm : IWorkflowMethods
{
    private bool IsAuthorized => IsAllowTo(TaskDetail is not null ? Permissions.CardActivation.EnigmaApprove() : Permissions.CardActivation.Request());
    [Inject] IActivationAppService ActivationAppService { get; set; } = null!;
    [Inject] ICardDetailsAppService CardDetailsAppService { get; set; } = null!;
    decimal? CardRequestId { get; set; }
    List<ListItem<decimal>>? TayseerCardList { get; set; }

    protected override async Task OnInitializedAsync()
    {
        //Notification.Loading($"Loading data...");
        await PrepareRequestForm();
        Notification.Hide();
        await ReadyForAction.InvokeAsync(true);
    }

    async Task PrepareRequestForm()
    {
        if (!IsAuthorized)
            await Listen.InvokeAsync(new() { IsAccessDenied = true });

        if (SelectedCard?.ProductType is ProductTypes.Tayseer)
        {
            //TODO: get secondary card detail and check secondaryActivationFlag
            //Activation seperate for tayseer
            var primaryCardStatus = SelectedCard.AccountActiviationFlag == "Y";

            var secondaryCardNumber = SelectedCard.Parameters?.SecondaryCardNumber;
            var secondaryCardDetailResponse = await CardDetailsAppService.GetMasterSecondaryCardDetails(SelectedCard.RequestId!);
            if (!secondaryCardDetailResponse.IsSuccessWithData)
            {
                Notification.Failure("Unable to find master card detail!");
                return;
            }

            var secondaryCardStatus = secondaryCardDetailResponse?.Data?.ActivationFlag != "Activated";

            TayseerCardList =
        [
            new($"{DisplayCardNumber(SelectedCard.CardNumberDto)} [{(primaryCardStatus ? "Pending Activation" : "Activated")}]", SelectedCard!.RequestId),
            new($"{DisplayCardNumber(secondaryCardNumber)} [{(secondaryCardStatus ? "Pending Activation" : "Activated")}]", secondaryCardDetailResponse!.Data!.RequestId!)
        ];

            if (CardRequestId == null && TayseerCardList.AnyWithNull())
                CardRequestId = SelectedCard!.RequestId;
        }
    }
    public async Task<bool> SubmitRequest(CancellationToken? cancellationToken = default)
    {
        CardRequestId = TayseerCardList.AnyWithNull() ? CardRequestId : SelectedCard!.RequestId;
        
        await Listen.NotifyStatus($"Card activation is in process. card# {DisplayCardNumber(SelectedCard?.CardNumberDto)}");
        var response = await ActivationAppService.ActivateSingleCard(new() { RequestId = CardRequestId!.Value });
        var status = response.IsSuccess && (response?.Data?[0]?.IsActivated ?? false);
        await Listen.NotifyStatus(data: response);
        return status;
    }

    public async Task PrintApplication() => await DownloadAfterSalesEForm.InvokeAsync(new() { HolderName = SelectedCard.HolderEmbossName });

    public Task ProcessAction(ActionType actionType, string ReasonForRejection) => throw new NotImplementedException();
    public Task Cancel() => throw new NotImplementedException();
}
