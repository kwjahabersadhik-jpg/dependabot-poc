namespace CreditCardsSystem.Domain.Models;

public class RecentSearchDto
{
    public string KfhId { get; set; } = default!;

    public string? Context { get; set; }

    public string? Term { get; set; }

    public string? Type { get; set; }

    public IDictionary<string, object>? Metadata { get; set; }

    public DateTimeOffset SearchedOn { get; set; }
}