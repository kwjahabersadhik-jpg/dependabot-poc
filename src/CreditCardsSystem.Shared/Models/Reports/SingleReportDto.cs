namespace CreditCardsSystem.Domain.Models.Reports;


public abstract class RebrandDto
{
    public bool IsRebrand { get; set; }
}

public class SingleReportDto: RebrandDto
{
    public decimal? CardLimit { get; set; }
    public decimal? MinimumLimit { get; set; }
    public decimal? TotalBeginBalance { get; set; }
    public string CardNumber { get; set; }
    public string Name { get; set; }
    public string Addrss1 { get; set; }
    public string Addrss2 { get; set; }
    public string Addrss3 { get; set; }
    public string City { get; set; }
    public string Branch { get; set; }
    public decimal? TotalDue { get; set; }
    public decimal? AvailableLimit { get; set; }
    public string ShadowAccountNumber { get; set; }
    public IEnumerable<SingleReportDetailDto> Details { get; set; }
    public string ReportPeriod { get; set; }
}
public class SingleReportDetailDto : RebrandDto
{


    public string AccountNo { get; set; }
    public DateTime? TransPostDate { get; set; }
    public DateTime? TransEffectiveDate { get; set; }
    public string CardType { get; set; }
    public string TransDescription { get; set; }
    public string ForeignCurrency { get; set; }
    public decimal? ForeignCurrencyAmount { get; set; }
    public decimal? TransAmount { get; set; }
}