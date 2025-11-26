namespace CreditCardsSystem.Domain.Models.Promotions;

public class AddPromotionToBeneficiaryRequest
{
    public string CivilId { get; set; } = null!;
    public DateTime ApplicationDate { get; set; }
    public string? Remarks { get; set; }
    public string? PromotionName { get; set; }
    public decimal RequestId { get; set; }
}
