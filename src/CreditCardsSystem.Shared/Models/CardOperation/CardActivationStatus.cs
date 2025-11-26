using CreditCardsSystem.Domain.Models.SupplementaryCard;

namespace CreditCardsSystem.Domain.Models.CardOperation
{
    public class CardActivationStatus
    {
        public string CardNumber { get; set; } = default!;
        public bool? IsActivated { get; set; } = default!;
        public string? Message { get; set; } = default!;
    }

    public class SupplementaryCardUpdateStatus
    {
        public decimal? RequestId { get; set; } = default!;
        public bool IsUpdated { get; set; } = false;
        public string? Message { get; set; } = default!;
    }

    public class SupplementaryOnboardingStatus
    {
        public decimal? RequestId { get; set; } = default!;
        public bool IsOnboarderd { get; set; } = false;
        public string? Message { get; set; } = default!;
        public string? SupplementaryCardNumber { get; set; }
        public SupplementaryCardDetail CardDetail { get; set; }
    }
}
