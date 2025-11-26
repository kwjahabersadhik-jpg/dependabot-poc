namespace CreditCardsSystem.Domain.Interfaces;

public interface IRequestsHelperMethods
{
    Task<long> GetNewRequestId(string dbSequence);
}