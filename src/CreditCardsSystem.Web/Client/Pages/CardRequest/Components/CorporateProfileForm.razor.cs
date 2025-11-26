using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Interfaces;
using CreditCardsSystem.Domain.Models.Access;
using CreditCardsSystem.Web.Client.Components;
using Microsoft.AspNetCore.Components;

namespace CreditCardsSystem.Web.Client.Pages.CardRequest.Components;

public partial class CorporateProfileForm : IWorkflowMethods
{
    public bool IsAuthorized { get => IsAllowTo(Permissions.CorporateProfile.EnigmaApprove()); }
    [Inject] ICorporateAppService CorporateAppService { get; set; } = null!;



    protected override async Task OnInitializedAsync()
    {
        await ReadyForAction.InvokeAsync(true);
    }


    public async Task ProcessAction(ActionType actionType, string ReasonForRejection)
    {
        if (!IsAuthorized)
        {
            await Listen.InvokeAsync(new() { IsAccessDenied = true });
            return;
        }


        var result = await CorporateAppService.ProcessProfileRequest(new()
        {
            ReasonForRejection = ReasonForRejection,
            ActionType = actionType,
            RequestActivityId = RequestActivity!.RequestActivityId,
            TaskId = TaskDetail?.Id,
            WorkFlowInstanceId = TaskDetail?.InstanceId,
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
