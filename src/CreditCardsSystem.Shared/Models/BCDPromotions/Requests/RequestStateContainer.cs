namespace CreditCardsSystem.Domain.Models.BCDPromotions.Requests;

public class RequestStateContainer
{
    public RequestActivityDto RequestHeader { get; set; } = new();
    public List<RequestActivityDetailsDto> RequestDetails { get; set; } = new();
    public string UserId { get; set; } = string.Empty;
    public string DeletionReason { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;


}