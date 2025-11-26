using CreditCardsSystem.Domain.Models.CardIssuance;

namespace CreditCardsSystem.Domain.Shared.Models.Request;

public class AddRequestParameterRequest
{
    public RequestParameterDto Parameters { get; set; } = new();
    public decimal RequestId { get; set; }
}
