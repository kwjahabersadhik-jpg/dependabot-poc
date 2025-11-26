using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Models.StandingOrder;
using Refit;

namespace CreditCardsSystem.Domain.Interfaces;

public interface IStandingOrderAppService : IRefitClient
{
    const string Controller = "/api/StandingOrder/";

    [Get($"{Controller}{nameof(GetAllStandingOrders)}")]
    Task<ApiResponseModel<List<StandingOrderDto>>> GetAllStandingOrders(string civilId, int? standingOrderId = null);

    [Post($"{Controller}{nameof(AddStandingOrders)}")]
    Task<ApiResponseModel<StandingOrderResponse>> AddStandingOrders(StandingOrderRequest request);

    [Post($"{Controller}{nameof(UpdateStandingOrders)}")]
    Task<ApiResponseModel<StandingOrderResponse>> UpdateStandingOrders(StandingOrderRequest request);

    [Get($"{Controller}{nameof(GetOwnedCreditCards)}")]
    Task<ApiResponseModel<List<OwnedCreditCardsResponse>>> GetOwnedCreditCards(string civilId);

    [Post($"{Controller}{nameof(CloseStandingOrders)}")]
    Task<ApiResponseModel<StandingOrderResponse>> CloseStandingOrders(StandingOrderRequest request);

}

