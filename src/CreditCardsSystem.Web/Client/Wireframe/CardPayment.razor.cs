using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Models.Card;
using CreditCardsSystem.Domain.Models.CardIssuance;
using CreditCardsSystem.Domain.Models.CardPayment;
using CreditCardsSystem.Domain.Models.StandingOrder;
using CreditCardsSystem.Domain.Models.SupplementaryCard;
using CreditCardsSystem.Utility.Extensions;
using CreditCardsSystem.Web.Client.Components;
using CreditCardsSystem.Web.Client.Pages.CardDetails;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace CreditCardsSystem.Web.Client.Wireframe;

public partial class CardPayment
{
    #region Properties
    [Parameter] public EventCallback<UpdateCardRequestState> OnSuccess { get; set; }
    [Parameter] public EventCallback<UpdateCardRequestState> OnCancel { get; set; }
    [CascadingParameter(Name = "CardDetail")] public CardDetailsResponse? CardDetail { get; set; }


    private const string DefaultImage = "/dist/KFHCreditCards/credit/DefaultImage-light.png";
    private List<AccountDetailsDto> DebitAccounts { get; set; } = null!;
    private AccountDetailsDto SelectedDebitAccount { get; set; } = null!;
    private EditContext EditContext { get; set; } = null!;
    private CardPaymentRequest Model { get; set; } = new();
    private ValidationMessageStore? messageStore;
    private CardCurrencyDto? CardCurrency { get; set; } = null!;
    private ValidateCurrencyResponse CurrencyTransferData { get; set; } = null!;
    private ListItem[] beneficiaryTypes { get; set; } = default!;
    private bool IsDataLoaded { get; set; } = false;
    private bool IsValidData { get; set; } = true;
    private bool IsForeignCurrencyCard { get; set; } = false;
    private int SelectedBeneficiaryType = (int)BeneficiaryTypes.OwnedCard;

    #endregion

    #region Methods
    protected override async Task OnInitializedAsync()
    {
        BindDropDownItems();
        List<Task> tasks = new()
    {
        LoadDebitAccounts(),
        LoadOwnedCreditCards(),
        LoadPayeeCards()
    };
        await Task.WhenAll(tasks);

        await BindFormModel();
        BindFormEditContext();
    }

    private async Task LoadDebitAccounts(string accountNumber = "", string currency = "")
    {
        var response = string.IsNullOrEmpty(accountNumber) ?
        await AccountAppService.GetDebitAccounts(CurrentState?.CurrentCivilId!) :
        await AccountAppService.GetDebitAccountsByAccountNumber(accountNumber);

        if (response.IsSuccess)
        {
            DebitAccounts = string.IsNullOrEmpty(currency) ? response!.Data! : response!.Data!.Where(x => x.Currency == currency).ToList();
            StateHasChanged();
        }
    }

    private async Task SubmitCardPayment()
    {
        await BindFormModel();

        if (!EditContext!.Validate()) return;

        Notification.Loading("Payment is in process..");

        var response = await CardPaymentAppService.ExecuteCardPayment(Model);

        if (response.IsSuccess)
        {
            await OnSuccess.InvokeAsync(new() { DoReload = true, Message = response.Data?.Message! });
        }
        else
        {
            Notification?.Show(AlertType.Error, response.Message);

            if (EditContext == null) return;

            messageStore?.Clear();

            if (response.ValidationErrors != null)
                foreach (var error in response.ValidationErrors.Where(x => x.Property != null))
                {
                    messageStore?.Add(EditContext.Field(error.Property!), error.Error);
                }

            EditContext.NotifyValidationStateChanged();
        }
    }

    private async Task OnChangeAmountHandler(bool isTransferAmount = false)
    {
        if (!IsForeignCurrencyCard) return;

        CardCurrency = await CurrencyAppService.GetCardCurrencyByRequestId(Model.RequestId);
        decimal amount = isTransferAmount ? Model.ForeignAmount : Model.Amount;

        if (CardCurrency == null || amount <= 0)
            return;

        var currencyRateResponse = await CurrencyAppService.ValidateCurrencyRate(new()
        {
            CivilId = CurrentState?.CurrentCivilId!,
            SourceCurrencyCode = isTransferAmount ? CardCurrency?.CurrencyIsoCode! : ConfigurationBase.KuwaitCurrency,
            ForeignCurrencyCode = isTransferAmount ? ConfigurationBase.KuwaitCurrency : CardCurrency?.CurrencyIsoCode!,
            SourceAmount = isTransferAmount ? amount : -1,
            DestinationAmount = isTransferAmount ? -1 : amount,
        });

        if (!currencyRateResponse.IsSuccess)
        {
            Notification.Success(currencyRateResponse.Message);
            return;
        }

        CurrencyTransferData = currencyRateResponse?.Data!;


        Model.Amount = isTransferAmount ? CurrencyTransferData?.DestAmount ?? 0 : CurrencyTransferData?.SrcAmount ?? 0;
        Model.ForeignAmount = isTransferAmount ? CurrencyTransferData?.SrcAmount ?? 0 : CurrencyTransferData?.DestAmount ?? 0;

        

    }

    private async Task OnChangeDebitAccountNumber()
    {
        if (SelectedDebitAccount?.Acct == Model.DebitAccountNumber) return;

        if (DebitAccounts is not null)
            SelectedDebitAccount = DebitAccounts!.FirstOrDefault(x => x.Acct == Model.DebitAccountNumber)!;

        await OnFormModelChanged();
    }

    private async Task OnFormModelChanged()
    {
        if (Model.RequestId == default) return;

        ResetAmounts();

        if (CardCurrency is null || (CardCurrency is not null && CardCurrency.RequestId != CardDetail!.RequestId))
            CardCurrency = (await CurrencyAppService.GetCardCurrencyByRequestId(Model.RequestId)) ?? new();

        bool isSelectedCardIsSupplementaryCard = SelectedBeneficiaryType == (int)BeneficiaryTypes.SupplementaryCard;
        bool isOwnedCard = !isSelectedCardIsSupplementaryCard;
        bool isPayingSupplementaryCardByForeignCurrencyDebitAccount = isSelectedCardIsSupplementaryCard && SelectedDebitAccount?.Currency != ConfigurationBase.KuwaitCurrency;
        IsValidData = !isPayingSupplementaryCardByForeignCurrencyDebitAccount;
        IsForeignCurrencyCard = CardCurrency!.IsForeignCurrency;

        if (isPayingSupplementaryCardByForeignCurrencyDebitAccount)
        {
            Notification.Failure($"Payment from {SelectedDebitAccount?.Currency} accounts is not allowed for supplementary card");
            return;
        }

        bool isSameCard = CardCurrency.RequestId == CardDetail!.RequestId;
        if (isOwnedCard && !isSameCard)
        {
            string cardDebitAccountNumber = CardDetail is null ? SelectedOwnedCard?.DebitAccountNumber! : CardDetail!.BankAccountNumber!;

            string debitAccountNumberIfCorporateOwnedCard = (CardCurrency!.IsCorporateCard && isOwnedCard) ? cardDebitAccountNumber : "";

            string validDebitAccountCurrency = (CardCurrency!.IsForeignCurrency) ? ConfigurationBase.KuwaitCurrency : CardCurrency!.CurrencyIsoCode;

            await LoadDebitAccounts(debitAccountNumberIfCorporateOwnedCard, validDebitAccountCurrency);
        }
    }

    private async Task BindFormModel()
    {
        IsDataLoaded = true;
        Model.CivilId = CurrentState?.CurrentCivilId!;
        Model.Currency = SelectedDebitAccount?.Currency ?? "";

        if (CardDetail != null)
        {
            Model.BeneficiaryCardNumber = CardDetail.CardNumberDto;
            Model.RequestId = CardDetail.RequestId;
            Model.BranchNumber = CardDetail.BranchId;
            SelectedBeneficiaryType = CardDetail.IsPrimaryCard & !CardDetail.IsSupplementaryCard ? (int)BeneficiaryTypes.OwnedCard : (int)BeneficiaryTypes.SupplementaryCard;

            if (CardCurrency is null || (CardCurrency is not null && CardCurrency.RequestId != CardDetail.RequestId))
                CardCurrency = (await CurrencyAppService.GetCardCurrencyByRequestId(Model.RequestId)) ?? new();

            IsForeignCurrencyCard = CardCurrency?.IsForeignCurrency ?? false;
        }
    }

    private void BindFormEditContext()
    {
        EditContext = new EditContext(Model);
        messageStore = new(EditContext);
        EditContext.OnValidationRequested += (s, e) => messageStore?.Clear();
        EditContext.OnFieldChanged += (s, e) => messageStore?.Clear(e.FieldIdentifier);
    }

    private void UnBindFormEditContext()
    {
        if (EditContext == null) return;

        EditContext.OnValidationRequested -= (s, e) => messageStore?.Clear();
        EditContext.OnFieldChanged -= (s, e) => messageStore?.Clear(e.FieldIdentifier);
    }

    private async void OnCancelHandler()
    {
        await OnCancel.InvokeAsync();
    }

    private void ResetAmounts()
    {
        Model.Amount = 0;
        Model.ForeignAmount = 0;
    }

    public void Dispose()
    {
        UnBindFormEditContext();
    }

    #region With Card Selection Iption
    private List<OwnedCreditCardsResponse> OwnedCards { get; set; } = null!;
    private List<SupplementaryCardDetail> SupplementaryCards { get; set; } = null!;
    private OwnedCreditCardsResponse? SelectedOwnedCard { get; set; }
    private SupplementaryCardDetail? SelectedSupplementaryCard { get; set; }
    private string? SelectedOwnedCardNumber { get; set; }
    private string? SelectedSupplementaryCardNumber { get; set; }
    private void BindDropDownItems()
    {
        if (CardDetail != null) return;
        beneficiaryTypes = Enum.GetValues(typeof(BeneficiaryTypes)).Cast<BeneficiaryTypes>()
        .Select(x => new ListItem()
        {
            Text = x.GetDescription(),
            Value = (int)x
        }).ToArray();
    }
    private async Task LoadOwnedCreditCards()
    {
        if (CardDetail != null) return;
        var response = await CardPaymentAppService.GetOwnedCreditCards(CurrentState?.CurrentCivilId!);
        if (response.IsSuccess)
        {
            OwnedCards = response.Data?.ToList() ?? new();
            StateHasChanged();
        }
    }
    private async Task LoadPayeeCards()
    {
        if (CardDetail != null) return;

        var response = await CardDetailsAppService.GetSupplementaryCardsByCivilId(CurrentState?.CurrentCivilId!);
        if (response.IsSuccess)
        {
            response.Data?.ForEach(x => x.CardNumberDto = x.CardNumberDto ?? x.RequestId?.ToString() ?? "");
            SupplementaryCards = response.Data ?? new();
            StateHasChanged();
        }
    }
    private async Task LoadExternalData()
    {
        bool isSelectedCardIsOwnedCard = SelectedBeneficiaryType == (int)BeneficiaryTypes.OwnedCard;
        decimal? requestId = isSelectedCardIsOwnedCard ? SelectedOwnedCard?.RequestId : SelectedSupplementaryCard?.SourceRequestId;

        var response = await CardDetailsAppService.GetCardInfo(requestId ?? 0, includeCardBalance: true);
        decimal dueAmount = 0;
        if (response.IsSuccess && isSelectedCardIsOwnedCard && SelectedOwnedCard != null)
        {
            SelectedOwnedCard.ExternalStatus = response!.Data!.ExternalStatus;
            SelectedOwnedCard.DueAmount = dueAmount = response!.Data!.ApproveLimit - response!.Data!.AvailableLimit;
        }

        if (response.IsSuccess && !isSelectedCardIsOwnedCard && SelectedSupplementaryCard != null)
        {
            SelectedSupplementaryCard.ExternalStatus = response!.Data!.ExternalStatus;
        }

        if (dueAmount == 0 && response.Data?.ExternalStatus == ConfigurationBase.Status_CancelOrClose)
            IsValidData = false;

        StateHasChanged();
    }

    private async Task OnChangeOwnedCardNumber()
    {
        if (Model.BeneficiaryCardNumber == SelectedOwnedCardNumber) return;

        Model.BeneficiaryCardNumber = SelectedOwnedCardNumber ?? "";

        SelectedOwnedCard = OwnedCards.FirstOrDefault(x => x.CardNumberDto == SelectedOwnedCardNumber);
        if (SelectedOwnedCard == null) return;

        Model.RequestId = SelectedOwnedCard.RequestId;
        await OnFormModelChanged();
    }
    private async Task OnChangeSupplementaryCardNumber()
    {
        if (Model.BeneficiaryCardNumber == SelectedSupplementaryCardNumber) return;

        Model.BeneficiaryCardNumber = SelectedSupplementaryCardNumber ?? "";

        SelectedSupplementaryCard = SupplementaryCards.FirstOrDefault(x => x.CardNumberDto == SelectedSupplementaryCardNumber);
        if (SelectedSupplementaryCard == null) return;

        Model.RequestId = (decimal)SelectedSupplementaryCard.SourceRequestId;
        await OnFormModelChanged();
    }
    #endregion

    #endregion
}
