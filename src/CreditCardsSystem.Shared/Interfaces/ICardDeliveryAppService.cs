using CreditCardsSystem.Domain.Models.CardDelivery;

namespace CreditCardsSystem.Domain.Interfaces
{
    public interface ICardDeliveryAppService
    {
        Task<List<CardDeliveryDto>?> GetCardDelivery(string civilId);
    }
}
