using Kfh.Aurora.Blazor.Components.ViewModels.ListTiles;
using Kfh.Aurora.Workflow.Dto;

namespace CreditCardsSystem.Web.Client.Components.Tasks.Model
{
    public static class TaskViewModelExtensions
    {
        public static List<TaskListTileViewModel> Convert(this List<TaskResult> tasksDtos)
        {
            List<TaskListTileViewModel> vms = new List<TaskListTileViewModel>();
            tasksDtos.ForEach(e =>
            {
                vms.Add(new TaskListTileViewModel()
                {
                    Id = e.Id,
                    InstanceId = e.InstanceId,
                    ActionUrl = new(e.ActionUrl!),
                    ApplicationName = e.Application,
                    CreatedDate = e.CreatedDate.DateTime,
                    Assignee = e.Assignee ?? "",
                    Status = e.Status,
                    Assignees = e.Assignees.Select(e => new TaskAssigneeViewModel(e.Type, e.Value)).ToList(),
                    Type = e.Type,
                    Source = e.Source,
                    Payload = e.Payload,
                    CompletedDate = e.CompletedDate?.DateTime,
                    IsCompleted = e.IsCompleted,
                    Title = e.Title,
                    Description = e.Description
                });
            });
            return vms;
        }
    }
}
