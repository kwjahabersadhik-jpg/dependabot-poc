using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Models.CardOperation;
using CreditCardsSystem.Domain.Models.CreditReverse;
using Refit;

namespace CreditCardsSystem.Domain.Interfaces;


public interface ICreditReverseAppService : IRefitClient
{
    const string Controller = "/api/CreditReverse/";

    [Get($"{Controller}{nameof(GetPendingRequest)}")]
    Task<ApiResponseModel<List<CreditReverseDto>>> GetPendingRequest(string cardNumber);

    [Post($"{Controller}{nameof(RequestCreditReverse)}")]
    Task<ApiResponseModel<CreditReverseResponse>> RequestCreditReverse(CreditReverseRequest request);

    [Post($"{Controller}{nameof(ProcessCreditReverseRequest)}")]
    Task<ApiResponseModel<ProcessResponse>> ProcessCreditReverseRequest(ProcessCreditReverseRequest request);

    [Get($"{Controller}{nameof(DeleteCreditReverseRequestById)}")]
    Task<ApiResponseModel<CreditReverseResponse>> DeleteCreditReverseRequestById(long id);
}
