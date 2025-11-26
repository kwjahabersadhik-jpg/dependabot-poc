namespace CreditCardsSystem.Domain.Models.BCDPromotions.Requests;

public class RequestActivityDetailsDto
{
    public long Id { get; set; }
    public long RequestActivityId { get; set; }
    public string Parameter { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}