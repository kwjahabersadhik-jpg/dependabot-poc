using CreditCardsSystem.Data.Models;
using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Models.CardIssuance;
using CreditCardsSystem.Domain.Models.CardOperation;
using CreditCardsSystem.Domain.Models.Request;
using CreditCardsSystem.Domain.Shared.Models.Account;
using Refit;

namespace CreditCardsSystem.Domain.Interfaces;

public interface IRequestAppService : IRefitClient
{
    const string Controller = "/api/Request/";

    [Get($"{Controller}{nameof(GetUserDetails)}")]
    Task<UserDto> GetUserDetails(decimal? kfhId);

    [Get($"{Controller}{nameof(GetRequestDetailByCardNumber)}")]
    Task<ApiResponseModel<RequestDto>> GetRequestDetailByCardNumber(string cardNumber);

    [Get($"{Controller}{nameof(GetRequestIdByCardNumber)}")]
    Task<ApiResponseModel<string>> GetRequestIdByCardNumber(string cardNumber);


    [Get($"{Controller}{nameof(HasPendingOrActiveCard)}")]
    Task<bool> HasPendingOrActiveCard(string civilId);

    [Post($"{Controller}{nameof(DelegateRequest)}")]
    Task<ApiResponseModel<DelegateResponse>> DelegateRequest(DelegateRequest request);


    [Get($"{Controller}{nameof(GetRequestDetail)}")]
    Task<ApiResponseModel<RequestDto>> GetRequestDetail(decimal reqId);

    [Get($"{Controller}{nameof(CancelRequest)}")]
    Task<ApiResponseModel<CancelRequestResponse>> CancelRequest(decimal reqId);

    [Get($"{Controller}{nameof(GetPendingRequests)}")]
    Task<ApiResponseModel<List<RequestDto>>> GetPendingRequests(string civilId, int? productId);

    [Get($"{Controller}{nameof(GetAllRequestStatus)}")]
    Task<ApiResponseModel<List<RequestStatusDto>>> GetAllRequestStatus();

    [Post($"{Controller}{nameof(GetAllRequests)}")]
    Task<ApiResponseModel<List<RequestDto>>> GetAllRequests(RequestFilter filter, int page = 1, int size = 20);

    [Get($"{Controller}{nameof(GetParameters)}")]
    Task<List<RequestParameter>> GetParameters(decimal reqId);
    Task AddRequestParameters(RequestParameterDto parameters, decimal reqId, bool deleteBeforeInsert = false);

    Task<RequestResponse> CreateNewRequest(RequestDto request);

    Task<bool> HasPendingOrActiveCard(string civilId, int productId);
    Task<decimal> GenerateNewRequestId(string civilId);
    Task UpdateRequestParameter(decimal reqId, string parameter, string value);

    Task UpdateCollateralDetails(decimal reqId, string accountNumber, string depositNumber, int amount);

    Task RemoveRequestParameters(RequestParameterDto parameters, decimal reqId);
}

