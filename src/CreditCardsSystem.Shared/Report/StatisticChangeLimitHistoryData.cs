


namespace CreditCardsSystem.Domain.Models.Reports;

public class StatisticalChangeLimitHistoryData
{
    public decimal Id { get; set; }
    public string ReqId { get; set; }
    public decimal OldLimit { get; set; }
    public decimal NewLimit { get; set; }
    public string IsTempLimitChange { get; set; }
    public DateTime LogDate { get; set; }
    public string Status { get; set; }
    public string RefuseReason { get; set; }
    public string InitiatorId { get; set; }
    public string ApproverId { get; set; }
    public DateTime? ApproveDate { get; set; }
    public DateTime? RejectDate { get; set; }
    public decimal? PurgeDays { get; set; }
    public string CivilId { get; set; }
    public string CardNo { get; set; }
    public string ChangeType { get; set; }
}