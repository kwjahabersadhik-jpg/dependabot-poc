using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Interfaces;
using CreditCardsSystem.Web.Client.Components;
using Microsoft.AspNetCore.Components;

namespace CreditCardsSystem.Web.Client.Pages.CardRequest.Components
{
    public partial class MembershipRequestForm : IWorkflowMethods
    {
        [Inject] IMemberShipAppService MemberShipAppService { get; set; } = null!;

        string ApprovalReason { get; set; } = string.Empty;

        protected override async Task OnInitializedAsync()
        {
            //Notification.Loading($"Loading data...");
            await BindTaskDetail();
            Notification.Hide();
        }
        private async Task BindTaskDetail()
        {

            await Task.CompletedTask;
        }
        public async Task ProcessAction(ActionType actionType, string ReasonForRejection = "")
        {
            var processResponse = await MemberShipAppService.ProcessMembershipDeleteRequest(new()
            {
                WorkFlowInstanceId = TaskDetail!.InstanceId,
                TaskId = TaskDetail!.Id,
                Activity = CFUActivity.MemberShipDeleteRequest,
                ActionType = actionType,
                RequestActivityId = 0,
                ApproverReason = ApprovalReason,
                ReasonForRejection = ReasonForRejection
            });

            Notification.Hide();

            if (processResponse.IsSuccess)
            {
                await Listen.NotifyStatus(processResponse);
                return;
            }
            else
            {
                await Listen.NotifyStatus(processResponse);
            }
        }
        public Task Cancel() => throw new NotImplementedException();
        public Task PrintApplication() => throw new NotImplementedException();
        public Task<bool> SubmitRequest(CancellationToken? cancellationToken = default) => throw new NotImplementedException();
    }

}