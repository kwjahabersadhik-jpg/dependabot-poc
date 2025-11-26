using CreditCardsSystem.Domain.Common;

namespace CreditCardsSystem.Domain.Models.CardIssuance;

public class CardsCount
{
    public CardsCount(int existing = 0)
    {
        Existing = existing;
    }
    public int Remaining { get => MaximumIssue - (Existing + New); }
    public int MaximumIssue { get; set; } = ConfigurationBase.MaximumSupplementaryIssueByPrimaryCard;
    public int MaximumReceive { get; set; } = ConfigurationBase.MaximumSupplementaryReceiveByCustomer;
    public int Existing { get; set; } = 0;
    public int New { get; set; } = 0;
}
