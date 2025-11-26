using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Models.Reports;
using Refit;

namespace CreditCardsSystem.Domain.Interfaces;

public interface IReplacementTrackingReportAppService : IRefitClient
{
    const string Controller = "/api/ReplacementTrackingReport/";


    [Post($"{Controller}{nameof(GetReport)}")]
    Task<ApiResponseModel<ReplacementTrackingReportData>> GetReport(ReplacementTrackingReportFilter filter);



    [Post($"{Controller}{nameof(PrintReport)}")]
    Task<ApiResponseModel<EFormResponse>> PrintReport(ReplacementTrackingReportFilter filter);
}