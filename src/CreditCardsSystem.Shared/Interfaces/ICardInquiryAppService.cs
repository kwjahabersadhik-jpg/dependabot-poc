using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Models.CardDelivery;
using Refit;

namespace CreditCardsSystem.Domain.Interfaces
{
    public interface ICardInquiryAppService : IRefitClient
    {
        const string Controller = "/api/CardInquiry/";

        [Get($"{Controller}{nameof(Inquiry)}")]
        Task<ApiResponseModel<List<CardInquiryDto>>> Inquiry(string civilId);
    }
}
