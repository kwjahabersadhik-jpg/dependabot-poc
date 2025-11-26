using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Interfaces;
using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Models.Access;
using CreditCardsSystem.Domain.Models.CardIssuance;
using CreditCardsSystem.Domain.Models.CardPayment;
using CreditCardsSystem.Domain.Models.StandingOrder;
using CreditCardsSystem.Domain.Models.SupplementaryCard;
using CreditCardsSystem.Utility.Crypto;
using CreditCardsSystem.Utility.Extensions;
using CreditCardsSystem.Web.Client.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace CreditCardsSystem.Web.Client.Pages.CardRequest.Components
{
    public partial class PaymentForm : IWorkflowMethods
    {
        private bool IsAuthorized => IsAllowTo(Permissions.CardPayment.Create());


        [Inject] IAccountsAppService AccountAppService { get; set; } = null!;
        [Inject] ICardDetailsAppService CardDetailsAppService { get; set; } = null!;
        [Inject] ICardPaymentAppService CardPaymentAppService { get; set; } = null!;
        [Inject] ICurrencyAppService CurrencyAppService { get; set; } = null!;
        [Inject] IReportAppService ReportService { get; set; } = null!;


        private DataItem<List<AccountDetailsDto>> DebitAccounts { get; set; } = new();
        private AccountDetailsDto SelectedDebitAccount { get; set; } = null!;
        private CardPaymentRequest Model { get; set; } = new();
        private CardCurrencyDto? CardCurrency { get; set; } = null!;
        private ValidateCurrencyResponse CurrencyTransferData { get; set; } = null!;
        private ListItem[] beneficiaryTypes { get; set; } = default!;
        private bool IsDataLoaded { get; set; } = false;
        private bool IsValidData { get; set; } = true;
        private bool IsForeignCurrencyCard { get; set; } = false;


        #region Methods

        public async Task PrintApplication()
        {
            //await DownloadAfterSalesEForm.InvokeAsync();
            await DownloadAfterSalesEForm.InvokeAsync(new() { HolderName = SelectedCard?.HolderEmbossName });

        }

        public Task ProcessAction(ActionType actionType, string ReasonForRejection = "")
        {
            throw new NotImplementedException();
        }

        public async Task<bool> SubmitRequest(CancellationToken? cancellationToken = default)
        {
            await BindFormModel();
            if (!await IsFormValid()) return false;

            await Listen.NotifyStatus(DataStatus.Processing, Title: "Card payment", Message: $"Requesting card payment is in process");

            var response = await CardPaymentAppService.ExecuteCardPayment(Model);
            await Listen.NotifyStatus(response);
            await HandleApiResponse(response, formEditContext, formMessageStore);

            if (response.IsSuccess)
            {

                await DownloadPaymentVoucher();

                if (IsForeignCurrencyCard)
                {
                    await DownloadDeclarationForm();
                    //Print declaration form
                }
            }

            return response.IsSuccess;

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

        private async Task BindTaskDetail()
        {
            if (!IsAuthorized)
                await Listen.InvokeAsync(new() { IsAccessDenied = true });
        }

        private async Task PrepareRequestForm()
        {
            if (!IsAuthorized)
                await Listen.InvokeAsync(new() { IsAccessDenied = true });

            BindFormEditContext(Model);
            BindDropDownItems();
            List<Task> tasks = new() { LoadDebitAccounts(), LoadOwnedCreditCards(), LoadPayeeCards() };
            await Task.WhenAll(tasks);
            await BindFormModel();
            await ReadyForAction.InvokeAsync(true);
        }

        private async Task LoadDebitAccounts(string accountNumber = "", string currency = "")
        {
            DebitAccounts.Loading();
            var response = string.IsNullOrEmpty(accountNumber) ?
            await AccountAppService.GetDebitAccounts(CurrentState?.CurrentCivilId!) :
            await AccountAppService.GetDebitAccountsByAccountNumber(accountNumber);

            if (response.IsSuccess)
            {
                DebitAccounts.SetData(string.IsNullOrEmpty(currency) ? response!.Data! : response!.Data!.Where(x => x.Currency == currency).ToList());
            }
            else
            {
                DebitAccounts.Error(new(response.Message));
            }
            StateHasChanged();
        }


        private async Task OnChangeAmountHandler(bool isSourceForeignAmount = false)
        {
            if (!IsForeignCurrencyCard) return;

            CardCurrency = await CurrencyAppService.GetCardCurrencyByRequestId(Model.RequestId!);

            decimal sourceAmount = isSourceForeignAmount ? Model.ForeignAmount : Model.Amount;

            if (CardCurrency == null || sourceAmount <= 0)
                return;


            var currencyRateResponse = await CurrencyAppService.ValidateCurrencyRate(new()
            {
                CivilId = CurrentState?.CurrentCivilId!,
                SourceCurrencyCode = isSourceForeignAmount ? CardCurrency?.CurrencyIsoCode! : ConfigurationBase.KuwaitCurrency,
                SourceAmount = sourceAmount,

                ForeignCurrencyCode = isSourceForeignAmount ? ConfigurationBase.KuwaitCurrency : CardCurrency?.CurrencyIsoCode!,
                DestinationAmount = -1
            });

            if (!currencyRateResponse.IsSuccess)
            {
                Notification.Success(currencyRateResponse.Message);
                return;
            }

            CurrencyTransferData = currencyRateResponse?.Data!;


            Model.Amount = isSourceForeignAmount ? CurrencyTransferData?.DestAmount ?? 0 : CurrencyTransferData?.SrcAmount ?? 0;
            Model.ForeignAmount = isSourceForeignAmount ? CurrencyTransferData?.SrcAmount ?? 0 : CurrencyTransferData?.DestAmount ?? 0;

        }

        private async Task OnChangeDebitAccountNumber()
        {
            if (SelectedDebitAccount?.Acct == Model.DebitAccountNumber) return;

            if (DebitAccounts is not null)
                SelectedDebitAccount = DebitAccounts.Data!.FirstOrDefault(x => x.Acct == Model.DebitAccountNumber)!;

            await OnFormModelChanged();
        }

        private async Task OnFormModelChanged()
        {
            if (Model.RequestId == default) return;

            ResetAmounts();

            if (CardCurrency is null || (CardCurrency is not null && CardCurrency.RequestId != SelectedCard!.RequestId))
                CardCurrency = (await CurrencyAppService.GetCardCurrencyByRequestId(Model.RequestId)) ?? new();

            bool isSelectedCardIsSupplementaryCard = SelectedCard!.IsSupplementaryCard;
            bool isOwnedCard = !isSelectedCardIsSupplementaryCard;
            bool isPayingSupplementaryCardByForeignCurrencyDebitAccount = isSelectedCardIsSupplementaryCard && SelectedDebitAccount?.Currency != ConfigurationBase.KuwaitCurrency;
            IsValidData = !isPayingSupplementaryCardByForeignCurrencyDebitAccount;
            IsForeignCurrencyCard = CardCurrency!.IsForeignCurrency;

            if (isPayingSupplementaryCardByForeignCurrencyDebitAccount)
            {
                Notification.Failure($"Payment from {SelectedDebitAccount?.Currency} accounts is not allowed for supplementary card");
                return;
            }

            bool isSameCard = CardCurrency.RequestId == SelectedCard!.RequestId;
            if (isOwnedCard && !isSameCard)
            {
                string cardDebitAccountNumber = SelectedCard is null ? SelectedOwnedCard?.DebitAccountNumber! : SelectedCard!.BankAccountNumber!;

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

            if (SelectedCard != null)
            {
                Model.BeneficiaryCardNumber = SelectedCard.CardNumberDto;
                Model.RequestId = SelectedCard.RequestId;
                Model.BranchNumber = SelectedCard.BranchId;

                if (CardCurrency is null || (CardCurrency is not null && CardCurrency.RequestId != SelectedCard.RequestId))
                    CardCurrency = (await CurrencyAppService.GetCardCurrencyByRequestId(Model.RequestId)) ?? new();

                IsForeignCurrencyCard = CardCurrency?.IsForeignCurrency ?? false;
            }
        }



        private void ResetAmounts()
        {
            Model.Amount = 0;
            Model.ForeignAmount = 0;
        }


        #region With Card Selection Option
        private List<OwnedCreditCardsResponse> OwnedCards { get; set; } = null!;
        private List<SupplementaryCardDetail> SupplementaryCards { get; set; } = null!;
        private OwnedCreditCardsResponse? SelectedOwnedCard { get; set; }



        private void BindDropDownItems()
        {
            if (SelectedCard != null) return;
            beneficiaryTypes = Enum.GetValues(typeof(BeneficiaryTypes)).Cast<BeneficiaryTypes>()
            .Select(x => new ListItem()
            {
                Text = x.GetDescription(),
                Value = (int)x
            }).ToArray();
        }
        private async Task LoadOwnedCreditCards()
        {
            if (SelectedCard != null) return;
            var response = await CardPaymentAppService.GetOwnedCreditCards(CurrentState?.CurrentCivilId!);
            if (response.IsSuccess)
            {
                OwnedCards = response.Data?.ToList() ?? new();
                StateHasChanged();
            }
        }
        private async Task LoadPayeeCards()
        {
            if (SelectedCard != null) return;

            var response = await CardDetailsAppService.GetSupplementaryCardsByCivilId(CurrentState?.CurrentCivilId!);
            if (response.IsSuccess)
            {
                response.Data?.ForEach(x => x.CardNumberDto = x.CardNumberDto ?? x.RequestId?.ToString() ?? "");
                SupplementaryCards = response.Data ?? new();
                StateHasChanged();
            }
        }




        #endregion

        #endregion

        private async Task DownloadDeclarationForm()
        {
            Notification.Loading($"Downloading declaration form..");

            var eFormResponse = await ReportService.GenerateDeclartationForm(Model.RequestId);

            if (!eFormResponse.IsSuccess)
            {
                Notification.Failure($"Unable to download! {eFormResponse.Message}");
                return;
            }

            await DownloadFile(eFormResponse);
        }
        private async Task DownloadPaymentVoucher()
        {
            Notification.Loading($"Downloading credit card payment voucher..");

            var eFormResponse = await ReportService.GenerateCardPaymentVoucher(new PaymentVoucher()
            {
                AcctNo = Model.DebitAccountNumber,
                Amount = Model.Amount.ToString(),
                CivilID = Model.CivilId,
                CurrencyISO = Model.Currency ?? "",
                MaskedCardNumber = DisplayCardNumber(Model.BeneficiaryCardNumber) ?? ""
            });

            if (!eFormResponse.IsSuccess)
            {
                Notification.Failure($"Unable to download card payment voucher! {eFormResponse.Message}");
                return;
            }

            await DownloadFile(eFormResponse);
        }

        private async Task DownloadFile(ApiResponseModel<Domain.Models.Reports.EFormResponse> eFormResponse)
        {
            Notification.Hide();
            var streamData = new MemoryStream(eFormResponse.Data!.FileBytes!);
            using var streamRef = new DotNetStreamReference(stream: streamData);
            await JS.InvokeVoidAsync("downloadFileFromStream", $"{eFormResponse.Data?.FileName}", streamRef);
        }
        public Task Cancel()
        {
            throw new NotImplementedException();
        }
    }
}
