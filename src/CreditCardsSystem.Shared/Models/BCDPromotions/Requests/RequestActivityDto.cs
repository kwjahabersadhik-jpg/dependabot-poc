using CreditCardsSystem.Domain.Shared.Enums;

namespace CreditCardsSystem.Domain.Models.BCDPromotions.Requests;

public class RequestActivityDto
{
    public long RequestActivityId { get; set; }
    public int ActivityFormId { get; set; }
    public int ActivityTypeId { get; set; }
    public long ActivityStatusId { get; set; }
    public DateTime CreationDate { get; set; }
    public DateTime LastUpdateDate { get; set; }
    public long MakerId { get; set; }
    public long CheckerId { get; set; }
    public string MakerName { get; set; } = string.Empty;
    public string CheckerName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public bool? IsLocked { get; set; }
    public ActivityForm ActivityForm => (ActivityForm)ActivityFormId;
    public string ActivityFormName => ActivityForm.ToString();
    public ActivityType ActivityType => (ActivityType)ActivityTypeId;
    public ActivityStatus ActivityStatus => (ActivityStatus)ActivityStatusId;
    public string ActivityTypeName => ActivityType.ToString();
    public string ActivityStatusName => ActivityStatus.ToString();
    public List<RequestActivityDetailsDto> RequestActivityDetails { get; set; } = new();

}