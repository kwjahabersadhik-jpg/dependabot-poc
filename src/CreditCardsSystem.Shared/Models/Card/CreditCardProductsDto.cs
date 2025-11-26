
namespace CreditCardsSystem.Domain.Models.Card;

public class CreditCardProductsDto
{
    public int? CardType { get; set; }
    public string? Name { get; set; }
    public string? ArabicName { get; set; }
    public decimal? MaxLimit { get; set; }
    public decimal? MinLimit { get; set; }

}
