using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Models.CardIssuance;
using CreditCardsSystem.Domain.Models.CardOperation;
using Refit;

namespace CreditCardsSystem.Domain.Interfaces;

public interface IAddressAppService : IRefitClient
{
    const string Controller = "/api/Address/";

    [Get($"{Controller}{nameof(GetRecentBillingAddress)}")]
    Task<ApiResponseModel<BillingAddressModel>> GetRecentBillingAddress(string? civilId = null, decimal? requestId = null);

    [Get($"{Controller}{nameof(UpdateBillingAddress)}")]
    Task<ApiResponseModel<BillingAddressModel>> UpdateBillingAddress(UpdateBillingAddressRequest request);
}