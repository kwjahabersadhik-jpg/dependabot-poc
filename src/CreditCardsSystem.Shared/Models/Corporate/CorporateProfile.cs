using CreditCardsSystem.Domain.Attributes;
using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Models.Corporate;
using System.ComponentModel.DataAnnotations;

namespace CreditCardsSystem.Data.Models;

public partial class CorporateProfileDto : ValidateModel<CorporateProfileDto>
{
    [Required]
    public string CorporateCivilId { get; set; } = null!;

    [Required]

    public string CorporateNameEn { get; set; } = null!;

    [Required]

    public string CorporateNameAr { get; set; } = null!;

    [ArabicValidator]
    [Required]

    public string EmbossingName { get; set; } = null!;

    public decimal GlobalLimit => Convert.ToDecimal(GlobalLimitDto?.Amount);


    public decimal RimCode { get; set; }

    [Required]

    public string CustomerClass { get; set; } = null!;


    public string? RelationshipNo { get; set; }

    public string? CustomerNo { get; set; }

    public string? BillingAccountNo { get; set; }

    [Required]
    public string? KfhAccountNo { get; set; }

    [ArabicValidator]
    [Required]

    public string? AddressLine1 { get; set; }

    [ArabicValidator]
    [Required]
    public string? AddressLine2 { get; set; }

    public List<CorporateCard> CorporateCards { get; set; } = new();
    public decimal AvailableLimit { get; set; }
    public GlobalLimitDto? GlobalLimitDto { get; set; }

    public decimal UsedLimit { get; set; }
    public decimal RemainingLimit
    {
        get
        {
            if (GlobalLimitDto is null) return 0;

            double? remainingLimit = CorporateCards.Any() ? GlobalLimitDto?.UndisbursedAmount : GlobalLimitDto?.Amount;
            return Convert.ToDecimal(remainingLimit);
        }
    }

    public bool IsProfileNotFoundInFDR { get; set; } = false;
    public bool IsActiveRim { get; set; }
}

