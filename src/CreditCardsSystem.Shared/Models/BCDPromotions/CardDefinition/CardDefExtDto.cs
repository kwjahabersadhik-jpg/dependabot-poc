using System.ComponentModel.DataAnnotations;

namespace CreditCardsSystem.Domain.Shared.Models.BCDPromotions.CardDefinition
{
    public class CardDefExtDto
    {
        public string ExtensionId { get; set; }

        public int CardType { get; set; }

        [StringLength(50)]
        public string Attribute { get; set; } = null!;

        [StringLength(100)]
        public string? Value { get; set; }

    }
}
