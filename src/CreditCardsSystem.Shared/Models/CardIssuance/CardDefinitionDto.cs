using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Models.Promotions;
using CreditCardsSystem.Domain.Shared.Models.BCDPromotions.CardDefinition;

namespace CreditCardsSystem.Domain.Models.CardIssuance;

public class CardDefinitionDto
{
    public int ProductId { get; set; }

    public string Name { get; set; } = null!;

    public int BinNo { get; set; }

    public int SystemNo { get; set; }

    public int Duality { get; set; }

    public string? MerchantAcct { get; set; }

    public decimal? MinLimit { get; set; }

    public decimal? MaxLimit { get; set; }

    public decimal? Fees { get; set; }

    public decimal? MonthlyMaxDue { get; set; }

    public decimal? Installments { get; set; }

    public bool? Islocked { get; set; }
    public string? CurrencyId { get; set; }

    public int CardType { get; set; }

    public CardDefinitionExtentionDto? Extension { get; set; }

    public List<CardDefExtDto> CardDefExts { get; set; } = new();

    public List<CreditCardPromotionDto> Promotions { get; set; } = null!;
    public CardEligiblityMatrixDto? Eligibility { get; set; } = new();
    public ProductTypes ProductType { get; set; }


    public bool IsPrepaid => (MaxLimit == 0 && MinLimit == 0);

    public bool IsTayseerT12 => (Duality == 7 && Installments == 12);

    public bool IsTayseerT3 => (Duality == 7 && Installments == 3);

    public bool IsTayseer => (Duality == 7);

    public string CardProduct
    {
        get
        {
            if (IsPrepaid)
                return "Pre Paid";
            else if (IsTayseerT12)
                return "Tayseer T12";
            else if (IsTayseerT3)
                return "Tayseer T3";
            else
                return "Credit Card";
        }
    }
}
