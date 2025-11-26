using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Models.CardOperation;
using Refit;

namespace CreditCardsSystem.Domain.Interfaces;

public interface IDeliveryAppService : IRefitClient
{
    const string Controller = "/api/Delivery/";
    [Get($"{Controller}{nameof(DeliverCard)}")]
    Task<ApiResponseModel<CardDeliverResponse>> DeliverCard(string cardNumber);

    [Get($"{Controller}{nameof(RequestDelivery)}")]
    Task<ApiResponseModel<CardDeliverResponse>> RequestDelivery(DeliveryOption? deliveryOption, decimal? oldToNewReqId, int? deliveryBranchId);
}
