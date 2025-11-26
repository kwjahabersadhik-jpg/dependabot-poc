using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Interfaces;
using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Web.Client.Components;
using Kfh.Aurora.Utilities;
using Microsoft.AspNetCore.Components;

namespace CreditCardsSystem.Web.Client.Pages.CardRequest.Components;

public partial class MoreDetail : IWorkflowMethods
{
    [Inject] ICardDetailsAppService CardDetailsAppService { get; set; } = default!;
    [Inject] IActivationAppService ActivationAppService { get; set; } = default!;



    private async Task LoadMasterCardStatus()
    {
        if (SelectedCard?.SecondaryCardNoDto != null)
        {
            ChangeLoadingCardStatus(true);
            var response = await CardDetailsAppService.GetMasterSecondaryCardDetails(SelectedCard.RequestId);
            if (response.IsSuccess)
            {
                SelectedCard.MasterCardStatus = response.Data?.ActivationFlag;
                SelectedCard.MasterCardRequestId = response.Data?.RequestId;
                StateHasChanged();
            }
            ChangeLoadingCardStatus(false);
        }
    }
    public async Task<bool> SecondaryMasterCardActivation()
    {

        await Listen.NotifyStatus($"Secondary card (master card) activation is in process. card# {DisplayCardNumber(SelectedCard?.SecondaryCardNoDto)}");
        var response = await ActivationAppService.ActivateMultipleCards(new() { CivilId = "0", CardNumbers = [SelectedCard!.SecondaryCardNoDto] });
        var status = response.IsSuccess && (response?.Data?[0]?.IsActivated ?? false);
        await Listen.NotifyStatus(data: response);
        return status;
    }
    private bool IsLoadingCardStatus { get; set; } = false;


    private void ChangeLoadingCardStatus(bool isLoading)
    {
        IsLoadingCardStatus = isLoading;
        StateHasChanged();
    }
    private async Task NavigateToParentCardDetailPage(string? primaryCardCivilId, string? primaryCardRequestId)
    {

        UpdateAppState(primaryCardCivilId!, primaryCardRequestId!);
        var queryParams = new Dictionary<string, string>
        {
            ["civilId"] = CurrentState.CurrentCivilId?.Encode()!
        };

        await Listen.InvokeAsync(new ActionStatus() { IsSuccess = true, Message = "Navigating to Parent customer view page!" });
        NavigateTo("/customer-view", queryParams);
        //await AdvancedSearchRef.SwitchProfile(primaryCardCivilId!);
        await Task.CompletedTask;

    }
    private void UpdateAppState(string? primaryCardCivilId, string? primaryCardRequestId)
    {
        //if the navigation flow from supplementary list
        if (CurrentState is null) return;

        CurrentState.CurrentCivilId = primaryCardCivilId ?? CurrentState.PrimaryCivilId;
        CurrentState.PrimaryCivilId = CurrentState.CurrentCivilId;
        CurrentState.IsFromSpplementay = false;

        if (!string.IsNullOrEmpty(primaryCardRequestId) && decimal.TryParse(primaryCardRequestId, out decimal primaryCardRequestIdOut))
            CurrentState.PrimaryRequestId = primaryCardRequestIdOut;
    }
    public Task PrintApplication()
    {
        throw new NotImplementedException();
    }

    public Task ProcessAction(ActionType actionType, string ReasonForRejection = "")
    {
        throw new NotImplementedException();
    }

    public async Task<bool> SubmitRequest(CancellationToken? cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task Cancel()
    {
        throw new NotImplementedException();
    }
}
