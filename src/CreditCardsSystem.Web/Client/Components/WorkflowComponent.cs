using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Models.Card;
using CreditCardsSystem.Domain.Models.Reports;
using CreditCardsSystem.Domain.Models.RequestActivity;
using CreditCardsSystem.Domain.Shared.Models.RequestActivity;
using Kfh.Aurora.Workflow.Dto;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace CreditCardsSystem.Web.Client.Components;

public class WorkflowComponent : ApplicationComponent, IDisposable
{
    [Parameter]
    public string? RejectionReason { get; set; }

    [Parameter]
    public CardDetailsResponse? SelectedCard { get; set; } = default!;

    [Parameter]
    public RequestActivityDto RequestActivity { get; set; } = default!;

    [Parameter]
    public EventCallback<bool> ReadyForAction { get; set; }

    [Parameter]
    public EventCallback<ActionStatus> Listen { get; set; }

    [Parameter]
    public EventCallback<AfterSalesForm> DownloadAfterSalesEForm { get; set; }

    [Parameter]
    public TaskResult? TaskDetail { get; set; }

    [Parameter]
    public IEnumerable<PendingRequest>? PendingRequests { get; set; }

    [Parameter]
    public EventCallback<ActionMessageStatus> SetActionMessage { get; set; }

    public EditContext? formEditContext { get; set; }

    public ValidationMessageStore? formMessageStore { get; set; }

    public void Dispose()
    {
        UnBindFormEditContext();
    }

    public void BindFormEditContext<T>(T Model) where T : class, new()
    {
        Model ??= new();
        formEditContext = new EditContext(Model);
        formMessageStore = new ValidationMessageStore(formEditContext);
        formEditContext.OnValidationRequested += (s, e) => formMessageStore?.Clear();
        formEditContext.OnFieldChanged += (s, e) => formMessageStore?.Clear(e.FieldIdentifier);
    }
    public void UnBindFormEditContext()
    {
        if (formEditContext == null) return;

        formEditContext.OnValidationRequested -= (s, e) => formMessageStore?.Clear();
        formEditContext.OnFieldChanged -= (s, e) => formMessageStore?.Clear(e.FieldIdentifier);
    }

    public async Task<bool> IsFormValid(bool isEdit = false)
    {

        if (isEdit && !formEditContext.IsModified())
        {
            Notification.Failure(GlobalResources.NoChanges);
            return false;
        }
        // Is not valid or not modified
        if (!formEditContext.Validate())
        {
            Notification.Failure(GlobalResources.InvalidInput);
            return false;
        }

        return await Task.FromResult(true);
    }

}


