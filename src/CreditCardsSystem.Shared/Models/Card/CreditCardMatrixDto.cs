using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Utility.Extensions;

namespace CreditCardsSystem.Domain.Models;

public class CardEligiblityMatrixDto
{
    public ProductTypes ProductType { get; set; } = default;
    public int? ProductID { get; set; }
    public string? ProductName { get; set; } = string.Empty;
    public CardStatuses Status { get; set; } = CardStatuses.Active;
    public bool IsCorporate { get; set; }
    public int Priority { get; set; }
    public string? CurrencyOriginalId { get; set; } = string.Empty;
    public bool IsCoBrandPrepaid { get; set; }
    public bool IsCobrandCredit { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public string[] AllowedClassCodes { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public CardDefinitionExtentionLiteDto? Extention { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public string[] AllowedBranches { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public int AgeMaximumLimit
    {
        get
        {
            return Extention == null ? 0 : Extention.AgeMaximumLimit.ToInt();
        }
    }

    [System.Text.Json.Serialization.JsonIgnore]
    public int AgeMinimumLimit
    {
        get
        {
            return Extention == null ? 0 : Extention.AgeMinimumLimit.ToInt();
        }
    }

    public bool? AllowedNonKfh { get; set; }
    public decimal? MinLimit { get; set; }
    public decimal? MaxLimit { get; set; }
    public CardCurrencyDto? CurrencyDto { get; set; }
    public IssuanceTypes IssuanceTypeId { get; set; }

    public override string ToString()
    {
        if (MinLimit > 0 || MaxLimit > 0)
        {
            string currency = CurrencyDto is null ? ConfigurationBase.KuwaitCurrency : CurrencyDto.CurrencyIsoCode;
            int? points = CurrencyDto?.CurrencyDecimalPlaces;
            return $"{(IsCorporate ? "Corporate" : "")} {ProductType} Limits ({MinLimit.ToMoney(currency, points)} - {MaxLimit.ToMoney(currency, points)})";
        }

        return ProductType.ToString() + (IsCorporate ? " - Corporate" : "");
    }
}

