namespace CreditCardsSystem.Domain.Models.CreditReverse;


public class CreditReverseDto
{
    public Int64 ID { get; set; }
    public Int64 RequestID { get; set; }
    public string? CardNo { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal AmountKDMaker { get; set; }
    public decimal AmountKDChecker { get; set; }
    public decimal RateMaker { get; set; }
    public decimal? RateChecker { get; set; }
    public int? RequestedBy { get; set; }
    public int? ApprovedBy { get; set; }
    public string? RequestorReason { get; set; } = string.Empty;
    public string? ApproverReason { get; set; } = string.Empty;
    public DateTime? RequestDate { get; set; }
    public DateTime? AppoveDate { get; set; }
    public DateTime? RejectDate { get; set; }
    public int? Status { get; set; }
    public string AccountNo { get; set; } = string.Empty;
    public decimal? ApproveLimit { get; set; }
    public int? CardType { get; set; }
    public string CivilID { get; set; } = string.Empty;
    public string? CustomerNameEn { get; set; } = string.Empty;
    public string? CustomerNameAr { get; set; } = string.Empty;
    public string? CardCurrency { get; set; } = string.Empty;
    public bool IsLocked { get; set; } = false;
}
