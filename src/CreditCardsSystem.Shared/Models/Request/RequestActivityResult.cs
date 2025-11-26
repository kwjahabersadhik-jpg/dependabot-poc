namespace CreditCardsSystem.Domain.Models.Request;
public class RequestActivityResult
{
    public decimal RequestActivityID { get; set; }
    public string IT_DESCRIPTION_AR { get; set; }
    public decimal RequestActivityStatusID { get; set; }
    public DateTime? ArchiveDate { get; set; }
    public DateTime? CreationDate { get; set; }
    public DateTime? LastUpdateDate { get; set; }
    public decimal? BranchID { get; set; }
    public string? BranchName { get; set; }
    public string? CivilID { get; set; }
    public decimal? RequestID { get; set; }
    public string? CustomerName { get; set; }
    public decimal? CFUActivityID { get; set; }
    public decimal? IssuanceTypeID { get; set; }
    public decimal? TellerID { get; set; }
    public decimal? ApproverID { get; set; }
    public string? TellerName { get; set; }
    public string? ApproverName { get; set; }
    public string? CardNo { get; set; }
    public string RequestStatusEn { get; set; }
    public string RequestStatusAr { get; set; }
    public string CA_DESCRIPTION_EN { get; set; }
    public string CA_DESCRIPTION_AR { get; set; }
    public string IT_DESCRIPTION_EN { get; set; }
    public int ReqStatus { get; set; }
}