namespace CreditCardsSystem.Data.Models;

public partial class RequestStatusDto
{
    public decimal StatusId { get; set; }

    public string? ArabicDescription { get; set; }

    public string EnglishDescription { get; set; } = null!;
}
