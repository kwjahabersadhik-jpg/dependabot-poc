using CreditCardsSystem.Domain.Models.Promotions;

namespace CreditCardsSystem.Domain.Shared.Models.BCDPromotions.EligiblePromotions;

public class PromotionCardDto
{
    public int PromotionId { get; set; }

    public int CardType { get; set; }

    public decimal? PctId { get; set; }

    public int? Collateralid { get; set; }

    public bool? Islocked { get; set; }

    public int PromotionCardId { get; set; }
    public PromotionDto Promotion { get; set; } = null!;


}