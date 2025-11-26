using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Models.CardPayment;
using CreditCardsSystem.Domain.Models.StandingOrder;
using CreditCardsSystem.Domain.Models.SupplementaryCard;
using CreditCardsSystem.Domain.Shared.Models.CardPayment;
using Refit;

namespace CreditCardsSystem.Domain.Interfaces;

public interface ICardPaymentAppService : IRefitClient
{
    const string Controller = "/api/CardPayment/";

    [Get($"{Controller}{nameof(GetOwnedCreditCards)}")]
    Task<ApiResponseModel<List<OwnedCreditCardsResponse>>> GetOwnedCreditCards(string civilId);

    [Get($"{Controller}{nameof(GetSupplementaryCards)}")]
    Task<ApiResponseModel<List<SupplementaryCardDetail>>> GetSupplementaryCards(string civilId);

    [Post($"{Controller}{nameof(ExecuteCardPayment)}")]
    Task<ApiResponseModel<CardPaymentResponse>> ExecuteCardPayment(CardPaymentRequest request);

    [Post($"{Controller}{nameof(TransferMonetary)}")]
    Task<ApiResponseModel<TransferMonetaryResponse>> TransferMonetary([Body] TransferMonetaryRequest request);

    [Post($"{Controller}{nameof(ReverseMonetary)}")]
    Task<ApiResponseModel<TransferMonetaryResponse>> ReverseMonetary([Body] TransferMonetaryRequest request);
}

