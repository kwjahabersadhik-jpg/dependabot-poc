using System.ComponentModel.DataAnnotations;

namespace CreditCardsSystem.Domain.Models.BCDPromotions.Groups;

public class PromotionGroupDto
{
    [Display(Name = "Group Id")]
    public long GroupID { get; set; }

    [Display(Name = "Promotion Id")]
    public int PromotionId { get; set; }

    [Display(Name = "Description")]
    public string Description { get; set; }

    [Display(Name = "Status")]
    public string Status { get; set; }

    [Display(Name = "Promotion Name")]
    public string PromotionName { get; set; }

    public bool? IsLocked { get; set; }

}