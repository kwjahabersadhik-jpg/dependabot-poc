using System.ComponentModel;

namespace CreditCardsSystem.Domain.Enums
{
    public enum CollateralForm
    {
        [Description("Against Deposit")]
        AGAINST_DEPOSIT = 1,

        [Description("Against Margin")]
        AGAINST_MARGIN = 3,

        [Description("Against Salary")]
        AGAINST_SALARY = 5,

        [Description("Against Exception")]
        EXCEPTION = 7
    }
}
