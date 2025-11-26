using CreditCardsSystem.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace CreditCardsSystem.Domain.Models.Reports;

public class SingleReportFilter : ValidateModel<SingleReportFilter>
{

    [Required]
    public DateTime Period { get; set; } = DateTime.Now;

    [Required]
    public string CardNumber { get; set; }
}
