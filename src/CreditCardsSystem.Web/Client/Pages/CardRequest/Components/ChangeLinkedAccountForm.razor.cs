using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Interfaces;
using CreditCardsSystem.Domain.Models.Access;
using CreditCardsSystem.Domain.Models.CardIssuance;
using CreditCardsSystem.Domain.Models.CardOperation;
using CreditCardsSystem.Domain.Models.Reports;
using CreditCardsSystem.Web.Client.Components;
using Microsoft.AspNetCore.Components;

namespace CreditCardsSystem.Web.Client.Pages.CardRequest.Components
{
    public partial class ChangeLinkedAccountForm : IWorkflowMethods
    {

        private bool IsAuthorized => IsAllowTo(TaskDetail is not null ? Permissions.ChangeLinkedAccount.EnigmaApprove() : Permissions.ChangeLinkedAccount.Request());
        [Inject] IAddressAppService AddressService { get; set; } = null!;

        #region Maker


        [Parameter]
        public ChangeLinkedAccountRequest Model { get; set; } = new();

        async Task PrepareRequestForm()
        {
            if (!IsAuthorized || SelectedCard!.IsSupplementaryCard)
            {
                await Listen.InvokeAsync(new() { IsAccessDenied = true });
                return;
            }


            Model.RequestId = SelectedCard!.RequestId;

            #region Loading Debit Account
            IsUsdCard = (new Collateral[] { Collateral.AGAINST_DEPOSIT_USD, Collateral.AGAINST_SALARY_USD }).Any(x => x.Equals(SelectedCard!.Parameters!.Collateral));
            DebitAccounts.Loading();
            var debitAccounts = (IsUsdCard ? await AccountService.GetDebitAccountsForUSDCard(SelectedCard!.CivilId!) : await AccountService.GetDebitAccountsByAccountNumber(SelectedCard!.BankAccountNumber!));
            if (debitAccounts.IsSuccess)
            {
                DebitAccounts.SetData(debitAccounts.Data ?? new());
            }
            else
            {
                DebitAccounts.Error(new(debitAccounts.Message));
            }
            Model.NewLinkedAccountNumber = SelectedCard!.BankAccountNumber ?? "";
            Model.OldLinkedAccountNumber = SelectedCard!.BankAccountNumber ?? "";
            Model.IsSupplementaryCard = SelectedCard!.IsSupplementaryCard;
            #endregion

            await ReadyForAction.InvokeAsync(true);
        }

        public async Task<bool> SubmitRequest(CancellationToken? cancellationToken = default)
        {
            if (!await IsValidForm())
            {
                Notification.Failure(GlobalResources.InvalidInput);
                return false;
            }

            await Listen.NotifyStatus(DataStatus.Processing, Title: "Link Account", Message: $"Requesting linked acccount number change");
            var requestResponse = await changeOfAddressAppService.RequestChangeLinkedAccount(Model);
            await Task.Delay(2000);
            await Listen.NotifyStatus(data: requestResponse); ;
            return requestResponse.IsSuccess;

            async Task<bool> IsValidForm()
            {
                if (!string.IsNullOrEmpty(SelectedCard.BankAccountNumber))
                    return await Task.FromResult(SelectedCard.BankAccountNumber != Model.NewLinkedAccountNumber);

                return await Task.FromResult(!string.IsNullOrEmpty(Model.NewLinkedAccountNumber));
            }
        }
        #endregion

        #region Checker


        bool IsViewOnly => RequestActivity?.RequestActivityStatus != RequestActivityStatus.Pending;

        async Task BindTaskDetail()
        {

            if (!IsAuthorized)
            {
                await Listen.InvokeAsync(new() { IsAccessDenied = true });
                return;
            }
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

                await Listen.NotifyStatus(processResponse);
            }
        }
        #endregion



        [Inject] IAccountsAppService AccountService { get; set; } = null!;
        [Inject] IChangeOfAddressAppService changeOfAddressAppService { get; set; } = null!;

        [Parameter]
        public DataItem<List<AccountDetailsDto>> DebitAccounts { get; set; } = new();

        [Parameter]
        public EventCallback OnChangeAccountNumber { get; set; }


        private AccountDetailsDto? SelectedDebitAccount { get; set; }

        protected override async Task OnInitializedAsync()
        {



            if (TaskDetail is not null)
                await BindTaskDetail();
            else
                await PrepareRequestForm();

            Notification.Hide();


        }

        bool IsUsdCard { get; set; }


        public async Task PrintApplication()
        {

            var billingAddress = (await AddressService.GetRecentBillingAddress(requestId: SelectedCard!.RequestId!))?.Data!;
            await DownloadAfterSalesEForm.InvokeAsync(new AfterSalesForm()
            {
                AccountNo = Model.NewLinkedAccountNumber,
                HolderName = SelectedCard.HolderEmbossName,
                Address = $"POBox:{billingAddress.PostOfficeBoxNumber} City:{billingAddress.City} Post Code:{billingAddress.PostalCode} Street:{billingAddress.Street}",
                MobileNo = billingAddress.Mobile.ToString(),
                Tel = $"Home Phone:{billingAddress.HomePhone} Work Phone:{billingAddress.WorkPhone}"
            });
        }


        private async Task OnChangeDebitAccountNumber()
        {
            if (SelectedDebitAccount?.Acct == Model.NewLinkedAccountNumber) return;

            if (DebitAccounts is not null)
                SelectedDebitAccount = DebitAccounts.Data!.FirstOrDefault(x => x.Acct == Model.NewLinkedAccountNumber)!;

        }

        public Task Cancel()
        {
            throw new NotImplementedException();
        }
    }
}
