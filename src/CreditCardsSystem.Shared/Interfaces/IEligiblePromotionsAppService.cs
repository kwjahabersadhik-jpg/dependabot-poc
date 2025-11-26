using CreditCardsSystem.Domain.Interfaces;
using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Models.BCDPromotions.Requests;
using CreditCardsSystem.Domain.Shared.Models.BCDPromotions.EligiblePromotions;
using Refit;

namespace CreditCardsSystem.Domain.Shared.Interfaces;

public interface IEligiblePromotionsAppService : IRefitClient
{
    const string Controller = "/api/EligiblePromotions/";

    [Get($"{Controller}{nameof(GetEligibleProducts)}")]
    Task<ApiResponseModel<List<EligiblePromotionDto>>> GetEligibleProducts();

    [Post($"{Controller}{nameof(AddRequest)}")]
    Task<ApiResponseModel<AddRequestResponse>> AddRequest([Body] RequestDto<EligiblePromotionDto> request);

    [Put($"{Controller}{nameof(UpdateLockStatus)}")]
    Task<ApiResponseModel<AddRequestResponse>> UpdateLockStatus(long id);

    [Post($"{Controller}{nameof(IsPromotionExist)}")]
    Task<bool> IsPromotionExist([Body] EligiblePromotionDto promotion);


}
