using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Models.Reports;
using Refit;

namespace CreditCardsSystem.Domain.Interfaces;

public interface ISingleReportAppService : IRefitClient
{
    const string Controller = "/api/SingleReport/";


    #region Statistical Report
    [Post($"{Controller}{nameof(PrintReport)}")]
    Task<ApiResponseModel<EFormResponse>> PrintReport(SingleReportFilter filter);


    #endregion

}