using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Models.CreditCards;
using System.Text.Json.Serialization;

namespace CreditCardsSystem.Domain.Models.SupplementaryCard;

public class SupplementaryCardDetail
{
    public string CardType { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
    public decimal SourceRequestId { get; set; }

    [JsonIgnore]
    public string? CardNumber { get; set; }

    public decimal? RequestId { get; set; }
    public string? FullName { get; set; }
    public decimal StatusId { get; set; }
    public string? Description { get; set; }
    public CreditCardResponse? CardData { get; set; }
    public string? CivilId { get; set; }
    public CreditCardStatus CardStatus { get; set; }
    public string? ExternalStatus { get; set; }
    public decimal TypeId { get; set; }
    public string HolderName { get; set; }
    public string Relation { get; set; }
    public string CardNumberDto { get; set; }
}


