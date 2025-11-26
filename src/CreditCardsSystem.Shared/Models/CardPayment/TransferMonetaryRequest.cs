using CreditCardsSystem.Domain.Shared.Models.Account;

namespace CreditCardsSystem.Domain.Shared.Models.CardPayment;

public class TransferMonetaryRequest
{
    public MarginAccount MarginAccount { get; set; } = null!;

    public string DebitAccountNumber { get; set; } = null!;

    /// <summary>
    /// This reference number for reverse payment
    /// </summary>
    public string? ReferenceNumber { get; set; }
    public string ProductName { get; set; }
}
public class TransferMonetaryResponse
{
    public string ReferenceNumber { get; set; } = null!;
}