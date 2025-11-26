using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Interfaces;
using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Models.Access;
using CreditCardsSystem.Domain.Models.CardIssuance;
using CreditCardsSystem.Domain.Models.CardOperation;
using CreditCardsSystem.Utility.Extensions;
using CreditCardsSystem.Web.Client.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace CreditCardsSystem.Web.Client.Pages.CardRequest.Components;

public partial class MigrateCollateralForm : IWorkflowMethods
{

    private bool IsAuthorized => IsAllowTo(TaskDetail is not null ? Permissions.MigrateCollateral.EnigmaApprove() : Permissions.MigrateCollateral.Request());

    [Inject] private IMigrateCollateralAppService CardOperationService { get; set; } = null!;

    [Inject] private IAccountsAppService AccountService { get; set; } = null!;
    [Inject] private IEmployeeAppService EmployeeService { get; set; } = null!;
    [Inject] private ICurrencyAppService CurrencyService { get; set; } = null!;

    public MigrateCollateralRequest Model { get; set; } = new();

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
        {
            await Listen.InvokeAsync(new() { IsAccessDenied = true });
            return;
        }



        await Task.CompletedTask;
    }

    async Task PrepareRequestForm()
    {
        if (!IsAuthorized)
        {
            await Listen.InvokeAsync(new() { IsAccessDenied = true });
            return;
        }


        await BindDropDownData();
        Model.RequestId = Convert.ToDecimal(SelectedCard!.RequestId);
        CardCurrency = await CurrencyService.GetCardCurrency(SelectedCard!.CardType);
        BindFormEditContext(Model);
        await ReadyForAction.InvokeAsync(true);
    }
    public async Task ProcessAction(ActionType actionType, string ReasonForRejection)
    {
        if (TaskDetail is null) return;


        var result = await CardOperationService.ProcessMigrateCollateral(new()
        {
            ActionType = actionType,
            ReasonForRejection = ReasonForRejection,
            RequestId = RequestActivity!.RequestId,
            RequestActivityId = RequestActivity!.RequestActivityId,
            TaskId = TaskDetail?.Id,
            WorkFlowInstanceId = TaskDetail?.InstanceId
        });
        await Listen.NotifyStatus(data: result); ;
    }
    public async Task<bool> SubmitRequest(CancellationToken? cancellationToken = default)
    {
        if (!await IsFormValid()) return false;

        await Listen.NotifyStatus(DataStatus.Processing, Title: "Collateral migration", Message: $"Requesting collateral migration");


        var response = await CardOperationService.RequestMigrateCollateral(Model);
        await Listen.NotifyStatus(response);
        return response.IsSuccess;
    }

    public Task PrintApplication()
    {
        throw new NotImplementedException();
    }

    string? PrimaryCardAccountNumber { get; set; }
    private record collateralRecord
    {
        public collateralRecord(Collateral collateral, string name = "")
        {
            this.name = string.IsNullOrEmpty(name) ? collateral.GetDescription() : name;
            this.value = collateral;
        }
        public string name { get; set; }
        public Collateral value { get; set; }
    }
    private List<collateralRecord> collateralData = new();
    DataItem<ValidateSellerIdResponse> SellerNameData = new();
    private ValidationMessageStore? validationMessageStore;
    private EditContext EditContextRequest { get; set; } = null!;
    private async Task OnChangeSellerId()
    {
        SellerNameData.Reset();
        validationMessageStore?.Clear(EditContextRequest.Field(nameof(Model.IsConfirmedSellerId)));
        validationMessageStore?.Clear(EditContextRequest.Field(nameof(Model.SellerId)));
        await VerifySellerId();
    }
    private async Task VerifySellerId()
    {
        SellerNameData.Loading();

        var seller = await EmployeeService.ValidateSellerId(Model.SellerId?.ToString("0")!);

        //CardRequest.IsConfirmedSellerId = seller.IsSuccess;

        if (!seller.IsSuccess)
        {
            SellerNameData.Error(new(seller.Message));
            EditContextRequest.AddAndNotifyFieldError(validationMessageStore!, () => Model.SellerId!, GlobalResources.InvalidSellerId, true);
            return;
        }

        SellerNameData.SetData(seller.Data!);
    }
    private async Task BindDropDownData()
    {
        collateralData.Add(new(Collateral.AGAINST_MARGIN, "Migrate to Margin Account"));
        collateralData.Add(new(Collateral.AGAINST_DEPOSIT, "Migrate to Deposit Account"));
        await Task.CompletedTask;
    }
    private Collateral? lastSelectedCollateral;
    private DataItem<List<AccountDetailsDto>> CardAccounts { get; set; } = new();
    private DataItem<List<AccountDetailsDto>> DebitAccounts { get; set; } = new();
    private CardCurrencyDto? CardCurrency { get; set; }
    private bool IsChargeCard => SelectedCard?.ProductType == ProductTypes.ChargeCard;
    private bool IsTayseerCard => SelectedCard?.ProductType == ProductTypes.Tayseer;
    public bool IsUsdChargeCard => IsChargeCard && IsUsdCard;
    private bool IsUsdCard => CardCurrency?.CurrencyIsoCode == ConfigurationBase.USDollerCurrency;
    private AccountDetailsDto? SelectedDebitAccount { get; set; }
    private AccountDetailsDto? SelectedCardAccount { get; set; }
    public bool IsAgreeToCreateMargin { get; set; } = false;


    private async Task OnChangeCollateral()
    {
        if (lastSelectedCollateral == Model.Collateral) return;

        lastSelectedCollateral = Model.Collateral;

        //reloading debit account list
        await GetCustomerAccountList(SelectedCard?.CivilId!);

    }
    private async Task GetCustomerAccountList(string CivilID, bool loadDebitAccount = true)
    {

        await LoadCardAccounts(CivilID);

        async Task LoadCardAccounts(string CivilID)
        {
            if (Model.Collateral is null)
                return;

            CardAccounts.Loading();

            ApiResponseModel<List<AccountDetailsDto>>? response = Model.Collateral switch
            {
                Collateral.AGAINST_DEPOSIT => IsUsdCard ? await AccountService.GetDepositAccountsForUSDCard(CivilID) : await AccountService.GetDepositAccounts(CivilID),
                Collateral.AGAINST_MARGIN => await AccountService.GetDebitAccounts(CivilID),
                _ => null
            };

            if (response is null)
                return;

            if (!response.IsSuccess)
            {
                var errors = response.ValidationErrors != null ? string.Join(",", response.ValidationErrors.Select(x => x.Error).ToArray()) : "";
                Notification.Failure($"Technical error ! {errors}");
                CardAccounts.Error();
                return;
            }

            CardAccounts.SetData(response.Data ?? new());

            StateHasChanged();
        }

        //async Task LoadDebitAccounts(string CivilID)
        //{
        //    ApiResponseModel<List<AccountDetailsDto>> accountResponse = new();

        //    DebitAccounts.Loading();

        //    //TODO: Refactor ( create property for IsUSDCard in Eligibility)
        //    if (IsUsdChargeCard)
        //    {
        //        accountResponse = await AccountAppService.GetDebitAccountsForUSDCard(CivilID);
        //    }
        //    else
        //    {
        //        accountResponse = await AccountAppService.GetDebitAccounts(CivilID);

        //        string? cardCurrencyCode = CardCurrency?.CurrencyIsoCode;

        //        if (!string.IsNullOrEmpty(cardCurrencyCode) && cardCurrencyCode != ConfigurationBase.KuwaitCurrency)
        //            accountResponse.Data = accountResponse?.Data?.Where(account => account.Currency == ConfigurationBase.KuwaitCurrency).ToList();
        //    }


        //    if (!accountResponse?.IsSuccess ?? false)
        //    {
        //        var errors = accountResponse?.ValidationErrors != null ? string.Join(",", accountResponse.ValidationErrors.Select(x => x.Error).ToArray()) : "";
        //        Notification.Failure($"Technical error ! {errors}");
        //        DebitAccounts.Error();
        //        return;
        //    }

        //    DebitAccounts.SetData(accountResponse?.Data ?? new());
        //    StateHasChanged();
        //}
    }


    private async Task OnCardAccountChanged(string accountNumber)
    {

        if (SelectedCardAccount?.Acct == accountNumber)
            return;

        SelectedCardAccount = CardAccounts.Data?.FirstOrDefault(x => x.Acct == accountNumber);

        StateHasChanged();
        await Task.CompletedTask;
    }

    public Task Cancel()
    {
        throw new NotImplementedException();
    }
}
