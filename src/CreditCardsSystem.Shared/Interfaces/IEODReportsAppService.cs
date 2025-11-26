using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Models.Reports;
using CreditCardsSystem.Domain.Models.RequestActivity;
using Refit;
using System.Collections.ObjectModel;

namespace CreditCardsSystem.Domain.Interfaces;

public interface IEODReportsAppService : IRefitClient
{
    const string Controller = "/api/EODReports/";

    #region EOD Report
    [Get($"{Controller}{nameof(GetCFUActivities)}")]
    Task<ReadOnlyCollection<CFUActivityDto>> GetCFUActivities();


    [Post($"{Controller}{nameof(GetEODBranchReport)}")]
    Task<ApiResponseModel<EODBranchReportDto>> GetEODBranchReport(EODBranchReportRequest request);

    [Post($"{Controller}{nameof(GetEODStaffReport)}")]
    Task<ApiResponseModel<EODStaffReportDto>> GetEODStaffReport(EODStaffReportRequest request);

    [Post($"{Controller}{nameof(GetEODSubReport)}")]
    Task<ApiResponseModel<EODSubReport>> GetEODSubReport(EODSubReportRequest request);


    [Post($"{Controller}{nameof(PrintEODStaffReport)}")]
    Task<ApiResponseModel<EFormResponse>> PrintEODStaffReport(EODStaffReportDto reportData);


    [Post($"{Controller}{nameof(PrintEODBranchReport)}")]
    Task<ApiResponseModel<EFormResponse>> PrintEODBranchReport(EODBranchReportDto reportData);

    #endregion
}