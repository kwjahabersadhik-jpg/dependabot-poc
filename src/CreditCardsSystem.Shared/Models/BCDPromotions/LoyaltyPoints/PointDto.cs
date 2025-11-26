namespace CreditCardsSystem.Domain.Models.BCDPromotions.LoyaltyPoints;

public class PointDto
{
    public decimal Id { get; set; }

    public decimal LocalPoints { get; set; }

    public decimal InternationalPoints { get; set; }

    public decimal? CostPerPoint { get; set; }

    public decimal? LocalPointsTemp { get; set; }

    public decimal? InternationalPointsTemp { get; set; }

    public decimal? CostPerPointTemp { get; set; }

    public decimal MakerId { get; set; }

    public decimal? CheckerId { get; set; }

    public DateTime MakedOn { get; set; }

    public DateTime? CheckedOn { get; set; }
}