using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Models.CardOperation;
using Refit;

namespace CreditCardsSystem.Domain.Interfaces;

public interface IActivationAppService : IRefitClient
{
    const string Controller = "/api/Activation/";

    [Post($"{Controller}{nameof(RequestCardReActivation)}")]
    Task<ApiResponseModel<List<CardActivationStatus>>> RequestCardReActivation(CardReActivationRequest request);

    [Post($"{Controller}{nameof(ProcessCardReActivationRequest)}")]
    Task<ApiResponseModel<ProcessResponse>> ProcessCardReActivationRequest(ActivityProcessRequest request);

    [Post($"{Controller}{nameof(ActivateSingleCard)}")]
    Task<ApiResponseModel<List<CardActivationStatus>>> ActivateSingleCard(CardActivationRequest request);

    [Post($"{Controller}{nameof(ActivateMultipleCards)}")]
    Task<ApiResponseModel<List<CardActivationStatus>>> ActivateMultipleCards(BulkCardActivationRequest request);

}
