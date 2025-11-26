namespace CreditCardsSystem.Domain.Models.RequestActivity;

public class CFUActivityDto
{

    public decimal CfuActivityId { get; set; }

    public string CfuActivityKey { get; set; } = null!;

    public string DescriptionAr { get; set; } = null!;

    public string DescriptionEn { get; set; } = null!;

    public string? Enabled { get; set; }
}
