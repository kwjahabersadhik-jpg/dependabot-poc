namespace CreditCardsSystem.Domain.Models.Reports;

public class EODBaseRequest
{
    public required int BranchId { get; set; }
    public long? TellerId { get; set; }
    public DateTime? FromCreationDate { set; get; }
    public DateTime? ToCreationDate { set; get; }
    public int? CfuActivityId { get; set; }
}
public class EODStaffReportRequest : EODBaseRequest
{
    public new required long TellerId { get; set; }
}
public class EODBranchReportRequest : EODBaseRequest
{
}
public class EODSubReportRequest : EODBaseRequest
{
}



public class EODSubReport
{
    public long? TellerId { get; set; }
    public string TellerName { set; get; }
    public string ActivityName { get; set; }
    public IEnumerable<EODSubReportSummary> Summaries { get; set; }
}


public class EODSubReportSummary
{
    public string CivilId { set; get; }
    public string CustomerName { set; get; }
    public string CardNumber { set; get; }

}
public class EODBranchReportDto : RebrandDto
{
    public string BranchName { get; set; }
    public IEnumerable<EODBranchReportItemDto> Items { get; set; }
}

public class EODBranchReportItemDto
{
    public decimal? ActivityId { get; set; }
    public string ActivityDescription { get; set; }
    public IEnumerable<EODReportSummary> Summaries { get; set; }
}

public class EODReportSummary
{
    #region Properties
    public int Serial { set; get; }
    public decimal TellerId { set; get; }
    public decimal CfuActivityId { set; get; }
    public string TellerName { set; get; }
    public int NoOfActivities { set; get; }
    public decimal? BranchId { get; set; }
    #endregion Properties
}


public class EODStaffReportDto : RebrandDto
{
    public decimal TellerId { set; get; }
    public string TellerName { set; get; }
    public string BranchName { set; get; }
    public IEnumerable<EODStaffReportSummary> Summaries { get; set; }
}
public class EODStaffReportSummary
{
    public string ActivityDescription { get; set; }
    public int Count { get; set; }
    public decimal? ActivityId { get; set; }
}

public class ChangeLimitReportDto : RebrandDto
{
    public DateTime? RequestedDateFrom { get; set; }
    public DateTime? RequestedDateTo { get; set; }
    public string CardNumber { get; set; } = string.Empty;
    public IEnumerable<StatisticalChangeLimitHistoryData> Data { get; set; }
}