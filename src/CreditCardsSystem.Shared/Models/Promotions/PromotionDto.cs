using System.ComponentModel.DataAnnotations;

namespace CreditCardsSystem.Domain.Models.Promotions;


public class PromotionDto
{
    [Display(Name = "Promotion Id")]
    public int PromotionId { get; set; }

    [Display(Name = "Promotion Name")]
    public string PromotionName { get; set; } = null!;

    [Display(Name = "Start Date")]
    public DateTime? StartDate { get; set; }

    [Display(Name = "End Date")]
    public DateTime? EndDate { get; set; }

    [Display(Name = "Description")]
    public string? PromotionDescription { get; set; }

    [Display(Name = "Status")]
    public string Status { get; set; } = null!;

    [Display(Name = "Usage Flag")]
    public string UsageFlag { get; set; } = null!;

    public bool? IsLocked { get; set; }

    public TimeSpan? Duration { get; set; }

}
