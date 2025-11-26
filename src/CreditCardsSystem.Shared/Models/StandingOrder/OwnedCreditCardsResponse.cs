using CreditCardsSystem.Domain.Enums;
using System.Text.Json.Serialization;

namespace CreditCardsSystem.Domain.Models.StandingOrder;

public class OwnedCreditCardsResponse
{
    [JsonIgnore]
    public string? CardNumber { get; set; } = null!;
    public string CardNumberDto { get; set; } = null!;
    public string ProductName { get; set; } = null!;
    public int CardType { get; set; }
    public decimal RequestId { get; set; }
    public CreditCardStatus CardStatus { get; set; }
    public decimal ApprovedLimit { get; set; }
    public decimal DueAmount { get; set; }
    public string? ExternalStatus { get; set; }
    public string? DebitAccountNumber { get; set; }
    public string? CurrencyISOCode { get; set; }
}
