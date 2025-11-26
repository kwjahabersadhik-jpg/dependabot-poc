using CreditCardsSystem.Data.Models;
using CreditCardsSystem.Domain.Interfaces;
using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Models.Card;
using CreditCardsSystem.Domain.Models.SupplementaryCard;
using Refit;

namespace CreditCardsSystem.Domain.Shared.Interfaces
{
    public interface IPreRegisteredPayeeAppService : IRefitClient
    {
        const string Controller = "/api/PreRegisteredPayee/";

        [Get($"{Controller}{nameof(GetPreregisteredPayeeByCivilId)}")]
        Task<ApiResponseModel<List<SupplementaryCardDetail>>> GetPreregisteredPayeeByCivilId(string civilId, int? payeeTypeId = null, decimal primaryCardRequestId = 0);

        Task<bool> UpdatePreregisteredPayee(CardDetailsResponse cardRequest);
        Task<bool> AddPreregisteredPayee(PreregisteredPayee payee);
        Task<IEnumerable<PreregisteredPayee>> GetPreregisteredPayeeByCardNumber(string cardNumber);
    }
}
