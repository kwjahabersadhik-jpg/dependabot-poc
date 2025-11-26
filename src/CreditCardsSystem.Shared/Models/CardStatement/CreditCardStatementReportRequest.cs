using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Models.Reports;

namespace CreditCardsSystem.Domain.Models.CardStatement;

public class CreditCardStatementReportRequest : RebrandDto
{
    public string? Name { get; set; }
    public string? CardNo { get; set; }
    public string? FromDate { get; set; }
    public string? ToDate { get; set; }
    public int NoTransactions { get; set; }
    public ReportType ReportType { get; set; }
    public bool IsCobrand { get; set; }
    public decimal Cashback { get; set; }

    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public decimal TotalDeclined { get; set; }
    public decimal TotalHold { get; set; }
    public CreditCardStatementReportParameter? Parameter { get; set; }
    public string? CardCurrency { get; set; }
}
