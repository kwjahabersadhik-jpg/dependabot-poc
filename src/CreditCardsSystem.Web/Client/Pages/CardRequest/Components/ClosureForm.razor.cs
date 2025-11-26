using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Interfaces;
using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Models.Access;
using CreditCardsSystem.Domain.Models.CardIssuance;
using CreditCardsSystem.Domain.Models.CardOperation;
using CreditCardsSystem.Domain.Models.Workflow;
using CreditCardsSystem.Web.Client.Components;
using Microsoft.AspNetCore.Components;

namespace CreditCardsSystem.Web.Client.Pages.CardRequest.Components;

public partial class ClosureForm : IWorkflowMethods
{
    private bool IsAuthorized => IsAllowTo(TaskDetail is not null ? Permissions.CardClosure.EnigmaApprove() : Permissions.CardClosure.Request());

    [Inject] IClosureAppService closureAppService { get; set; } = null!;



    DataItem<ValidateCardClosureResponse> formDataItem { get; set; } = new();
    ValidateCardClosureResponse? formData => formDataItem?.Data;
    DataItem<List<AccountDetailsDto>> DebitAccounts { get; set; } = new();

    string? CardAccountNumber { get; set; }





    //List<AccountDetailsDto>? debitAccounts;

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
    }

    async Task PrepareRequestForm()
    {
        if (!IsAuthorized)
        {
            await Listen.InvokeAsync(new() { IsAccessDenied = true });
            Notification.Hide();
            return;
        }

        CardAccountNumber = SelectedCard!.BankAccountNumber;

        await GetClosureFormData(SelectedCard!.RequestId);

        await ReadyForAction.InvokeAsync(formDataItem.Status == DataStatus.Success);
    }

    public async Task<bool> SubmitRequest(CancellationToken? cancellationToken = default)
    {

        await Listen.NotifyStatus(DataStatus.Processing, Title: "Card closure", Message: $"Card closure is in process");
        var response = await closureAppService.RequestCardClosure(new()
        {
            RequestId = SelectedCard!.RequestId,
            BranchId = ConfigurationBase.BranchId,
            AccountNumber = CardAccountNumber!
        });
        await Listen.NotifyStatus(data: response);

        return response.IsSuccess;
    }

    public async Task ProcessAction(ActionType actionType, string ReasonForRejection)
    {
        if (TaskDetail is not null)
        {
            var result = await closureAppService.ProcessCardClosureRequest(new()
            {
                ReasonForRejection = ReasonForRejection,
                ActionType = actionType,
                RequestActivityId = RequestActivity!.RequestActivityId,
                TaskId = TaskDetail?.Id,
                WorkFlowInstanceId = TaskDetail?.InstanceId,
                AccountNumber = TaskDetail!.Payload.GetValueOrDefault(WorkflowVariables.AccountNumber)?.ToString() ?? ""
            });
            await Listen.NotifyStatus(data: result); ;
        }
    }


    private AccountDetailsDto? SelectedDebitAccount { get; set; }
    async Task GetClosureFormData(decimal requestId)
    {
        formDataItem.Loading();
        DebitAccounts.Loading();
        await SetActionMessage.InvokeAsync(new() { ProcessStatus = DataStatus.Processing, Message = "Calculating due / refund amount..." });
        var requestFormResult = await closureAppService.GetCardClosureRequestFormData(new()
        {
            BranchId = ConfigurationBase.BranchId,
            IncludeValidation = false,
            RequestId = requestId,
            AccountNumber = CardAccountNumber ?? ""
        });

        if (requestFormResult.IsSuccess)
        {
            formDataItem.SetData(requestFormResult.Data!);
            DebitAccounts.SetData(requestFormResult.Data!.DebitAccounts ?? []);
        }
        else
        {
            formDataItem.Error(new(requestFormResult.Message));
            DebitAccounts.Error(new(requestFormResult.Message));
        }

        await SetActionMessage.InvokeAsync(new()
        {
            //Action = EventCallback.Factory.Create<ActionMessageStatus>(this, GetClosureFormData(requestId)),
            ProcessStatus = formDataItem.Status,
            Message = formDataItem.Exception?.Message ?? ""
        });


        StateHasChanged();
    }
    async Task OnChangeCardAccountNumber(string accountNumber)
    {
        CardAccountNumber = accountNumber;
        SelectedDebitAccount = formData?.DebitAccounts?.FirstOrDefault(x => x.Acct == CardAccountNumber);
        if (SelectedDebitAccount is null)
            return;


        bool inSufficientBalance = SelectedDebitAccount.AvailableBalance < formData!.TotalFee;
        if (inSufficientBalance)
            Notification.Failure("The customer has no enough balance for the selected debit account");

        await ReadyForAction.InvokeAsync(!inSufficientBalance);

    }



    public async Task PrintApplication() => await DownloadAfterSalesEForm.InvokeAsync(new() { HolderName = SelectedCard?.HolderEmbossName });
    public Task Cancel() => throw new NotImplementedException();
}

