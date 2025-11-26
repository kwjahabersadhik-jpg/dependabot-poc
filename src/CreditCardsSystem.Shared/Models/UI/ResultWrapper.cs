namespace CreditCardsSystem.Domain.Models.UI;

public class ResultWrapper<T>
{
    public bool IsError { get; set; }
    public string? Message { get; set; }
    public T? Response { get; set; }
}