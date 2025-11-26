using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Models.CardOperation;
using Refit;

namespace CreditCardsSystem.Domain.Interfaces;

public interface IReplacementAppService : IRefitClient
{
    const string Controller = "/api/Replacement/";

    [Post($"{Controller}{nameof(RequestCardReplacement)}")]
    Task<ApiResponseModel<CardReplacementResponse>> RequestCardReplacement(CardReplacementRequest request);


    [Post($"{Controller}{nameof(ProcessCardReplacementRequest)}")]
    Task<ApiResponseModel<ProcessResponse>> ProcessCardReplacementRequest(ProcessCardClosureRequest request);

}
