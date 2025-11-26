using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Models.Card;
using CreditCardsSystem.Domain.Models.SupplementaryCard;

namespace CreditCardsSystem.Web.Client.Pages.CardDetails;


public class CardDetailState
{
    public DataItem<CardDetailsResponse> MyCard { get; set; } = new();
    public DataItem<List<SupplementaryCardDetail>> SupplementaryCards { get; set; } = new();
}
public class UpdateCardRequestState
{
    public bool DoReload { get; set; } = false;
    public string Message { get; set; } = string.Empty;
}

