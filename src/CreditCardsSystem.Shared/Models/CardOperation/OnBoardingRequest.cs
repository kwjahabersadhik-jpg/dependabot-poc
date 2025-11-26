namespace CreditCardsSystem.Domain.Models.CardOperation;

public class OnBoardingRequest
{
    public int CardType { get; set; }
    public long RequestId { get; set; }
    public bool IsTayseerCard { get; set; } = false;
    public string? CardName { get; set; }
}
