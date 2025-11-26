using System.ComponentModel;

namespace CreditCardsSystem.Domain.Enums;

public enum FraudulentReasons
{
    RepeatedCardNo = 1,
    SimilarAmount = 2,
    RepeatedReference = 3,
    SuspiciousBin = 4,
    SuspiciousMerchant = 5,
    SuspiciousCard = 6
}
public enum GenerateFileRequestType
{
    [Description("FD (VISA & Master)")]
    F = 1,

    [Description("AMEX")]
    A = 2
}
public enum MasterFileStatus
{
    [Description("Data Loaded")]
    DataLoaded = 1,

    [Description("Rules Applied")]
    RulesApplied = 2,

    [Description("FD Output Generated")]
    FDOutputGenerated = 3
}
public enum CardTypes
{
    AMEX = 3,
    VISA = 4,
    MasterCard = 5
}
public enum TransactionStatus
{
    [Description("All")]
    All = -1,

    [Description("Default Not Fraud")]
    NotFraud = 0,

    [Description("Confirmed Fraud")]
    ConfirmedFraud = 1,

    [Description("Suspicious")]
    Suspicious = 2,

    [Description("Not Reviewed")]
    NotReviewed = 3
}

public enum DeleteRequestStatus
{
    Approve = 1,
    Reject
}
public enum MiscConstants
{
    REF_NUMBER_LENGTH = 16,
    REF_NUMBERTWO_LENGTH = 4,
    CHECK_NUMBER_LENGTH = 10,
    AMOUNT_LENGTH = 14,
    FLAG_LENGTH = 1,
    CHANNEL_ID_LENGTH = 4
}
public enum EAccountStatus
{
    All,
    Active,
    Closed,
    Dormant
}

public enum EStandingOrderTransferType
{
    All,
    International,
    Internal,
    Local,
    Own,
}

[Serializable]
public enum CustomerStatus : short
{
    Active = 1,
    Account_expired,
    Blocked,
    Locked,
    Deleted
}

[Serializable]
public enum CreditCardStatus
{

    None = -2,
    All = -1,
    Pending = 0,
    Approved = 1,
    Closed = 2,
    Cancelled = 3,
    Rejected = 4,
    Active = 5,
    Lost = 6,
    Stolen = 7,
    Stopped = 8,
    ChargeOff = 10,
    [Description("Pending for Credit Checking Review")]
    PendingForCreditCheckingReview = 11,
    TemporaryClosed = 12,
    Delinquent150Days = 13,
    Delinquent120Days = 14,
    Delinquent180Days = 15,
    Delinquent210Days = 16,
    Delinquent240Days = 17,
    CreditCheckingReviewRejected = 18,
    AccountBoardingStarted = 19,
    CardUpgradeStarted = 20,
    CreditCheckingReviewed = 21,
    CreditCheckingRejected = 22,
    Delinquent30Days = 23,
    Delinquent60Days = 24,
    Delinquent90Days = 25,
    PendingForMinorsApproval = 26,
    TemporaryClosedbyCustomer = 61,
    Informaticarefreshment = 99,
}

public enum ERimServices
{

    KFH_Online_RIM_Level = 800,
    SMS_Reminder = 802,
    E_Statement_subscription = 805,
    Online_Father_Access_to_Children = 850,
}

public enum DataStatus
{
    Uninitialized,
    Loading,
    Success,
    Error,
    Processing
}
public enum CardCategoryType
{
    Normal,
    Primary,
    Supplementary
}


public enum PayeeStatus
{
    ALL = 0,
    PENDING = 1,
    APPROVED = 2,
    ACTEVATED = 3,
    REJECTED = 4
}

public enum BeneficiaryTypes
{
    [Description("Owned Card")]
    OwnedCard,
    [Description("Supplementary Card")]
    SupplementaryCard
}

public enum DurationTypes
{
    [Description("Date")]
    Date,
    [Description("Count")]
    Count,
    [Description("Unlimited")]
    Unlimited
}
public enum Collateral
{
    NONE = 0,

    [Description("Against Deposit")]
    AGAINST_DEPOSIT = 1,

    [Description("Against Deposit USD")]
    AGAINST_DEPOSIT_USD = 2,

    [Description("Against Margin")]
    AGAINST_MARGIN = 3,

    [Description("Against Margin")]
    AGAINST_MARGIN_INCREMENTAL = 4,

    [Description("Against Salary")]
    AGAINST_SALARY = 5,

    [Description("Against Salary USD")]
    AGAINST_SALARY_USD = 6,

    [Description("Exception")]
    EXCEPTION = 7,

    [Description("Prepaid Card")]
    PREPAID_CARDS = 8,

    [Description("Prepaid Card")]
    AGAINST_CORPORATE_CARD = 9,

    [Description("Foreign Currency Prepaid Card")]
    FOREIGN_CURRENCY_PREPAID_CARDS = 10,

    [Description("Supplementary charge card")]
    SUPPLEMENTARY_CHARGE_CARD = 11,

    [Description("Salary and margin")]
    SALARY_AND_MARGIN = 12,

    [Description("Against Block")]
    AGAINST_BLOCK = 13
}
public enum RequestActionType
{
    View = 0,
    Edit = 1,
    Approve = 2,
    FullEdit = 3,
    ChangeLimit = 4,
    ApproveCreditChecking = 5,
    RequestAdmin = 6,
    FinalApproveCreditChecking = 7,
    CreditCheckingReviewed = 8,
    MinorsChargeCardHoldersApproval = 9,
    CancelLimitChange = 10,
    CollateralMigrationApproval = 11,
    PrintOrUpload = 12
}

public enum ChargeCardType
{
    N = 0,
    P,
    S
}

public enum StatusType
{
    All,
    Internal,
    External
}

public enum Gender
{
    Male = 1,
    Female = 0
}

public enum Residency
{
    Resident = 1,
    NonResident
}

public enum MarriageStatus
{
    Married = 1,
    Widowed,
    Unmarried,
    Divorced,
    Other
}

public enum ThemeMode
{
    Light,
    Dark
}


public enum ActionType
{
    None = -1,
    New = 1,
    Pending = 2,
    Achieved = 3,
    [Description("Approve")]
    Approved = 4,
    [Description("Reject")]
    Rejected = 5,
    Deleted = 6,
    ReSubmitted = 7,
    Returned = 8,
    Canceled = 9,
    AssignToBCD = 10,
    PendingForCreditCheckingReview = 11,
    CreditCheckingReviewed = 21,
}

public enum RequestTypeGroup
{
    General,
    Update,
    Critical
}


public enum RequestType
{
    [Description("View Details")]
    Detail = -1,

    [Description("Replace Damage")]
    ReplacementForDamage = -2,

    [Description("Replace Lost")]
    ReplacementForLost = -3,

    [Description("Download E-Form")]
    DownloadEForm = -4,

    [Description("Cancel Card")]
    Cancel = -5,

    [Description("Statement")]
    CardStatement = -6,

    [Description("Report Lost/Stolen")]
    ReportLostOrStolen = -7,

    [Description("Change Status")]
    ChangeStatus = -8,

    [Description("Standing Order")]
    StandingOrder = -9,

    [Description("Payment")]
    CardPayment = -10,

    [Description("Migrate Collateral")]
    Migration = -11,
    [Description("Credit Reverse")]
    CreditReverse = -12,

    [Description("Replacement Tracking Report")]
    ReplaceTrackReport = -13,

    [Description("Change Link Account")]
    ChangeLinkAccountNumber = 29,

    [Description("Change Name")]
    ChangeCardHolderName = 28,

    [Description("Change Address")]
    ChangeAddress = 5,

    [Description("Activate")]
    Activate = 24,

    [Description("ReActivate")]
    ReActivate = 27,

    [Description("Close Card")]
    CardClosure = 1,

    [Description("Stop Card")]
    StopCard = 30,

    [Description("Change Limit")]
    ChangeLimit = 6,
}
public enum ChangeLimitStatus
{
    PENDING,
    APPROVED,
    REJECTED,
    CANCEL_TEMP_LIMIT,
    TEMP_LIMIT_CANCELED,
    ALL
}

public enum CreditCheckStatus
{
    Approved = 1,
    Rejected = 2
}

public enum PrintForm
{
    Eform,
    DebitVoucher,
    DepositVoucher
}