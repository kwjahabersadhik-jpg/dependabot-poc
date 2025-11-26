namespace CreditCardsSystem.Domain.Shared.Models.Account;

public class TransferAccount
{
    public readonly string ServiceName;

    public TransferAccount(string serviceName)
    {
        ServiceName = serviceName;
    }

    public string AccountNumber { get; set; } = null!;
    public double Amount { get; set; }
    public bool IsDebit { get; set; }
    public string Description { get { return "Credit Limit Transfer"; } }
}
