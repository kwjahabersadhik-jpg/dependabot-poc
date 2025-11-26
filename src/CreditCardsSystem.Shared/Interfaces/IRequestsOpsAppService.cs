using CreditCardsSystem.Domain.Interfaces;
using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Models.BCDPromotions.Requests;
using CreditCardsSystem.Domain.Shared.Enums;
using Refit;

namespace CreditCardsSystem.Domain.Shared.Interfaces;

public interface IRequestsOpsAppService : IRefitClient
{
    const string Controller = "/api/RequestsOps/";

    [Get($"{Controller}{nameof(GetRequestsByCriteria)}")]
    Task<ApiResponseModel<List<RequestActivityDto>>> GetRequestsByCriteria(RequestsSearchCriteria searchCriteria);

    [Get($"{Controller}{nameof(GetRequestDetailsById)}")]
    Task<ApiResponseModel<List<RequestActivityDetailsDto>>> GetRequestDetailsById(long reqId);

    [Post($"{Controller}{nameof(Approve)}")]
    Task<ApiResponseModel<object>> Approve([Body] ApproveRequestDto approveRequestDto);

    [Get($"{Controller}{nameof(DeleteOrReject)}")]
    Task<ApiResponseModel<object>> DeleteOrReject(string reason, RequestStatus requestStatus, long requestId);
}