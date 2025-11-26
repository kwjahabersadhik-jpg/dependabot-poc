using Kfh.Aurora.Workflow.Dto;

namespace CreditCardsSystem.Domain.Models;

public class TaskDto
{
    public string Id { get; set; } = default!;

    public string Title { get; set; } = default!;

    public string Description { get; set; } = default!;

    public string Status { get; set; } = default!;

    public string Type { set; get; } = default!;

    public bool IsCompleted { get; set; }

    public string? Application { get; set; }

    public List<Assignees>? Assignees { get; set; }

    public string? Assignee { get; set; }

    public string? ActionUrl { get; set; }

    public string? Source { get; set; }

    public Dictionary<string, object> Payload { get; set; } = default!;

    public DateTimeOffset CreatedDate { get; set; }

    public DateTimeOffset? CompletedDate { get; set; }

}

