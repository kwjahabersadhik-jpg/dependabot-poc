using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Models.CreditCards;
using Refit;

namespace CreditCardsSystem.Domain.Interfaces;

public interface ICreditCardsAppService : IRefitClient
{
    const string Controller = "/api/CreditCards/";

    [Get($"{Controller}{nameof(GetCreditCardsByCivilId)}")]
    Task<ApiResponseModel<List<CreditCardResponse>>> GetCreditCardsByCivilId(string civilId);

}

