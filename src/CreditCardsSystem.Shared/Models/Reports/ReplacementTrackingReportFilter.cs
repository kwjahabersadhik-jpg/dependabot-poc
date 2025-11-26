using CreditCardsSystem.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace CreditCardsSystem.Domain.Models.Reports;

public class ReplacementTrackingReportFilter : ValidateModel<ReplacementTrackingReportFilter>
{

    [Required]
    public long RequestId { get; set; }

    public FileExtension FileExtension { get; set; } = FileExtension.pdf;


}
