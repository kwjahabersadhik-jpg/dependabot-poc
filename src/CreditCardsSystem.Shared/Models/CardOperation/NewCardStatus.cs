using CreditCardsSystem.Domain.Enums;

namespace CreditCardsSystem.Domain.Models.CardOperation;

public record NewCardStatus(decimal RequestId, CreditCardStatus Status);
