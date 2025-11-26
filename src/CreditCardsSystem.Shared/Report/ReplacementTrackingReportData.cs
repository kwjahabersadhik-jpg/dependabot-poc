
using CreditCardsSystem.Domain.Models.Reports;

namespace CreditCardsSystem.Domain
{
    public class ReplacementTrackingReportData : RebrandDto
    {
        public string CivilId { get; set; }
        public string FdAcctNo { get; set; }
        public string CardNo { get; set; }
        public string HolderName { get; set; }
        public int RecordsCount => Details.Count();
        public IEnumerable<ReplacementTrackingReportDetail> Details { get; set; }
        public FileExtension FileExtension { get; set; }
    }

    public class ReplacementTrackingReportDetail 
    {
        public string AcctNo { get; set; }
        public decimal? BranchId { get; set; }
        public string BranchName { get; set; }
        public string OldCardNumber { get; set; }
        public long? Mobile { get; set; }
        public decimal? TellerId { get; set; }
        public DateTime? CreationDate { get; set; }
    }
}