using CreditCardsSystem.Domain.Models.BCDPromotions.Services;

namespace CreditCardsSystem.Domain.Models.BCDPromotions.PCTs;

public class PctServiceDto
{
    public PctDto Pct { get; set; } = new();
    public ServiceDto Service { get; set; } = new();
}