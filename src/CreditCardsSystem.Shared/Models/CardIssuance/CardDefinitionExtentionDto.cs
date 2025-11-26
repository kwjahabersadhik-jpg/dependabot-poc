using System.ComponentModel.DataAnnotations.Schema;

namespace CreditCardsSystem.Domain.Models
{
    public class CardDefinitionExtentionLiteDto : IDisposable
    {
        public string? UpgradeDowngradeToCardType { get; set; }
        public string? AgeMaximumLimit { get; set; }
        public string? AgeMinimumLimit { get; set; }
        public string? Currency { get; set; }

        public void Dispose()
        {
            GC.Collect(3);
            GC.SuppressFinalize(this);
        }
    }
    public class CardDefinitionExtentionDto : IDisposable
    {
        [Column("ORG")]
        public string? Currency { get; set; } = string.Empty;

        [Column("LOGO")]
        public string? Logo { get; set; } = string.Empty;

        [Column("LOGO_MC")]
        public string? LogoMC { get; set; } = string.Empty;

        [Column("LOSTSTOLEN_FEES")] public string? LostStolenFees { get; set; } = string.Empty;

        [Column("RIM_CODE")] public string? RimCode { get; set; } = string.Empty;

        [Column("MIN_SUPPLEMENTARY_CARDS")] public string? MinimumSupplementaryCards { get; set; } = string.Empty;

        [Column("ALLOW_ACTIVATION")] public string? AllowActivation { get; set; } = string.Empty;

        [Column("PCT_DEFAULT_STAFF")] public string? PctDefaultStaff { get; set; } = string.Empty;

        [Column("NON_FUNDABLE_YEARLY_FEES")] public string? NonFundableYearlyFees { get; set; } = string.Empty;

        [Column("EMBLEM")] public string? Emblem { get; set; } = string.Empty;

        [Column("IS_PREPAID")] public string? IsPrepaid { get; set; } = string.Empty;

        [Column("STAFF_ANNUAL_FEE")] public string? StaffAnnualFee { get; set; } = string.Empty;

        [Column("EMBLEM_MC")] public string? EmblemMC { get; set; } = string.Empty;

        [Column("LOGO_VISA")] public string? LogoVisa { get; set; } = string.Empty;

        [Column("ORG_MC")] public string? OrgMC { get; set; } = string.Empty;

        [Column("MAX_MFT")] public string? MaxMft { get; set; } = string.Empty;

        [Column("DAMAGE_FEES")] public string? DamageFees { get; set; } = string.Empty;

        [Column("RETAIL_PLAN_NO")] public string? RetailPlanNo { get; set; } = string.Empty;

        [Column("AGE_MAX_LIMIT")]
        public string? AgeMaximumLimit { get; set; } = string.Empty;

        [Column("ORG_VISA")] public string? OrgVisa { get; set; } = string.Empty;

        [Column("MAX_SUPPLEMENTARY_CARDS")] public string? MaxSupplimentaryCards { get; set; } = string.Empty;

        [Column("PCT_DEFAULT")] public string? PctDefault { get; set; } = string.Empty;

        [Column("UPGRADE_DOWNGRADE_TO_CARD_TYPE")] public string? UpgradeDowngradeToCardType { get; set; } = string.Empty;

        [Column("AGE_MIN_LIMIT")]
        public string? AgeMinimumLimit { get; set; } = string.Empty;

        [Column("CASH_PLAN_NO")] public string? CashPlanNo { get; set; } = string.Empty;

        [Column("EMBLEM_VISA")] public string? EmblemVisa { get; set; } = string.Empty;

        [Column("MIN_MFT")] public string? MinMFT { get; set; } = string.Empty;

        [Column("NET SAL REQ")] public string? NetSalRequest { get; set; } = string.Empty;

        public void Dispose()
        {
            GC.Collect(3);
            GC.SuppressFinalize(this);
        }
    }
}
