using System.ComponentModel;

namespace CreditCardsSystem.Domain.Enums;

public enum AddressType
{
    [Description("P.O.Box")]
    POBox,
    [Description("Full Address")]
    FullAddress
}
