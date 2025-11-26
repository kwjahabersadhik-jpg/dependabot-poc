using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Models.Card;

namespace CreditCardsSystem.Domain.Models.CardOperation;

public class CardRequestFormRequest
{
    public RequestType RequestType { get; set; }
    public CreditCardDto? Card { get; set; }
}
