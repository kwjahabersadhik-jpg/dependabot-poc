using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Models.CardOperation;
using Refit;

namespace CreditCardsSystem.Domain.Interfaces;

public interface IClosureAppService : IRefitClient
{
    const string Controller = "/api/Closure/";

    [Get($"{Controller}{nameof(CloseCreditCard)}")]
    Task<ApiResponseModel<CardClosureResponse>> CloseCreditCard(string cardNumber, bool isSupplementary = true, bool isNew = false);

    [Post($"{Controller}{nameof(GetCardClosureRequestFormData)}")]
    Task<ApiResponseModel<ValidateCardClosureResponse>> GetCardClosureRequestFormData(CardClosureRequest request, bool skipPendingRequetCheck = false);

    [Post($"{Controller}{nameof(RequestCardClosure)}")]
    Task<ApiResponseModel<List<CardActivationStatus>>> RequestCardClosure(CardClosureRequest request);

    [Post($"{Controller}{nameof(ProcessCardClosureRequest)}")]
    Task<ApiResponseModel<ProcessResponse>> ProcessCardClosureRequest(ProcessCardClosureRequest request);

}
