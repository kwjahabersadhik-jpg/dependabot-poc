using CreditCardsSystem.Domain.Shared.Enums;

namespace CreditCardsSystem.Domain.Models.BCDPromotions.Requests;

public class ApproveRequestDto
{
    public List<RequestActivityDetailsDto> RequestDetails { get; set; } = new();
    public ActivityForm ActivityForm { get; set; }
    public ActivityType ActivityType { get; set; }

}