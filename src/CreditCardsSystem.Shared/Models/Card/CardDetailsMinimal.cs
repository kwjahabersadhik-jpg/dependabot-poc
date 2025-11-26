using CreditCardsSystem.Domain.Enums;
using System.Text.Json.Serialization;

namespace CreditCardsSystem.Domain.Models.Card;

public class CardDetailsMinimal
{
    public string CivilId { get; set; } = null!;
    public int CardType { get; set; }

    [JsonIgnore]
    public string CardNumber { get; set; } = null!;

    public string CardNumberDto { get; set; } = null!;
    public decimal RequestId { get; set; }
    public ProductTypes ProductType { get; set; }
    public IssuanceTypes IssuanceType { get; set; }
    public string ProductName { get; set; }
    public string? BankAccountNumber { get; set; }
    public bool IsPrimaryCard { get; set; }
    public CreditCardStatus CardStatus { get; set; }
    public decimal ApprovedLimit { get; set; }
    public bool IsCorporateCard { get; set; }

    [JsonIgnore]
    public string? AUBCardNumber { get; set; }
    public string? AUBCardNumberDto { get; set; }
    public bool IsSupplementary { get; set; }
}