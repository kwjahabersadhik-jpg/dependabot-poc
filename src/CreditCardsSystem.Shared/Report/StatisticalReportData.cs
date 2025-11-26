using CreditCardsSystem.Domain.Enums;

namespace CreditCardsSystem.Domain.Models.Report;

public class StatisticalReportData
{
    public string ProductName { get; set; }
    public CardCategoryType CardCategory { get; set; }

    public CreditCardStatus Status { get; set; }
    public DateTime RequestDate { get; set; }
    public string Branch { get; set; }
    public string CustomerName { get; set; }
    public double RequestLimit { get; set; }
    public string CardNo { get; set; }
    public double ApprovedLimit { get; set; }
    public DateTime ApproveDate { get; set; }
    public int SellerID { get; set; }
    public int TellerID { get; set; }
    public int ApprovedBy { get; set; }
    public string Collateral { get; set; }
    public string CivilID { get; set; }
    public string AcctNo { get; set; }
}