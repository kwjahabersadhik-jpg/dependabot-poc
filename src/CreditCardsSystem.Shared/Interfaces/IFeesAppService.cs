using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Models.Fees;
using Refit;

namespace CreditCardsSystem.Domain.Interfaces;

public interface IFeesAppService : IRefitClient
{
    const string Controller = "/api/Fees/";

    [Post($"{Controller}{nameof(PostServiceFee)}")]
    Task<ApiResponseModel<ServiceFeesResponse>> PostServiceFee(PostServiceFeesRequest request);

    [Post($"{Controller}{nameof(GetServiceFee)}")]
    Task<ApiResponseModel<ServiceFeesResponse>> GetServiceFee(ServiceFeesRequest request);
}

