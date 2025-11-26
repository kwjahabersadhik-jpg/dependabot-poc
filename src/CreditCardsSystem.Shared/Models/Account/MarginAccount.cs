namespace CreditCardsSystem.Domain.Shared.Models.Account;


public class MarginAccount
{
    public decimal RequestedLimit { get; }
    public string DebitMarginAccountNumber { get; }
    public decimal DebitMarginAmount { get; }

    public MarginAccount(decimal requestedLimit = 0, string debitMarginAccountNumber = "", decimal debitMarginAmount = 0)
    {
        RequestedLimit = requestedLimit;
        DebitMarginAccountNumber = debitMarginAccountNumber;
        DebitMarginAmount = debitMarginAmount;
    }

    public string AccountNumber { get; set; } = null!;
    public decimal AvailableBalance { get; set; } = 0;
    public bool HasInSuffienctBalance
    {
        get
        {
            return AvailableBalance < RequestedLimit;
        }
    }

    public decimal RemainingAmount
    {
        get
        {
            decimal remaining = RequestedLimit - AvailableBalance;

            if (!string.IsNullOrEmpty(DebitMarginAccountNumber) && DebitMarginAmount > 0)
                remaining -= DebitMarginAmount;

            return remaining < 0 ? 0 : remaining;
        }
    }

    public string ReferenceNumber { get; set; } = null!;

    //Using got deposit
    public string HoldId { get; set; } = null!;
}




