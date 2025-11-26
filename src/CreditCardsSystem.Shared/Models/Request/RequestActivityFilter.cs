using CreditCardsSystem.Domain.Enums;

namespace CreditCardsSystem.Domain.Models.Request;

public class RequestActivityFilter
{
    public string? CardNumber { get; set; }
    public string? CustomerCivilId { get; set; }
    public int? SellerId { get; set; }
    public decimal? ApproverId { get; set; }

    public decimal? RequestId { get; set; }
    public decimal? RequestActivityID { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public CFUActivity? CFUActivity { get; set; }
    public RequestActivityStatus? Status { get; set; }
    public bool FilterAll { get; set; } = false;
    public Dictionary<CFUActivity, string>? Parameters { get; set; }
    public List<CFUActivity>? CFUActivities { get; set; }

    public int? TellerId { get; set; }
    public int? BranchId { get; set; }

}
