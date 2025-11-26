using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Interfaces;
using CreditCardsSystem.Domain.Models.Access;
using CreditCardsSystem.Utility.Extensions;
using CreditCardsSystem.Web.Client.Components;
using Microsoft.AspNetCore.Components;

namespace CreditCardsSystem.Web.Client.Pages.CardRequest.Components;

public partial class ReActivationForm : IWorkflowMethods
{


    private bool IsAuthorized => IsAllowTo(TaskDetail is not null ? Permissions.CardReActivate.EnigmaApprove() : Permissions.CardReActivate.Request());


    [Inject] IActivationAppService ActivationAppService { get; set; } = null!;





    protected override async Task OnInitializedAsync()
    {
    }


    public async Task ProcessAction(ActionType actionType, string ReasonForRejection)
    {
        if (!IsAuthorized)
        {
            await Listen.InvokeAsync(new() { IsAccessDenied = true });
            return;
        }

        await Listen.NotifyStatus(DataStatus.Processing, Title: "Card re-activate", Message: $"Card re-activation {actionType.GetDescription()} is in process");

        var result = await ActivationAppService.ProcessCardReActivationRequest(new()
        {
            ReasonForRejection = ReasonForRejection,
            ActionType = actionType,
            RequestActivityId = RequestActivity!.RequestActivityId,
            TaskId = TaskDetail?.Id,
            WorkFlowInstanceId = TaskDetail?.InstanceId
        });

        await Listen.NotifyStatus(result); ;
    }

    public async Task<bool> SubmitRequest(CancellationToken? cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task PrintApplication()
    {
        throw new NotImplementedException();
    }


    public Task Cancel()
    {
        throw new NotImplementedException();
    }
}
