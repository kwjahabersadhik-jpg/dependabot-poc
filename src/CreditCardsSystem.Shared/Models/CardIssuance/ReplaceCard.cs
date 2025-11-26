namespace CreditCardsSystem.Domain.Models.CardIssuance;

public class ReplaceCard
{
    public string? CardNo { get; set; }
    public string? NewCardNo { get; set; }
    public double Balance { get; set; }
    public string? TransactionType { get; set; }
    public string? Status { get; set; }
    public DateTime ProcessDate { get; set; }

    public int? OldCardType { get; set; }
    public string? AccountNumber { get; set; }
    public double HighestAvailableLimit { get; set; }
    public string? NewCardType { get; set; }
    public double NewCardLimit { get; set; }
    public string? FdAcctNo { get; set; }
    public string DebitMarginAccountNumber { get; set; } = string.Empty;
    public decimal? DebitMarginAmount { get; set; }
    public long HoldId { get; set; }
    public int? HoldAmount { get; set; }
}
