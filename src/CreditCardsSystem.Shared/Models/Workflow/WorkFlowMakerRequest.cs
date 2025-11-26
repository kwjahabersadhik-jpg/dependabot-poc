using System.ComponentModel.DataAnnotations;

namespace CreditCardsSystem.Domain.Models.Workflow;

public class WorkFlowMakerRequest
{
    public WorkFlowKey WorkFlowKey { get; set; }
    public decimal RequestActivityId { get; set; }
    public Dictionary<string, object>? Variables { get; set; }
}


public class ReturnToMakerRequest
{
    public Guid? TaskId { get; set; }
    public Guid? InstanceId { get; set; }

    [RegularExpression(@"^[a-zA-Z\d\s]{1,200}$", ErrorMessage = "Invalid Comments")]
    public string? Comments { get; set; }
}
