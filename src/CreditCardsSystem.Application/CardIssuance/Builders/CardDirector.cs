using CreditCardsSystem.Domain.Enums;

namespace CreditCardsSystem.Application.CardIssuance.Builders;

public interface ICardDirector
{
    ICardBuilder GetBuilder(ProductTypes productTypes);
}

public class CardDirector : ICardDirector
{
    private readonly IEnumerable<ICardBuilder> _cardBuilders;

    public CardDirector(IEnumerable<ICardBuilder> cardBuilders)
    {
        _cardBuilders = cardBuilders;
    }

    public ICardBuilder GetBuilder(ProductTypes productTypes)
    {
        return productTypes switch
        {
            ProductTypes.PrePaid => FindBuilder(typeof(PrepaidCardBuilder)),
            ProductTypes.ChargeCard => FindBuilder(typeof(ChargeCardBuilder)),
            ProductTypes.Supplementary => FindBuilder(typeof(SupplementaryChargeCardBuilder)),
            ProductTypes.Tayseer => FindBuilder(typeof(TayseerCardBuilder)),
            ProductTypes.Corporate => FindBuilder(typeof(CorporateCardBuilder)),
            _ => FindBuilder(typeof(PrepaidCardBuilder))
        };
    }

    public ICardBuilder FindBuilder(Type builderType)
    {
        return _cardBuilders.First(x => x.GetType() == builderType);
    }
}
