using System.ComponentModel.DataAnnotations;

namespace CreditCardsSystem.Domain.Models.BCDPromotions.Services;

public class ServiceDto
{

    [Display(Name = "Service Id")]
    public long ServiceId { get; set; }

    [Display(Name = "Service No")]
    public int ServiceNo { get; set; }

    [Display(Name = "No Of Months")]
    public int NoOfMonths { get; set; }

    [Display(Name = "Description")]
    public string ServiceDescription { get; set; } = string.Empty;

    public bool? IsLocked { get; set; }
}