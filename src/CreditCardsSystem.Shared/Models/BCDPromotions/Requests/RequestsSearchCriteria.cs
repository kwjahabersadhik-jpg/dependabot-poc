using CreditCardsSystem.Domain.Shared.Enums;

namespace CreditCardsSystem.Domain.Models.BCDPromotions.Requests;

public class RequestsSearchCriteria
{
    public ActivityStatus ActivityStatus { get; set; }
    public ActivityForm ActivityForm { get; set; }
    public ActivityType ActivityType { get; set; }
    public int MakerID { get; set; }
    public long RequestId { get; set; }
}