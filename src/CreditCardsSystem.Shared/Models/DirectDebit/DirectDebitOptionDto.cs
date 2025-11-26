namespace CreditCardsSystem.Domain.Models.DirectDebit;


public partial class DirectDebitOptionDto
{
    public DateTime? EntryDate { get; set; }

    public string GenerationOptions { get; set; } = null!;

    public string? IsFileLoadReq { get; set; }

    public string GenerationStatus { get; set; } = null!;

    public string? IsReversalPayment { get; set; }
}
