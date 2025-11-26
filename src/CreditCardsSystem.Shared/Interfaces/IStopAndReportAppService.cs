using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Models.CardOperation;
using Refit;

namespace CreditCardsSystem.Domain.Interfaces;

public interface IStopAndReportAppService : IRefitClient
{
    const string Controller = "/api/StopAndReport/";

    [Post($"{Controller}{nameof(ReportLostOrStolen)}")]
    Task<ApiResponseModel<ProcessResponse>> ReportLostOrStolen(StopCardRequest request);

    [Post($"{Controller}{nameof(RequestStopCard)}")]
    Task<ApiResponseModel<ProcessResponse>> RequestStopCard(StopCardRequest request);


}
