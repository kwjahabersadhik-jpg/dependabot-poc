using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Models.BCDPromotions.Requests;
using CreditCardsSystem.Domain.Models.Promotions;
using Refit;

namespace CreditCardsSystem.Domain.Interfaces;

public interface IPromotionsAppService : IRefitClient
{
    const string Controller = "/api/Promotions/";


    [Post($"{Controller}{nameof(GetActivePromotionsByAccountNumber)}")]
    Task<ApiResponseModel<List<CreditCardPromotionDto>>> GetActivePromotionsByAccountNumber(GetActivePromotionsRequest request);

    [Get($"{Controller}{nameof(GetPromotions)}")]
    Task<ApiResponseModel<List<PromotionDto>>> GetPromotions();

    Task<List<CreditCardPromotionDto>> GetActivePromotionsByProductId(GetActivePromotionsRequest request);

    Task AddPromotionToBeneficiary(AddPromotionToBeneficiaryRequest request);
    Task<CreditCardPromotionDto?> GetPromotionById(GetActivePromotionsRequest request);

    [Post($"{Controller}{nameof(AddRequest)}")]
    Task<ApiResponseModel<AddRequestResponse>> AddRequest([Body] RequestDto<PromotionDto> request);

    [Put($"{Controller}{nameof(UpdateLockStatus)}")]
    Task<ApiResponseModel<AddRequestResponse>> UpdateLockStatus(int id);
}
