using System.ComponentModel.DataAnnotations;

namespace CreditCardsSystem.Domain.Models;

public partial class CardCurrencyDto
{
    [Key]
    public string CurrencyId { get; set; } = null!;

    private string? _currencyIsoCode;

    public string CurrencyIsoCode
    {
        get { return _currencyIsoCode?.ToUpper() ?? ""; }
        set { _currencyIsoCode = value; }
    }
    public string? CurrencyShortName { get; set; }
    public string CurrencyOriginalId { get; set; } = null!;
    public int? CurrencyDecimalPlaces { get; set; }
    public bool IsForeignCurrency { get; set; }
    public bool IsCorporateCard { get; set; } = false;
    public int CardType { get; set; }
    public decimal RequestId { get; set; }
    public decimal? BuyCashRate { get; set; }
    public decimal? SellCashRate { get; set; }
}

public partial class ForeignCurrencyDto
{
    public decimal BuyCashRate { get; set; }
    public string CurrencyOriginalId { get; set; } = null!;
}