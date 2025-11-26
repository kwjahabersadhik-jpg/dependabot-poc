using CreditCardsSystem.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace CreditCardsSystem.Domain.Models.CardIssuance;

public class ValidateCurrencyRequest : ValidateModel<ValidateCurrencyRequest>
{
    [Required]
    [StringLength(3, ErrorMessage = "{0} should not exceed {1} characters")]
    public string ForeignCurrencyCode { get; set; } = string.Empty;
    public string CivilId { get; set; } = string.Empty;
    public decimal SourceAmount { get; set; } = 0;
    public decimal DestinationAmount { get; set; } = 0;
    public string? SourceCurrencyCode { get; set; } 
}

public class ValidateCurrencyResponse
{
    public decimal DestAmount { get; set; }
    public decimal SrcAmount { get; set; }

    public string ForeignCurrencyCode { get; set; } = string.Empty;
    public string SourceCurrencyCode { get; set; } = string.Empty;
    public double TransferRate { get; set; }
    public double KdTransProfit { get; set; }
    public decimal AccountOpeningMinBalance { get; set; }
}