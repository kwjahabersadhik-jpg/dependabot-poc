using System.ComponentModel;

namespace CreditCardsSystem.Domain.Shared.Enums
{
    public enum BankAccountType
    {
        [Description("CUURENT_ACCOUNT")]
        Current = 101,

        [Description("SAVING_ACCOUNT")]
        Saving = 102,

        [Description("ELECTRONIC_ACCOUNT")]
        Electronic = 105,

        [Description("DEPOSIT_ACCOUNT")]
        Deposit = 107,

        [Description("MARGIN_ACCOUNT")]
        Margin = 108
    }
}