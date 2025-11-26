namespace CreditCardsSystem.Domain.Models.Workflow;

public class InstanceRunRequest
{
    public string WorkflowKey { get; set; } = default!;

    public string InitiatingApplication { get; set; } = default!;

    public string InitiatingUser { get; set; } = default!;

    public Dictionary<string, object> Variables { get; set; } = default!;
}


public class InstanceCancelRequest
{
    public Guid TaskId { get; set; } = default!;
}

public class InstanceCancelResponse
{
    public string Result { get; set; } = default!;
}