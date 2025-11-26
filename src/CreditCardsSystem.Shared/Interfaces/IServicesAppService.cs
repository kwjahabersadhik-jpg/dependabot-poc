using CreditCardsSystem.Domain.Interfaces;
using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Models.BCDPromotions.Requests;
using CreditCardsSystem.Domain.Models.BCDPromotions.Services;
using Refit;

namespace CreditCardsSystem.Domain.Shared.Interfaces;

public interface IServicesAppService : IRefitClient
{
    const string Controller = "/api/services/";

    [Get($"{Controller}{nameof(GetServices)}")]
    Task<ApiResponseModel<List<ServiceDto>>> GetServices();

    [Post($"{Controller}{nameof(AddRequest)}")]
    Task<ApiResponseModel<AddRequestResponse>> AddRequest([Body] RequestDto<ServiceDto> request);

    [Put($"{Controller}{nameof(UpdateLockStatus)}")]
    Task<ApiResponseModel<AddRequestResponse>> UpdateLockStatus(long id);

}