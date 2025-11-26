namespace CreditCardsSystem.Domain.Interfaces;

public interface IProfileSearch
{
    public Task SearchAsync(string civilId);
}