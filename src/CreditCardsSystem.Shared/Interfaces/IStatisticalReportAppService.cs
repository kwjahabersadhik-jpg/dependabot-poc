using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Models.Report;
using CreditCardsSystem.Domain.Models.Reports;
using CreditCardsSystem.Domain.Models.Request;
using Refit;

namespace CreditCardsSystem.Domain.Interfaces;

public interface IStatisticalReportAppService : IRefitClient
{
    const string Controller = "/api/StatisticalReport/";

    #region Statistical Report
    [Post($"{Controller}{nameof(GetStatisticalReport)}")]
    Task<ApiResponseModel<IEnumerable<StatisticalReportData>>> GetStatisticalReport(RequestFilter filter);

    [Post($"{Controller}{nameof(GetStatisticalChangeLimitHistory)}")]
    Task<ApiResponseModel<IEnumerable<StatisticalChangeLimitHistoryData>>> GetStatisticalChangeLimitHistory(RequestFilter filter);

    [Post($"{Controller}{nameof(PrintStatisticalChangeLimitHistoryReport)}")]
    Task<ApiResponseModel<EFormResponse>> PrintStatisticalChangeLimitHistoryReport(ChangeLimitReportDto reportData);
    #endregion
}