namespace CreditCardsSystem.Domain.Models.CardIssuance;

public class EligibleCardResponse
{
    public List<RequestDto>? HsPendingRequest { get; set; }
    public List<CardEligiblityMatrixDto> EligibleCards { get; set; } = default!;
}
