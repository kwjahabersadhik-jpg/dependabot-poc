namespace CreditCardsSystem.Domain.Models.CardIssuance;

public class AccountDetailsDto
{
    public string Acct { get; set; }
    public int? ClassCode { get; set; }
    public int? BranchId { get; set; }
    public string ApplicationType { get; set; } = null!;
    public string AcctType { get; set; }
    public string? AcctTypeArabic { get; set; }
    public string? AcctTypeEnglish { get; set; }
    public DateTime AccountOpenDate { get; set; }
    public decimal AvailableBalance { get; set; }
    public decimal CurrentBalance { get; set; }
    public decimal TotalBalance { get; set; }
    public string? StatusArabic { get; set; }
    public string? StatusEnglish { get; set; }

    private string? _currency;

    public string Currency
    {
        get { return _currency?.ToUpper() ?? ""; }
        set { _currency = value; }
    }

    public string? CurrencyArabic { get; set; }
    public string? CurrencyEnglish { get; set; }
    public int CurrencyDecimals { get; set; }
    public bool IsJoint { get; set; }
    public bool AllowStatement { get; set; }
    public bool AllowCredit { get; set; }
    public bool AllowDebit { get; set; }
    public bool AllowBillPayment { get; set; }
    public bool AllowCharityPayment { get; set; }
    public bool AllowStandingOrderCredit { get; set; }
    public bool AllowStandingOrderDebit { get; set; }
    public bool AllowCheckBook { get; set; }
    public bool AllowAtmCard { get; set; }
    public bool AllowCreditCard { get; set; }
    public bool AllowSms { get; set; }
    public bool AllowFax { get; set; }
    public string? Title { get; set; }
    public string? Iban { get; set; }
    public string Title1
    {
        get
        {
            return (Title?.Length > 40 ? Title[..40] : Title) ?? "";
        }
    }

    public string Title2
    {
        get
        {
            return (Title?.Length > 40 ? Title.Substring(40, Title.Length - 40) : "") ?? "";
        }
    }

    public bool ViewCurrentAccountBalance { get; set; } = false;

    public double? MonthlyIncome { get; set; } 
}
