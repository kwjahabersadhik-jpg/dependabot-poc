using CreditCardsSystem.Domain.Interfaces;
using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Models.BCDPromotions.Requests;
using CreditCardsSystem.Domain.Models.CardIssuance;
using CreditCardsSystem.Domain.Shared.Models.BCDPromotions.CardDefinition;
using Refit;

namespace CreditCardsSystem.Domain.Shared.Interfaces;

public interface ICardDefsAppService : IRefitClient
{
    const string Controller = "/api/carddefs/";

    [Post($"{Controller}{nameof(IsCardExist)}")]
    Task<bool> IsCardExist([Body] CardMatrixDto card);

    [Post($"{Controller}{nameof(AddCardMatrixRequest)}")]
    Task<ApiResponseModel<AddRequestResponse>> AddCardMatrixRequest([Body] RequestDto<CardMatrixDto> request);

    [Post($"{Controller}{nameof(AddCardDefinitionRequest)}")]
    Task<ApiResponseModel<AddRequestResponse>> AddCardDefinitionRequest([Body] RequestDto<PostCardDefDto> request);

    [Get($"{Controller}{nameof(GetCardsTypes)}")]
    Task<ApiResponseModel<List<CardDefinitionDto>>> GetCardsTypes();

    [Get($"{Controller}{nameof(GetCardTypeById)}")]
    Task<ApiResponseModel<CardDefinitionDto>> GetCardTypeById(int cardTypeId);

    [Get($"{Controller}{nameof(UpdateCardMatrixLockStatus)}")]
    Task<ApiResponseModel<AddRequestResponse>> UpdateCardMatrixLockStatus(long id);

    [Get($"{Controller}{nameof(UpdateCardDefLockStatus)}")]
    Task<ApiResponseModel<AddRequestResponse>> UpdateCardDefLockStatus(int id);

    [Get($"{Controller}{nameof(GetCardsMatrix)}")]
    Task<ApiResponseModel<List<CardMatrixDto>>> GetCardsMatrix();


}