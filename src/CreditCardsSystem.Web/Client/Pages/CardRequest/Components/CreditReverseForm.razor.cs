using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Interfaces;
using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Models.Access;
using CreditCardsSystem.Domain.Models.CardIssuance;
using CreditCardsSystem.Domain.Models.CreditReverse;
using CreditCardsSystem.Domain.Models.StandingOrder;
using CreditCardsSystem.Utility.Extensions;
using CreditCardsSystem.Web.Client.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.Linq.Expressions;

namespace CreditCardsSystem.Web.Client.Pages.CardRequest.Components
{
    public partial class CreditReverseForm : IWorkflowMethods
    {
        private bool IsAuthorized => IsAllowTo(TaskDetail is not null ? Permissions.CreditReverse.EnigmaApprove() : Permissions.CreditReverse.Request());

        [Inject] IAccountsAppService AccountAppService { get; set; } = null!;
        [Inject] ICreditReverseAppService CreditReverseService { get; set; } = null!;
        [Inject] ICurrencyAppService CurrencyAppService { get; set; } = null!;

        private DataItem<List<AccountDetailsDto>> DebitAccounts { get; set; } = new();
        private AccountDetailsDto SelectedDebitAccount { get; set; } = null!;
        //private EditContext EditContext { get; set; } = null!;
        private CreditReverseRequest Model { get; set; } = new();
        private CardCurrencyDto? CardCurrency { get; set; } = null!;
        private ValidateCurrencyResponse CurrencyTransferData { get; set; } = null!;
        private ListItem[] beneficiaryTypes { get; set; } = default!;
        //private bool IsDataLoaded { get; set; } = false;
        //private bool IsValidData { get; set; } = true;
        private bool IsForeignCurrencyCard { get; set; } = false;

        decimal PendingReverseAmount => PendingRequests?.Sum(x => x.Amount) ?? 0;
        decimal RemainingRefundAmount => (SelectedCard?.AvailableLimit - SelectedCard?.Limit) - PendingRequests?.Sum(x => x.Amount) ?? 0;

        #region Methods
        protected override async Task OnInitializedAsync()
        {

            //Notification.Loading($"Loading data...");

            if (TaskDetail is not null)
                await BindTaskDetail();
            else
                await PrepareRequestForm();

            Notification.Hide();

        }
        public async Task ProcessAction(ActionType actionType, string ReasonForRejection = "")
        {

            if (TaskDetail is null)
                return;

            var processResponse = await CreditReverseService.ProcessCreditReverseRequest(new()
            {
                ReasonForRejection = ReasonForRejection,
                ActionType = actionType,
                RequestActivityId = RequestActivity!.RequestActivityId,
                TaskId = TaskDetail?.Id,
                WorkFlowInstanceId = TaskDetail?.InstanceId,
            });

            await Listen.NotifyStatus(processResponse);
        }

        public async Task<bool> SubmitRequest(CancellationToken? cancellationToken = default)
        {
            await BindFormModel(isSubmit: true);

            if (!await IsFormValid()) return false;

            await Listen.NotifyStatus(DataStatus.Processing, Title: "Credit reverse", Message: $"Requesting credit reverse");
            var response = await CreditReverseService.RequestCreditReverse(Model);
            await Listen.NotifyStatus(response);
            await HandleApiResponse(response, formEditContext, formMessageStore);
            return response.IsSuccess;
        }



        public async Task PrintApplication()
        {
            await DownloadAfterSalesEForm.InvokeAsync(new() { HolderName = SelectedCard?.HolderEmbossName });
        }



        async Task DeleteCreditReverseRequest(long id)
        {
            var response = await CreditReverseService.DeleteCreditReverseRequestById(id);
            await HandleApiResponse(response);
            if (response.IsSuccess)
            {
                PendingRequests.Remove(PendingRequests.FirstOrDefault(x => x.ID == id));
                StateHasChanged();
            }
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
            //ConfigEditContext(EditContext, Model, messageStore);

            List<Task> tasks = new() { LoadDebitAccounts(), LoadPendingRequests() };
            await Task.WhenAll(tasks);
            await BindFormModel();
            await ReadyForAction.InvokeAsync(true);
        }

        private async Task LoadDebitAccounts(string accountNumber = "", string currency = "")
        {
            string civilId = CurrentState?.CurrentCivilId!;

            if (SelectedCard!.IsSupplementaryCard)
            {
                accountNumber = (await AccountAppService.GetAccountNumberByRequestId(SelectedCard.Parameters!.PrimaryCardRequestId!))?.Data ?? "";
                civilId = SelectedCard.Parameters!.PrimaryCardCivilId!;
            }
            DebitAccounts.Loading();
            var response = string.IsNullOrEmpty(accountNumber) ?
            await AccountAppService.GetDebitAccounts(civilId) :
            await AccountAppService.GetDebitAccountsByAccountNumber(accountNumber);


            bool IsYouthCard = ConfigurationBase.YouthCardTypes.Split(",").Any(x => x == SelectedCard!.CardType.ToString());

            if (response.IsSuccess)
            {
                var debitAccount = string.IsNullOrEmpty(currency) ? response!.Data! : response!.Data!.Where(x => x.Currency == currency).ToList();

                if (IsYouthCard)
                    debitAccount = debitAccount!.Where(x => ConfigurationBase.YouthAccountTypes.Split(",").Contains(x.AcctType)).ToList();
                else
                    debitAccount = debitAccount!.Where(x => !ConfigurationBase.YouthAccountTypes.Split(",").Contains(x.AcctType)).ToList();

                DebitAccounts.SetData([.. debitAccount.OrderByDescending(x => x.AvailableBalance)]);
            }
            else
            {
                Notification.Failure(response.Message);
                DebitAccounts = new();
                DebitAccounts.Error(new(response.Message));
            }

            StateHasChanged();
        }


        private async Task OnChangeAmountHandler()
        {
            if (!IsForeignCurrencyCard) return;

            CardCurrency = await CurrencyAppService.GetCardCurrencyByRequestId(Model.RequestId!);

            if (CardCurrency == null || Model.Amount <= 0)
                return;

            //formMessageStore.Clear(() => Model.Amount);
            //formMessageStore.Clear(() => Model.TransferAmount);

            var currencyRateResponse = await CurrencyAppService.ValidateCurrencyRate(new()
            {
                CivilId = CurrentState?.CurrentCivilId!,
                SourceCurrencyCode = CardCurrency?.CurrencyIsoCode,
                ForeignCurrencyCode = ConfigurationBase.KuwaitCurrency,
                SourceAmount = Model.Amount,
                DestinationAmount = -1
            });

            if (!currencyRateResponse.IsSuccess)
            {
                Notification.Failure(currencyRateResponse.Message);
                return;
            }

            CurrencyTransferData = currencyRateResponse?.Data!;
            Model.AmountInKWD = CurrencyTransferData?.DestAmount ?? 0;
            if (Model.Amount > RemainingRefundAmount)
            {
                formEditContext.AddAndNotifyFieldError(formMessageStore, () => Model.Amount, $"Invalid amount!, the amount is greater than maximum refund amount {RemainingRefundAmount}");
                return;
            }
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

            //ResetAmounts();

            if (CardCurrency is null || (CardCurrency is not null && CardCurrency.RequestId != SelectedCard!.RequestId))
                CardCurrency = (await CurrencyAppService.GetCardCurrencyByRequestId(Model.RequestId)) ?? new();

            bool isSelectedCardIsSupplementaryCard = SelectedCard!.IsSupplementaryCard;
            bool isOwnedCard = !isSelectedCardIsSupplementaryCard;
            bool isPayingSupplementaryCardByForeignCurrencyDebitAccount = isSelectedCardIsSupplementaryCard && SelectedDebitAccount?.Currency != ConfigurationBase.KuwaitCurrency;
            //IsValidData = !isPayingSupplementaryCardByForeignCurrencyDebitAccount;
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
        private async Task BindFormModel(bool isSubmit = false)
        {
            //IsDataLoaded = true;
            Model.CivilId = SelectedCard?.CivilId!;
            //Model.Currency = SelectedDebitAccount?.Currency ?? "";

            if (SelectedCard != null)
            {
                Model.BeneficiaryCardNumber = SelectedCard.CardNumberDto;
                Model.RequestId = SelectedCard.RequestId;
                Model.BranchNumber = SelectedCard.BranchId;
                Model.ExternalStatus = SelectedCard.ExternalStatus;
                if (CardCurrency is null || (CardCurrency is not null && CardCurrency.RequestId != SelectedCard.RequestId))
                    CardCurrency = (await CurrencyAppService.GetCardCurrencyByRequestId(Model.RequestId)) ?? new();

                IsForeignCurrencyCard = CardCurrency?.IsForeignCurrency ?? false;
            }

            if (!isSubmit)
            {
                await CopyRefundAmount();
            }
        }

        private void ResetAmounts()
        {
            Model.Amount = 0;
            Model.AmountInKWD = 0;
        }



        #region With Card Selection Option
        private List<CreditReverseDto> PendingRequests { get; set; } = null!;
        private OwnedCreditCardsResponse? SelectedOwnedCard { get; set; }
        private CreditReverseDto? SelectedPendingRequest { get; set; }


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
        private async Task LoadPendingRequests()
        {
            if (SelectedCard is null) return;

            var response = await CreditReverseService.GetPendingRequest(SelectedCard!.CardNumberDto!);
            if (response.IsSuccess)
            {
                PendingRequests = response.Data?.ToList() ?? new();
            }

            StateHasChanged();
        }

        #endregion

        #endregion
        async Task OnChangeAction(ListItem<CreditReverseDto> item)
        {

            if (item.Text!.Equals("More Detail", StringComparison.InvariantCultureIgnoreCase))
            {
                SelectedPendingRequest = item.Value;
                await CreditReverseDetails!.ToggleAsync();
            }

            if (item.Text.Equals("Delete", StringComparison.InvariantCultureIgnoreCase))
                await DeleteCreditReverseRequest(item.Value!.ID);
        }

        async Task CopyRefundAmount()
        {
            Model.Amount = RemainingRefundAmount;
            await OnChangeAmountHandler();
        }

        public Task Cancel()
        {
            throw new NotImplementedException();
        }
    }
}
