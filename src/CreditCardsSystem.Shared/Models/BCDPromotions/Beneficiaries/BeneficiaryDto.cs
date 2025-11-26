namespace CreditCardsSystem.Domain.Shared.Models.BCDPromotions.Beneficiaries
{
    public class BeneficiaryDto
    {
        public string CivilId { get; set; } = string.Empty;

        public int PromotionId { get; set; }

        public string CardNo { get; set; } = string.Empty;

        public DateTime ApplicationDate { get; set; }

        public string? Remarks { get; set; }

        public string PromotionName { get; set; } = string.Empty;

    }
}
