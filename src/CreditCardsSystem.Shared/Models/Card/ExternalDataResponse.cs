namespace CreditCardsSystem.Domain.Models;

public class ExternalDataResponse
{
    public string? ExternalStatus { get; set; }
    public decimal AvailableLimit { get; set; }
    public bool IsCardClosed { get; set; }
    public bool IsSupplementaryCard { get; set; }
}
