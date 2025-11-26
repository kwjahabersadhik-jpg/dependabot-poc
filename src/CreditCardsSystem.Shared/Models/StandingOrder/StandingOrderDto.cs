using System.Text.Json.Serialization;

namespace CreditCardsSystem.Domain.Models;

public class StandingOrderDto
{
    public string SourceAccount { get; set; }
    public string DestinationAccount { get; set; }
    public string PayeeName { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; }
    public string TypeIcon { get; set; }
    public TimeSpan? Duration { get; set; }
    public string Type { get; set; } = "Type";
    public string Actions { get; set; } = "icon-Dots";
    public string TransferType { get; set; }
    public int StandingOrderId { get; set; }
    public DateTime NextTransferDate { get; set; }
    public int NumberOfTransfers { get; set; }

    /// <summary>
    /// Description will contain beneficiary card number
    /// </summary>
    [JsonIgnore]
    public string Description { get; set; }
    public bool AllowUpdate { get; set; }
    public bool AllowDelete { get; set; }
    public string Period { get; set; }
    public string? CardNumberDto { get; set; }
}