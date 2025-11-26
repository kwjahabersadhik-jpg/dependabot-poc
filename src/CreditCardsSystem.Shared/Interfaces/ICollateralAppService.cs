using CreditCardsSystem.Domain.Interfaces;
using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Models.BCDPromotions.Requests;
using CreditCardsSystem.Domain.Shared.Models.BCDPromotions.Collaterals;
using CreditCardsSystem.Domain.Shared.Models.BCDPromotions.EligiblePromotions;
using Refit;

namespace CreditCardsSystem.Domain.Shared.Interfaces;

public interface ICollateralAppService : IRefitClient
{
    const string Controller = "/api/collateral/";

    [Get($"{Controller}{nameof(GetCollaterls)}")]
    Task<ApiResponseModel<List<CollateralDto>>> GetCollaterls();

    [Post($"{Controller}{nameof(AddRequest)}")]
    Task<ApiResponseModel<AddRequestResponse>> AddRequest([Body] RequestDto<EligiblePromotionDto> request);

    [Put($"{Controller}{nameof(UpdateLockStatus)}")]
    Task<ApiResponseModel<AddRequestResponse>> UpdateLockStatus(long id);

}