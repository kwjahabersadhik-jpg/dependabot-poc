using Kfh.Aurora.Blazor.Components.ViewModels.ListTiles;

namespace CreditCardsSystem.Web.Client.Components.Tasks.Model;

public class TaskListTileViewModel
{
    public required Guid Id { get; set; }
    public required string ApplicationName { get; set; }
    public required Uri ActionUrl { get; set; }
    public string Assignee { get; set; } = string.Empty;
    public DateTimeOffset CreatedDate { get; set; }

    public DateTimeOffset? CompletedDate { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsCompleted { get; set; } = false;
    public Dictionary<string, object> Payload { get; set; } = new();
    public string Source { get; set; } = string.Empty;
    public List<TaskAssigneeViewModel> Assignees { get; set; } = new();
    public string? ApplicationIcon { get; set; }
    public Guid InstanceId { get; internal set; }
}