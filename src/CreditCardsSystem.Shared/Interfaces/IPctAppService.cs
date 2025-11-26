using CreditCardsSystem.Domain.Interfaces;
using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Models.BCDPromotions.PCTs;
using CreditCardsSystem.Domain.Models.BCDPromotions.Requests;
using Refit;

namespace CreditCardsSystem.Domain.Shared.Interfaces;

public interface IPctAppService : IRefitClient
{
    const string Controller = "/api/pct/";

    [Get($"{Controller}{nameof(GetPcts)}")]
    Task<ApiResponseModel<List<PctDto>>> GetPcts();

    [Post($"{Controller}{nameof(AddRequest)}")]
    Task<ApiResponseModel<AddRequestResponse>> AddRequest([Body] RequestDto<PctDto> request);

    [Put($"{Controller}{nameof(UpdateLockStatus)}")]
    Task<ApiResponseModel<AddRequestResponse>> UpdateLockStatus(decimal id);
    Task<ApiResponseModel<PctDto>> GetPctById(decimal PctId);
}