using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Shared.Models.Card;
using System.Text;

namespace CreditCardsSystem.Domain.Shared.Models.Account
{
    public class FinancialPositionResponse
    {
        public List<CreditCardApplication> CreditCardApplications { get; set; } = null!;
        public List<TradingApplication> TradingApplications { get; set; } = null!;
        public List<RealEstateApplication> RealEstateApplications { get; set; } = null!;
        public long TimeTaken { get; set; }
        public ForeignCurrencyResponse? UsdRate { get; set; }
    }


    public class CreditCardApplication
    {
        public decimal RequestId { get; set; }
        public long HoldId { get; set; }
        public string DepositAccount { get; set; }
        public string? CardCollateral { get; set; }
        public int? DepositAmount { get; set; }
        public string MarginAccount { get; set; }
        public decimal MarginBalance { get; set; }
        public string HoldStatus { get; set; }
        public int ProductId { get; set; }
        public decimal? CardLimit { get; set; }
        public string ProductName { get; set; }
        public string CreditCardNumber { get; set; }
        public string AccountNumber { get; set; }
        public double MurabahaInstallments { get; set; }
        public double ReInstallments { get; set; }
        public string Expiry { get; set; }
        public decimal MinimumCardLimit { get; set; }
        public short CardStatus { get; set; }
        public bool IsFetchedBalance { get; set; } = false;
        public decimal? CardBalance { get; set; }
        public StringBuilder Message { get; set; } = new();
        public bool IsValid { get; set; } = false;
        public bool ShowBalance { get; set; } = true;
        public string UpgradeMatrix { get; set; } = string.Empty;
        public int? DaysDelinquent { get; set; }
        public ProductTypes ProductType { get; set; }
        public string CardCategoryParameter { get; set; }
        public CardCategoryType CardCategoryType { get; set; }
    }

    public class TradingApplication
    {
        public string? InvoiceNumber { get; set; }
        public decimal InstallmentAmount { get; set; }
    }

    public class RealEstateApplication
    {
        public string? RealEstateType { get; set; }
        public decimal InstallmentAmount { get; set; }
    }
}
