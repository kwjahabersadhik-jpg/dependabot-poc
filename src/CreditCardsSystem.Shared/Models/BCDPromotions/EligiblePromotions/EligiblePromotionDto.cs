using System.ComponentModel.DataAnnotations;

namespace CreditCardsSystem.Domain.Shared.Models.BCDPromotions.EligiblePromotions;

public class EligiblePromotionDto
{
    [Display(Name = "Promotion Id")]
    public int PromotionID { get; set; }

    [Display(Name = "Promotion Name")]
    [RegularExpression(@"^[a-zA-Z\d\s]{1,250}$", ErrorMessage = "Invalid PromotionName")]
    public string PromotionName { get; set; } = string.Empty;

    [Display(Name = "Card Type Id")]
    public int CardType { get; set; }

    [Display(Name = "Card Type Name")]
    [RegularExpression(@"^[a-zA-Z\d\s]{1,50}$", ErrorMessage = "Invalid CardTypeName")]
    public string CardTypeName { get; set; } = string.Empty;

    [Display(Name = "Pct Id")]
    public decimal PCTID { get; set; }

    [Display(Name = "Pct Name")]
    [RegularExpression(@"^[a-zA-Z\d\s]{1,150}$", ErrorMessage = "Invalid PCTName")]
    public string PCTName { get; set; } = string.Empty;

    [Display(Name = "Collateral Id")]
    public int CollateralID { get; set; }

    [Display(Name = "Collateral Name")]
    [RegularExpression(@"^[a-zA-Z\d\s]{1,100}$", ErrorMessage = "Invalid CollateralName")]
    public string CollateralName { get; set; } = string.Empty;

    [Display(Name = "Promotion Card Id")]
    public long PromotionCardId { get; set; }

    public bool? IsLocked { get; set; }


}