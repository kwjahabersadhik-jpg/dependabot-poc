namespace CreditCardsSystem.Domain.Models.StandingOrder;

public class CreditCardStandingOrderDTO
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string TypeArabic { get; set; } = string.Empty;
    public string TypeEnglish { get; set; } = string.Empty;
    public string Account { get; set; } = string.Empty;
    public string TransferedAccount { get; set; } = string.Empty;
    public string TransferedAccountName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public System.DateTime IssueDate { get; set; }
    public System.DateTime StartDate { get; set; }
    public System.DateTime NextTransferDate { get; set; }
    public System.DateTime ExpiryDate { get; set; }
    public bool Custom { get; set; }
    public bool SalaryTriggered { get; set; }
    public int NumberOfTransfers { get; set; }
    public int Frequency { get; set; }
    public string Period { get; set; } = string.Empty;
    public string PeriodArabic { get; set; } = string.Empty;
    public string PeriodEnglish { get; set; } = string.Empty;
    public bool AllowUpdate { get; set; }
    public bool AllowDelete { get; set; }
    public string ToCurrencyID { get; set; }
    public string ToCurrencyIsoCode { get; set; } = string.Empty;
}