namespace CreditCardsSystem.Domain.Models.Card;

public class CardStatusDto
{
    public string Code { get; set; } = null!;
    public string DescriptionEn { get; set; } = null!;
    public string DescriptionAr { get; set; } = null!;
    public decimal? LocalStatusId { get; set; }
}




public class CardStatusList
{
    public List<CardStatusDto> InternalStatus { get; set; } = new();
    public List<CardStatusDto> ExternalStatus { get; set; } = new();
}
