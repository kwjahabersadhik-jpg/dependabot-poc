using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Interfaces;
using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Models.Access;
using CreditCardsSystem.Domain.Models.Card;
using CreditCardsSystem.Domain.Models.CardIssuance;
using CreditCardsSystem.Domain.Models.StandingOrder;
using CreditCardsSystem.Domain.Models.SupplementaryCard;
using CreditCardsSystem.Utility.Crypto;
using CreditCardsSystem.Utility.Extensions;
using CreditCardsSystem.Web.Client.Components;
using Kfh.Aurora.Organization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace CreditCardsSystem.Web.Client.Pages.CardRequest.Components;

public partial class StandingOrderForm : IWorkflowMethods
{

    [Inject]
    IConfigurationAppService configurationAppService { get; set; } = null!;
    private bool IsAuthorized => IsAllowTo(StandingOrderId is null ? Permissions.StandingOrder.Create() : Permissions.StandingOrder.Edit());

    [Parameter]
    public int? StandingOrderId { get; set; }

    [Parameter]
    public bool EnableBeneficiarySelection { get; set; }

    [Parameter]
    public string? BeneficiaryCardNumber { get; set; }

    private string? Action { get; set; }

    [SupplyParameterFromForm]
    private StandingOrderRequest Model { get; set; } = new();
    private DataItem<List<AccountDetailsDto>> DebitAccounts { get; set; } = new();
    public AccountDetailsDto? SelectedDebitAccount { get; set; }
    private List<OwnedCreditCardsResponse> OwnedCards { get; set; } = null!;
    private List<SupplementaryCardDetail> SupplementaryCards { get; set; } = null!;
    private bool IsWithCharge { get; set; } = true;
    private List<AccountDetailsDto> SelectedCharge { get; set; } = new List<AccountDetailsDto>();


    private ListItem[] beneficiaryTypes { get; set; } = default!;
    private ListItem[] durationTypes { get; set; } = default!;
    private decimal AvailableBalance { get; set; }

    private int SelectedBeneficiaryType = (int)BeneficiaryTypes.OwnedCard;
    //private int? SelectedDurationType { get; set; } = (int)DurationTypes.Date;
    private string? SelectedOwnedCardNumber { get; set; }
    private decimal OwnedCardLimit { get; set; }
    private string OwnedCardProductName { get; set; } = null!;
    private string? SelectedSupplementaryCardNumber { get; set; }
    private string SupplementaryCardFullName { get; set; } = null!;
    private string SupplementaryCardDescription { get; set; } = null!;
    private string? CurrentBeneficiaryCardNumber { get; set; }
    private bool ShowDeleteDialog { get; set; } = false;
    private bool IsEditMode { get; set; } = false;


    int SelectedDuration { get; set; }

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
        await Task.CompletedTask;
    }

    async Task PrepareRequestForm()
    {
        if (!IsAuthorized)
        {
            await Listen.InvokeAsync(new() { IsAccessDenied = true });
            return;
        }


        BindTitle();
        BindFormEditContext(Model);
        BindDropDownItems();

        List<Task> tasks = new()
        {
            LoadStandingOrder(),
            LoadDebitAccounts(),
            LoadOwnedCreditCards(),
            LoadPayeeCards()
        };

        await Task.WhenAll(tasks);
        await BindStandingOrderModel();
        BindFormEditContext(Model);
        await ReadyForAction.InvokeAsync(true);

    }
    public async Task ProcessAction(ActionType actionType, string ReasonForRejection)
    {
        await Task.CompletedTask;
    }

    public async Task<bool> SubmitRequest(CancellationToken? cancellationToken = default)
    {
        Model.OrderDuration = (DurationTypes)SelectedDuration;

        if (!await IsFormValid()) return false;

        await BindPreRequest();
        await Listen.NotifyStatus(DataStatus.Processing, Title: "Standing Order", Message: $"{(StandingOrderId > 0 ? "Updating" : "Adding new")} standing order");
        ApiResponseModel<StandingOrderResponse> response = new();

        if (StandingOrderId > 0)
        {
            response = await _standingOrderAppService.UpdateStandingOrders(Model);
        }
        else
        {
            response = await _standingOrderAppService.AddStandingOrders(Model);
        }


        await HandleApiResponse(response, formEditContext, formMessageStore, true);

        await Listen.NotifyStatus(response);

        return response.IsSuccess;




    }
    public async Task PrintApplication()
    {
        //await DownloadAfterSalesEForm.InvokeAsync(new());
        await DownloadAfterSalesEForm.InvokeAsync(new() { HolderName = SelectedCard?.HolderEmbossName });
    }

    #region private methods
    async Task OnChangeOrderDuration(string value)
    {
        Model.NumberOfTransfer = null;
        Model.EndDate = null;
        Model.OrderDuration = (DurationTypes)Enum.Parse(typeof(DurationTypes), value);
        await Task.CompletedTask;

    }


    private void BindTitle()
    {
        IsEditMode = StandingOrderId != null && StandingOrderId != 0;
        Action = IsEditMode ? "Edit" : "Add";
    }
    private void BindDropDownItems()
    {
        beneficiaryTypes = Enum.GetValues(typeof(BeneficiaryTypes)).Cast<BeneficiaryTypes>().Select(x => new ListItem()
        {
            Text = x.GetDescription(),
            Value = (int)x
        }).ToArray();

        durationTypes = Enum.GetValues(typeof(DurationTypes)).Cast<DurationTypes>().Select(x => new ListItem()
        {
            Text = x.GetDescription(),
            Value = (int)x
        }).ToArray();
    }
    private async Task OnChangeDebitAccountNumber()
    {
        if (string.IsNullOrEmpty(Model.DebitAccountNumber)) return;

        SelectedDebitAccount = DebitAccounts.Data.FirstOrDefault(x => x.Acct == Model.DebitAccountNumber);
        AvailableBalance = SelectedDebitAccount?.AvailableBalance ?? 0;
        Model.Currency = SelectedDebitAccount?.Currency ?? "";

        try
        {
            var serviceName = ConfigurationBase.VAT_Add_StandingOrder_ServiceName;
            var result = await GetServiceFee(serviceName, Model.DebitAccountNumber);

            Model.ChargeAmount = result.fees;

            if (result.isVatApplicable)
                Model.VatAmount = Math.Round(Model.ChargeAmount * result.vatPercentage / 100, 3);
        }
        catch (Exception ex)
        {
            await Listen.InvokeAsync(new(false, DataStatus.Error, Message: ex.Message));
            await ReadyForAction.InvokeAsync(false);
            return;
        }



        Model.TotalAmount = (Model.ChargeAmount + Model.VatAmount);
    }
    private async Task resetSelection()
    {

        if (StandingOrderId is not null)
        {
            if (SelectedBeneficiaryType == (int)BeneficiaryTypes.OwnedCard && OwnedCards.Any(x => x.CardNumberDto == Model.BeneficiaryCardNumber))
            {
                SelectedOwnedCardNumber = Model.BeneficiaryCardNumber;
            }

            if (SelectedBeneficiaryType == (int)BeneficiaryTypes.SupplementaryCard && SupplementaryCards.Any(x => x.CardNumberDto == Model.BeneficiaryCardNumber))
            {
                SelectedSupplementaryCardNumber = Model.BeneficiaryCardNumber;
            }
        }
        else
        {
            SelectedSupplementaryCardNumber = null;
            SelectedOwnedCardNumber = null;
            SelectedCard = null;
        }
        await Task.CompletedTask;


    }
    private void OnChangeOwnedCardNumber()
    {

        Model.BeneficiaryCardNumber = SelectedOwnedCardNumber ?? "";
        var selectedCard = OwnedCards.FirstOrDefault(x => x.CardNumber == SelectedOwnedCardNumber);

        if (selectedCard == null) return;

        SelectedCard = new CardDetailsResponse()
        {
            CardStatus = selectedCard.CardStatus,
            CardNumber = selectedCard.CardNumberDto,
            RequestId = Convert.ToInt64(selectedCard.RequestId),
            ProductName = selectedCard.ProductName,
            ApproveLimit = selectedCard.ApprovedLimit,
            CardType = selectedCard.CardType
        };

        Model.IsApproved = SelectedCard.CardStatus == CreditCardStatus.Approved;
        OwnedCardLimit = SelectedCard.ApproveLimit;
        OwnedCardProductName = SelectedCard.ProductName.ToString();
        Model.RequestId = SelectedCard.RequestId;
    }
    private void OnChangeSupplementaryCardNumber()
    {
        SelectedCard = null;
        var selectedCard = SupplementaryCards.FirstOrDefault(x => x.CardNumber == SelectedSupplementaryCardNumber);
        Model.BeneficiaryCardNumber = SelectedSupplementaryCardNumber ?? "";


        if (selectedCard == null) return;

        SelectedCard = new CardDetailsResponse()
        {
            CardStatus = selectedCard.CardStatus,
            CardNumber = selectedCard.CardNumberDto,
            RequestId = Convert.ToInt64(selectedCard.RequestId),
            ProductName = selectedCard.CardData!.ProductName ?? "",
            CardType = selectedCard.CardData!.CardType,
            ApproveLimit = selectedCard.CardData.ApprovedLimit,
            IsSupplementaryCard = true
        };


        Model.IsApproved = SelectedCard.CardStatus == CreditCardStatus.Approved;
        SupplementaryCardFullName = selectedCard.FullName!;
        SupplementaryCardDescription = selectedCard!.Description!;
        Model.RequestId = SelectedCard.RequestId;
    }
    private async Task<(decimal fees, bool isVatApplicable, decimal vatPercentage)> GetServiceFee(string serviceName, string debitAccountNumber)
    {
        var response = await _feesAppService.GetServiceFee(new() { ServiceName = serviceName, DebitAccountNumber = debitAccountNumber });

        if (!response.IsSuccess)
            throw new ApiException(message: response.Message);

        if (response.IsSuccess)
            return (response!.Data!.Fees!, response!.Data!.IsVatApplicable, response!.Data!.VatPercentage);

        return default;
    }
    private async Task LoadStandingOrder()
    {
        if (StandingOrderId == null) return;
        var response = await _standingOrderAppService.GetAllStandingOrders(CurrentState?.CurrentCivilId ?? "", standingOrderId: StandingOrderId);
        if (response.IsSuccess)
        {
            var standingOrderDto = response?.Data?.FirstOrDefault() ?? new();

            Model = new StandingOrderRequest()
            {
                DebitAccountNumber = standingOrderDto.SourceAccount,
                Amount = standingOrderDto.Amount,
                Currency = standingOrderDto.Currency,
                StartDate = (standingOrderDto.StartDate == DateTime.MinValue || standingOrderDto.StartDate == null) ? standingOrderDto.NextTransferDate : standingOrderDto.StartDate ?? default,
                EndDate = standingOrderDto.EndDate,
                NumberOfTransfer = standingOrderDto.NumberOfTransfers,
                BeneficiaryCardNumber = DisplayCardNumber(standingOrderDto.CardNumberDto),
                AllowDelete = standingOrderDto.AllowDelete,
                AllowUpdate = standingOrderDto.AllowUpdate
            };
        }
    }
    private async Task BindStandingOrderModel()
    {
        Model.StartDate = DateTime.Today;

        if (StandingOrderId == null) return;

        if (EnableBeneficiarySelection == false)
            Model.BeneficiaryCardNumber = BeneficiaryCardNumber!;

        if (Model.EndDate is not null && Model.EndDate != ConfigurationBase.UnlimitedEndDate)
            Model.OrderDuration = DurationTypes.Date;

        else if (Model.OrderDuration is null && Model is { NumberOfTransfer: > 0 })
            Model.OrderDuration = DurationTypes.Count;

        else if (Model.OrderDuration is null && (Model.EndDate is not null && Model.EndDate == ConfigurationBase.UnlimitedEndDate))
            Model.OrderDuration = DurationTypes.Unlimited;

        Model.StartDate = IsEditMode ? Model.StartDate : DateTime.Today;


        if (OwnedCards.Any(x => x.CardNumber == Model.BeneficiaryCardNumber))
        {
            SelectedOwnedCardNumber = Model.BeneficiaryCardNumber;
            SelectedBeneficiaryType = (int)BeneficiaryTypes.OwnedCard;
            OnChangeOwnedCardNumber();
        }

        if (SupplementaryCards.Any(x => x.CardNumber == Model.BeneficiaryCardNumber))
        {
            SelectedSupplementaryCardNumber = Model.BeneficiaryCardNumber;
            SelectedBeneficiaryType = (int)BeneficiaryTypes.SupplementaryCard;
            OnChangeSupplementaryCardNumber();
        }

        if (string.IsNullOrEmpty(SelectedOwnedCardNumber) && string.IsNullOrEmpty(SelectedSupplementaryCardNumber))
        {
            CurrentBeneficiaryCardNumber = Model.BeneficiaryCardNumber;
        }

        await OnChangeDebitAccountNumber();
        StateHasChanged();
    }
    private async Task BindPreRequest()
    {
        var chargeAccount = SelectedCharge.FirstOrDefault();
        Model.ChargeAccountNumber = chargeAccount != null ? chargeAccount.Acct : "";

        Model.LastPayment = Model.OrderDuration switch
        {
            DurationTypes.Date => Model.EndDate?.Formed(),
            DurationTypes.Count => Model.NumberOfTransfer.ToString(),
            _ => ConfigurationBase.UnlimitedEndDate.ToString(ConfigurationBase.ReportDateFormat)
        };

        if (Model.OrderDuration == DurationTypes.Unlimited)
        {
            Model.EndDate = ConfigurationBase.UnlimitedEndDate;
            Model.NumberOfTransfer = null;
        }

        Branch userBranch = await configurationAppService.GetUserBranch();
        Model.BranchNumber = userBranch.BranchId;

        Model.StandingOrderId = StandingOrderId;

        //if (string.IsNullOrEmpty(Model.BeneficiaryCardNumber))
        //{
        Model.BeneficiaryCardNumber = SelectedCard?.CardNumber ?? Model.BeneficiaryCardNumber ?? "";
        //}

        await Task.CompletedTask;
    }
    private async Task LoadDebitAccounts()
    {
        DebitAccounts.Loading();
        var response = await _accountAppService.GetDebitAccounts(CurrentState.CurrentCivilId ?? "");

        if (response.IsSuccess)
        {
            DebitAccounts.SetData([.. (response?.Data ?? []).OrderByDescending(x => x.AvailableBalance)]);

        }
        else
        {
            DebitAccounts.Error(new(response.Message));
        }

        StateHasChanged();
    }
    private async Task LoadOwnedCreditCards()
    {
        var response = await _standingOrderAppService.GetOwnedCreditCards(CurrentState.CurrentCivilId ?? "");
        if (response.IsSuccess)
        {
            OwnedCards = response.Data?.ToList() ?? new();

            OwnedCards.ForEach(x => x.CardNumber = DisplayCardNumber(x.CardNumberDto));
            StateHasChanged();
        }
    }
    private async Task LoadPayeeCards()
    {
        var response = await _cardDetailsAppService.GetSupplementaryCardsByCivilId(CurrentState.CurrentCivilId ?? "");
        if (response.IsSuccess)
        {
            response.Data?.ForEach(x =>
            {
                x.CardNumberDto = x.CardNumberDto ?? x.RequestId?.ToString() ?? "";
                x.CardNumber = DisplayCardNumber(x.CardNumberDto) ?? x.RequestId?.ToString() ?? "";
            });
            SupplementaryCards = response.Data ?? new();
            StateHasChanged();
        }

    }
    #endregion
    public void Dispose()
    {
        UnBindFormEditContext();
    }

    public Task Cancel()
    {
        throw new NotImplementedException();
    }
}
