namespace CreditCardsSystem.Domain.Models.CardOperation;

public class CardClosureResponse
{
    public string? CardNumber { get; set; }
    public bool IsClosed { get; set; } = false;
    public string Message { get; set; }
}
