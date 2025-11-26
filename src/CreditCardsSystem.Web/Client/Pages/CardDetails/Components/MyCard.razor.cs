using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Interfaces;
using CreditCardsSystem.Domain.Models.Card;
using CreditCardsSystem.Utility.Extensions;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace CreditCardsSystem.Web.Client.Pages.CardDetails.Components;


public partial class MyCard
{

    [Parameter]
    public DataItem<CardDetailsResponse> Card { get; set; } = new();
    public CardDetailsResponse Data => Card.Data ?? new();

    [Parameter]
    public EventCallback ReloadCardInfo { get; set; }
    [Inject] private ICardDetailsAppService CardDetailAppService { get; set; } = default!;
    [Inject] private NavigationManager? NavigationManager { get; set; }

    [Inject] private IJSRuntime? JsRuntime { get; set; }


    private bool IsLoadedCardStatus { get; set; } = true;
    private bool IsLoadingCardStatus { get; set; } = false;
    private bool IsSubDataLoaded => Card.SubStatus != DataStatus.Loading;
    private string LoadingCardStatusButtonText { get; set; }
    private CFUActivity? SelectedActivity { get; set; }

    private List<RequestType> LoadMenuForCard()
    {
        var eligibleRequestTypes = new List<RequestType>(){
            new() { Name=CFUActivity.CHANGE_CARD_STATUS,Description=CFUActivity.CHANGE_CARD_STATUS.GetDescription() },
            new() { Name=CFUActivity.Supplementary_Card,Description=CFUActivity.Supplementary_Card.GetDescription()},
        };

        if (IsEligibleForCardPayment())
            eligibleRequestTypes.Add(new() { Name = CFUActivity.Card_Payment, Description = "Card Payment" });

        //BCD-996 
        if (IsEligibleForStandingOrder())
            eligibleRequestTypes.Add(new() { Name = CFUActivity.Standing_Order, Description = "Standing Order" });

        return eligibleRequestTypes;
    }

    private bool IsEligibleForCardPayment() => Data.IsCorporateCard == false && (Data.CardNumberDto != null &&
            Data.CardStatus is not (CreditCardStatus.Pending or CreditCardStatus.PendingForCreditCheckingReview or CreditCardStatus.Lost or CreditCardStatus.CreditCheckingReviewed or CreditCardStatus.Approved));

    private bool IsEligibleForStandingOrder() => !Data.IsCorporateCard;
    public class RequestType
    {
        public CFUActivity Name { get; set; }
        public string Description { get; set; } = string.Empty;
    }


    private void OnSuccess(UpdateCardRequestState updateState)
    {
        SelectedActivity = null;

        if (updateState.DoReload)
            ReloadCardInfo.InvokeAsync();

        Notification?.Success(updateState.Message);

    }
    private void OnCancel()
    {
        SelectedActivity = null;
    }
    private async Task NavigateToParentCardDetailPage(string? primaryCardCivilId, string? primaryCardRequestId)
    {

        UpdateAppState(primaryCardCivilId!, primaryCardRequestId!);
        await Task.CompletedTask;

        //await AdvancedSearchRef.SwitchProfile(primaryCardCivilId!);

        //var queryString = new Dictionary<string, string>() {
        //      { "CivilId", primaryCardCivilId! },
        //    { "RequestId", primaryCardRequestId! }
        //};


        //NavigationManager!.NavigateTo(QueryHelpers.AddQueryString("/card-detail", queryString));
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

    private async Task LoadMasterCardStatus()
    {
        if (Data.SecondaryCardNo != null)
        {
            ChangeLoadingCardStatus(isLoaded: false);
            var response = await CardDetailAppService.GetMasterSecondaryCardDetails(Data.RequestId);
            if (response.IsSuccess)
            {
                Data.MasterCardStatus = response.Data.ActivationFlag;
                StateHasChanged();
            }

            ChangeLoadingCardStatus(isLoaded: true);
        }
    }

    private void ChangeLoadingCardStatus(bool isLoaded)
    {
        if (!isLoaded)
        {
            LoadingCardStatusButtonText = " ";
            IsLoadingCardStatus = true;
        }
        else
        {
            IsLoadedCardStatus = false;
            IsLoadingCardStatus = !IsLoadedCardStatus;
            LoadingCardStatusButtonText = " ";
        }
        StateHasChanged();
    }

    private async Task GoToRequestHeader()
    {
        await JsRuntime!.InvokeVoidAsync("goToRequestHeader");
    }
}
