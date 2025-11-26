using System.ComponentModel.DataAnnotations;

namespace CreditCardsSystem.Domain.Models.BCDPromotions.PCTs;

public class PctDto
{
    [Display(Name = "Pct Id")]
    public decimal PctId { get; set; }

    [Display(Name = "Pct Flag")]
    public string PctFlag { get; set; } = null!;

    [Display(Name = "No Of Waved Months")]
    public int NoOfWavedMonths { get; set; }

    [Display(Name = "Creation Date")]
    public DateTime CreateDate { get; set; }

    [Display(Name = "Fees")]
    public decimal Fees { get; set; }

    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Display(Name = "Is Staff")]
    public bool IsStaff { get; set; }

    [Display(Name = "Service Id")]
    public decimal ServiceId { get; set; }

    [Display(Name = "Early Closure Percentage")]
    public decimal EarlyClosurePercentage { get; set; }

    [Display(Name = "Early Closure Fees")]
    public decimal EarlyClosureFees { get; set; }

    [Display(Name = "Early Closure Months")]
    public decimal EarlyClosureMonths { get; set; }

    [Display(Name = "Service No")]
    public int ServiceNo { get; set; }

    public bool? Islocked { get; set; }

}