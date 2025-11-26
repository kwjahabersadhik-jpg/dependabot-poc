using System.ComponentModel;

namespace CreditCardsSystem.Domain.Enums;

public enum PlasticActions
{
    [Description("No Action")]
    NoAction = 0,
    [Description("Issue New Card")]
    IssueNewCard = 1,
    [Description("Issue Additional Card")]
    IssueAdditionalCard = 2,
    [Description("Issue Replacement Card")]
    IssueReplacementCard = 3,
    [Description("Reissue Card")]
    ReissueCard = 7,
    [Description("Reissue Card With Different CardNumbering Scheme (CNS)")]
    ReissueCardWithDifferentCardNumberingScheme_CNS = 8,
    [Description("Card Technology Reissue")]
    CardTechnologyReissue = 9,
    [Description("Plastic Request Because An Additional Cardholder Has Been Added To An Existing Account")]
    PlasticRequestBecauseAnAdditionalCardholderHasBeenAddedToAnExistingAccount = 10,
    [Description("Random PIN Mailer Request")]
    RandomPIN_MailerRequest = 11,
    [Description("New Plastic Request For Lost Or Stolen")]
    NewPlasticRequestForLostOrStolen = 12,
    [Description("Replacement Cards With PIN Mailer")]
    ReplacementCardsWithPINMailer = 13
}
