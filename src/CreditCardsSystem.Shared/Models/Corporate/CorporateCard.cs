using CreditCardsSystem.Domain.Enums;
using System.Text.Json.Serialization;

namespace CreditCardsSystem.Domain.Models.Corporate;

public class CorporateCard
{
    public decimal RequestId { get; set; }
    public int RequestStatus { get; set; }
    public int CardType { get; set; }

    [JsonIgnore]
    public string? CardNumber { get; set; } = null!;

    public string? CardNumberDto { get; set; } = null!;

    public string? CardExpiry { get; set; } = null!;
    public string BankAccountNumber { get; set; } = null!;
    public string CivilId { get; set; } = null!;
    public string? FixedDepositAccountNumber { get; set; } = null!;
    public DateTime RequestDate { get; set; }
    public int BranchId { get; set; }
    public string CurrencyISO { get; set; }
    public string ProductType { get; set; }
    public CardCategoryType CardCategory { get; set; }
    public string AccountNumber { get; set; }
    public decimal CardLimit { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public DateTime OpenedDate { get; set; }
    public int StatusId { get; set; }
    public string Status { get; set; }
    public int? HoldAmount { get; set; }
    public decimal ApprovedLimit { get; set; }
    public string MemberShipId { get; set; }
    public string Collateral { get; set; }
    public string BranchName { get; set; }
    public long? MobileNumber { get; set; }
}
