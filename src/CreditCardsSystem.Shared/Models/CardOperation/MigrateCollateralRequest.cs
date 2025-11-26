using CreditCardsSystem.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace CreditCardsSystem.Domain.Models.CardOperation;

public class MigrateCollateralRequest
{
    [Required(ErrorMessage = "Please enter RequestId")]
    public decimal RequestId { get; set; }

    [Range(typeof(bool), "true", "true", ErrorMessage = "Please confirm the seller name")]
    public bool IsConfirmedSellerId { get; set; }

    [Required(ErrorMessage = "Please enter SellerId and confirm seller name")]
    public long? SellerId { get; set; } = null;
    public string? DebitAccountNumber { get; set; }

    [Required(ErrorMessage = "Please choose Collateral")]
    public Collateral? Collateral { get; set; }

    [Required(ErrorMessage = "Please select account number")]
    public string CollateralAccountNumber { get; set; } = string.Empty;
    public int CollateralAmount { get; set; }
    public string CollateralNumber { get; set; } = string.Empty;
}
public class MigrateCollateralResponse
{ }

