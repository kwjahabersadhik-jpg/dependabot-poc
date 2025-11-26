using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Interfaces;
using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Models.Access;
using CreditCardsSystem.Domain.Models.Account;
using CreditCardsSystem.Domain.Models.CardIssuance;
using CreditCardsSystem.Domain.Models.CardOperation;
using CreditCardsSystem.Domain.Models.SupplementaryCard;
using CreditCardsSystem.Web.Client.Components;
using Kfh.Aurora.Blazor.Components.UI;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;


namespace CreditCardsSystem.Web.Client.Pages.CardRequest.Components;

public partial class ChangeLimitForm : IWorkflowMethods
{

    //checker permission will validate on requested limit
    private bool IsAuthorized => TaskDetail is not null || IsAllowTo(Permissions.ChangeLimit.Request());

    [Inject] private IChangeLimitAppService ChangeLimitAppService { get; set; } = null!;
    [Inject] private ICardDetailsAppService CardDetailsAppService { get; set; } = null!;
    [Inject] private IAccountsAppService AccountService { get; set; } = null!;

    [Parameter]
    public ChangeLimitRequest Model { get; set; } = new();

    [Parameter]
    public List<AccountDetailsDto>? DebitAccounts { get; set; }

    public bool EnableTayseerCreditChecking { get; set; }
    public bool EnableKfhSalary { get; set; }
    public bool EnableTemporary { get; set; }
    public bool IsHavingPendingRequest { get; set; }

    Collateral collateral { get; set; }
    private OffCanvas? ChangeLimitHistoryDetail { get; set; }
    private DataItem<List<ChangeLimitHistoryDto>> ChangeLimitHistory { get; set; } = new();
    private ChangeLimitHistoryDto? SelectedHistory { get; set; }
    private DataItem<List<SupplementaryCardDetail>> Supplementary { get; set; } = new();
    private DataItem<List<AccountDetailsDto>> CardAccounts { get; set; } = new();
    private AccountDetailsDto? SelectedTransferCardAccount { get; set; }
    private DataItem<HoldDetailsDTO> HoldList { get; set; } = new();
    private IEnumerable<HoldDetailsListDto> SelectedHold { get; set; } = new List<HoldDetailsListDto>();

    protected override async Task OnInitializedAsync()
    {

        //Notification.Loading($"Loading data...");

        if (TaskDetail is not null)
            await BindTaskDetail();
        else
            await PrepareRequestForm();

        Notification.Hide();

    }

    async Task PrepareRequestForm()
    {


        if (!IsAuthorized)
        {
            await Listen.InvokeAsync(new() { IsAccessDenied = true });
            return;
        }


        Model = new() { RequestIdString = SelectedCard?.RequestId.ToString() ?? "" };

        BindFormEditContext(Model);

        if (Enum.TryParse(SelectedCard.Parameters?.Collateral, out Collateral _collateral))
            collateral = _collateral;

        EnableKfhSalary = SelectedCard.ProductType == ProductTypes.Tayseer;
        EnableTemporary = !(SelectedCard.IsCorporateCard);
        EnableTayseerCreditChecking = EnableKfhSalary && collateral is Collateral.AGAINST_SALARY or Collateral.EXCEPTION;

        ChangeLimitHistory.Loading();
        Supplementary.Loading();

        var historyResponse = ChangeLimitAppService.GetChangeLimitHistory(SelectedCard.RequestId);
        var supplementary = CardDetailsAppService.GetSupplementaryCardsByRequestId(SelectedCard.RequestId);

        List<Task> taskList = new() { historyResponse, supplementary };

        //If it is margin collateral
        if (collateral is Collateral.AGAINST_MARGIN)
        {
            CardAccounts.Loading();
            taskList.Add(AccountService.GetMarginAccounts(SelectedCard.CivilId));
        }

        //If it is deposit collateral
        if (collateral is Collateral.AGAINST_DEPOSIT or Collateral.AGAINST_DEPOSIT_USD)
        {
            CardAccounts.Loading();
            taskList.Add(AccountService.GetDepositAccounts(SelectedCard.CivilId));
        }

        await WatchCompletedTask(taskList);

        await ReadyForAction.InvokeAsync(true);
    }
    public async Task<bool> SubmitRequest(CancellationToken? cancellationToken = default)
    {
        if (IsHavingPendingRequest)
        {
            Notification.Failure(GlobalResources.NotAllowedToAddLimitChangeRequest);
            return false;
        }

        if (!await IsValidForm())
        {
            return false;
        }

        await Listen.NotifyStatus(DataStatus.Processing, Title: "Card Limit", Message: $"Requesting card limit change");

        if (collateral is Collateral.AGAINST_DEPOSIT or Collateral.AGAINST_DEPOSIT_USD && SelectedHold is not null)
        {
            Model.SelectedHold = SelectedHold.FirstOrDefault();
        }


        var response = await ChangeLimitAppService.RequestChangeLimit(Model);
        await Listen.HandleApiResponse(response, formEditContext, formMessageStore);

        return response.IsSuccess;

        async Task<bool> IsValidForm()
        {
            if (formEditContext is null) return false;

            if (EnableKfhSalary && Model.KFHSalary <= 0)
            {
                formEditContext.AddAndNotifyFieldError(formMessageStore!, () => Model.KFHSalary!, "KFH salary is required", true);
            }

            if (EnableTayseerCreditChecking && (Model.CinetSalary is null || Model.CinetSalary <= 0))
            {
                formEditContext.AddAndNotifyFieldError(formMessageStore!, () => Model.CinetSalary!, "Cinet salary is required", true);
            }

            if (EnableTayseerCreditChecking && (Model.CinetInstallment is null || Model.CinetInstallment <= 0))
            {
                formEditContext.AddAndNotifyFieldError(formMessageStore!, () => Model.CinetInstallment!, "Cinet Installment is required", true);
            }

            if (decimal.TryParse(SelectedCard?.MinLimit, out decimal _minLimit))
            {
                if (Model.NewLimit < _minLimit)
                    formEditContext.AddAndNotifyFieldError(formMessageStore!, () => Model.NewLimit!, $"Please enter the amount greater than {_minLimit} {SelectedCard?.Currency?.CurrencyIsoCode}", true);
            }

            if (decimal.TryParse(SelectedCard?.MaxLimit, out decimal _maxLimit))
            {
                if (Model.NewLimit > _maxLimit)
                    formEditContext.AddAndNotifyFieldError(formMessageStore!, () => Model.NewLimit!, $"Please enter the amount less than {_maxLimit}  {SelectedCard?.Currency?.CurrencyIsoCode}", true);
            }

            // Is not valid or not modified
            if (!await IsFormValid())
                return false;

            return await Task.FromResult(true);
        }

    }

    async Task BindTaskDetail()
    {


    }

    public async Task ProcessAction(ActionType actionType, string ReasonForRejection)
    {
        if (TaskDetail is null) return;


        var result = await ChangeLimitAppService.ProcessChangeLimitRequest(new()
        {
            ReasonForRejection = ReasonForRejection,
            ActionType = actionType,
            RequestActivityId = RequestActivity!.RequestActivityId,
            TaskId = TaskDetail?.Id,
            WorkFlowInstanceId = TaskDetail?.InstanceId
        });
        await Listen.NotifyStatus(data: result); ;
    }



    async Task WatchCompletedTask(List<Task> taskList)
    {
        while (taskList.Any())
        {
            Task completedTask = await Task.WhenAny(taskList);
            taskList.Remove(completedTask);

            switch (completedTask)
            {
                case Task<ApiResponseModel<List<ChangeLimitHistoryDto>>>:
                    if (ChangeLimitHistory is null)
                        break;

                    await BindDataItemFromTask(completedTask, ChangeLimitHistory);

                    IsHavingPendingRequest = ChangeLimitHistory?.Data?.Any(x => x.Status == ChangeLimitStatus.PENDING.ToString()) ?? false;
                    if (IsHavingPendingRequest)
                        Notification.Failure(GlobalResources.NotAllowedToAddLimitChangeRequest);

                    break;

                case Task<ApiResponseModel<List<SupplementaryCardDetail>>>:
                    await BindDataItemFromTask(completedTask, Supplementary);
                    break;

                case Task<ApiResponseModel<List<AccountDetailsDto>>>:
                    await BindDataItemFromTask(completedTask, CardAccounts);
                    break;
            }
        }
    }
    async Task BindDataItemFromTask<T>(Task task, DataItem<T> dataItem) where T : class
    {
        try
        {
            var response = await (Task<ApiResponseModel<T>>)task;

            if (response.IsSuccessWithData)
                dataItem.SetData(response.Data!);
            else
                dataItem.Error(new(response.Message));
        }
        catch (Exception)
        {

            throw;
        }


        StateHasChanged();
    }
    async Task OnChangCardAccount(string accountNumber)
    {

        if (SelectedTransferCardAccount?.Acct == accountNumber || string.IsNullOrEmpty(accountNumber)) return;
        SelectedTransferCardAccount = CardAccounts.Data?.FirstOrDefault(x => x.Acct == accountNumber);

        Model.CardAccount = accountNumber;
        if (collateral is Collateral.AGAINST_DEPOSIT or Collateral.AGAINST_DEPOSIT_USD)
        {
            var holdListResponse = await AccountService.GetHoldList(accountNumber);

            if (holdListResponse.IsSuccessWithData)
                HoldList.SetData(holdListResponse.Data!);
            else
                HoldList.Error(new(holdListResponse.Message));

            StateHasChanged();
        }
    }
    async Task OnChangeAction(ListItem<ChangeLimitHistoryDto> item)
    {
        SelectedHistory = item.Value;

        if (item.Text.Equals("More Detail", StringComparison.InvariantCultureIgnoreCase))
            await ChangeLimitHistoryDetail.ToggleAsync();

        if (item.Text.Equals("Cancel", StringComparison.InvariantCultureIgnoreCase))
            await Cancel(SelectedHistory!.Id);

        if (item.Text.Equals("Delete", StringComparison.InvariantCultureIgnoreCase))
            await Delete(SelectedHistory!.Id);
    }
    async Task Delete(decimal Id)
    {
        Notification.Loading("Deleting change limit is in process..");
        var response = await ChangeLimitAppService.DeleteChangeLimit(Id);

        if (response.IsSuccess)
        {
            Notification.Success("Successfully deleted !");
            await PrepareRequestForm();
        }
        else
            Notification.Success($"Failed to delete! {response.Message}");

    }
    async Task Cancel(decimal Id)
    {
        Notification.Loading("Cancelling change limit is in process..");
        var response = await ChangeLimitAppService.CancelChangeLimit(Id, Domain.Enums.ChangeLimitStatus.CANCEL_TEMP_LIMIT);
        if (response.IsSuccess)
        {
            Notification.Success("Successfully Cancelled !");
            await PrepareRequestForm();
        }
        else
            Notification.Success($"Failed to Cancel! {response.Message}");
    }

    public async Task PrintApplication() => await DownloadAfterSalesEForm.InvokeAsync(new()
    {
        HolderName = SelectedCard?.HolderEmbossName,
        NewLimit = Model.NewLimit.ToString(),
        OldLimit = SelectedCard?.ApproveLimit.ToString(),
        IsTemporaryLimitChange = Model.IsTemporary
    });
    public Task Cancel()
    {
        throw new NotImplementedException();
    }
}

