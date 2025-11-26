using System.ComponentModel;

namespace CreditCardsSystem.Domain.Enums;
public enum CardUpdateCFUActivity
{
    [Description("Card closure")]
    Card_Closure = 1,

    [Description("Reactivate Credit Card")]
    CARD_RE_ACTIVATION = 27,

    [Description("Replacement Lost/Stolen")]
    Replace_On_Lost_Or_Stolen = 2,

    [Description("Replacement for damage")]
    Replace_On_Damage = 3,

    [Description("Change Address")]
    CHANGE_BILLING_ADDRESS = 5,

    [Description("Change Limit")]
    LIMIT_CHANGE_INCR = 6,

    [Description("Cancel Change Limit")]
    CANCEL_LIMIT_CHANGE = 13,

    [Description("Change Name")]
    CHANGE_CARDHOLDERNAME = 28,

    [Description("Change Linked")]
    CHANGE_CARD_LINKED_ACCT = 29,
}

[Serializable]
public enum CFUActivity
{

    [Description("Card Closure")]
    Card_Closure = 1,
    [Description("Replacement Lost Or Stolen")]
    Replace_On_Lost_Or_Stolen = 2,
    [Description("Replacement for damage")]
    Replace_On_Damage = 3,

    [Description("CardIssuance")]
    CARD_ISSUANCE = 4,
    [Description("Change Address")]
    CHANGE_BILLING_ADDRESS = 5,
    [Description("Change Limit")]
    LIMIT_CHANGE_INCR = 6,
    DISPUTE = 7,
    EDIT_CUSTOMER_PROFILE = 8,
    HOLD_ADD = 9,
    CARD_UPGRADE_HOLD_ADD = 10,
    CARD_UPGRADE_HOLD_DELETE = 11,
    MARGIN_ACCOUNT_CREATE = 12,
    CANCEL_LIMIT_CHANGE = 13,
    [Description("Migrate to deposit account")]
    MIGRATE_COLLATERAL_DEPOSIT = 14,

    [Description("Migrate to margin account")]
    MIGRATE_COLLATERAL_MARGIN = 15,
    REQUEST_DELIVERY_STATUS = 16,
    MIGS_EDIT_FRAUDULENT_RULE = 17,
    MIGS_APPROVE_FRAUDULENT_RULE = 18,
    MIGS_REJECT_FRAUDULENT_RULE = 19,

    [Description("Corporate Profile Add")]
    CorporateProfileAdd = 22,

    [Description("Corporate Profile Update")]
    CorporateProfileUpdate = 23,

    [Description("Change Status")]
    CHANGE_CARD_STATUS = 24,

    [Description("Credit Reverse")]
    CREDIT_REVERSE = 25,

    UPDATE_CORP_LIMITS_IN_REQ_PARAMS = 26,
    [Description("Reactivate Credit Card")]
    CARD_RE_ACTIVATION = 27,
    [Description("Change Name")]
    CHANGE_CARDHOLDERNAME = 28,
    [Description("Change link")]
    CHANGE_CARD_LINKED_ACCT = 29,
    [Description("Temporary Closed")]
    Temporary_Closed = 30,
    [Description("Standing Order")]
    Standing_Order = 31,
    [Description("Card Payment")]
    Card_Payment = 32,
    [Description("Supplementary Card")]
    Supplementary_Card = 33,
    [Description("CardRequest")]
    Card_Request = 34,
    [Description("Membership Delete Request")]
    MemberShipDeleteRequest = 35,
    [Description("null")]
    NoActivity = 100
}
