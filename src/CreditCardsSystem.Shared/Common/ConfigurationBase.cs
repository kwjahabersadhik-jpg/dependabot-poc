using CreditCardsSystem.Domain.Enums;

namespace CreditCardsSystem.Domain.Common
{
    public class ConfigurationBase
    {
        public const string BranchesAllowedForVMXUpdate = "996";
        public const string RetiredOccupationID = "23";
        public const string MinimumBalanceFC = "KWDDebitAccountMinBalanceForForeignCurrency";
        public const string EVENT_CREATE_REQUEST = "create_request";
        public const int AlOsraPrimaryCardTypeId = 24;
        public const int AlOsraSupplementaryCardTypeId = 25;

        public const int PrimaryPrepaidCardPayeeTypeId = 3;
        public const int PrimaryChargeCardPayeeTypeId = 4;
        public const string VisaCardStartingNumbers = "4";
        public const string MasterCardStartingNumbers = "5";
        public const int CreditCardNumberLength = 16;
        public const string CustomStandingOrderServiceName = "credit_card_standing_order_limit_increase";
        public const string FreeCardsPromotionName = "FreeCards";
        public const string AgainstCorporateCard = "AGAINST_CORPORATE_CARD";
        public const string MQ_CARD_STATUS_CLOSED = "C";
        public const string CARD_STATUS_CLOSED = "CLOSED";


        public const int SupplementaryCardCloserStatusId = 4;
        public const string VAT_Add_StandingOrder_ServiceName = "add_credit_card_so";
        public const string VAT_Edit_StandingOrder_ServiceName = "edit_credit_card_so";
        public const string VAT_Delete_StandingOrder_ServiceName = "remove_credit_card_so";


        public const string SO_AllowedCardTypes = "SO_AllowedCardTypes";
        public const string DateFormat = "dd/MM/yyyy";
        public const string FileDateFormat = "dd-MM-yyyy HH_mm_ss";
        public const string DateTimeFormat = "dd/MM/yyyy hh:mm:ss tt";

        public const string KEY_SO_START_DATE = "SO_START_DATE";
        public const string KEY_SO_COUNT = "so_count";
        public const string KEY_SO_EXPIRY_DATE = "SO_EXPIRY_DATE";
        public const string KEY_SO_CREATE_DATE = "SO_CREATE_DATE";
        public const string CurrencyConfigCode = "ORG";
        public const string CorporateCardTypeIds = "CorporateCardTypeID";

        public const string SupplementaryChargeCard = "SupplementaryChargeCard";
        public const string Logo = "Logo";
        public const string Visa = "VISA";
        public const string MasterCard = "MC";
        public const string CashPlanNumber = "CASH_PLAN_NO";
        public const string RetailPlanNumber = "RETAIL_PLAN_NO";

        public const string Country = "KUWAIT";
        public const string CountryCode = "KWT";
        public const string KuwaitCurrency = "KWD";

        public const string AccountOnBoardingDateFormat = "yyyyMMdd";
        public const string UniqueIdPrefix = "0007860";

        public const int MasterCardKACWorld = 50;


        public const string Status_CancelOrClose = "Cancel/Close";
        public const string Status_LostOrStolen = "Lost/Stolen";
        public const string Status_LostOrStolenCode = "L";
        public const string Status_TemporaryClosed = "Temporary closed";
        public const string Status_AuthorizationProhibited = "Authorization prohibited";
        public const string Status_Delinquent = "Delinquent";
        public const string Status_ChargeOff = "Charge-Off";
        public const string Status_Normal = "Normal";
        public const string Status_InArrears = "In arrears";
        public const string Status_OverLimit = "OverLimit";

        public const string InternalInArrearsStatus = "D";
        public const string InternalInArrearsOverLimitStatus = "X";

        public const string ReportDateFormat = "MMMM dd, yyyy";
        public const string PreviousBalanceDescription = "PREVIOUS BALANCE";
        public const decimal MaximumCardPaymentAmount = 10000;
        public const string IntegrationServiceSuccessCode = "0000";
        public const string Personal = "PERSONAL";
        //public const string DebitAccountTypes = "101,102,105,109,110,129,132,142,146";
        public const string DepositAccountTypeUSD = "365";
        public const string DepositAccountClassCodeUSD = "72,82,92,102";

        public const string DepositAccountApplicationType = "CD";
        public const string MarginAccountType = "118";
        public const string ExpiryDateFormat = "MMyy";
        public const string CoBrandADPhnxBranchMapping = @"996@62,995@99,992@1";

        public const string ExceptionalProductIdsForCustomerClassFilter = "26";
        public const string EliminatedPlatinumCards = "14,15,17";

        public const int USDCard = 26;


        public const string USDollerCurrency = "USD";
        public const string USDollerCurrencyId = "787";


        public const int Tolerance = 10;
        public const decimal T12PLF = 10;
        public const decimal T3PLF = 2.5M;
        public const decimal T3Limit = 1200;
        public const decimal T12Limit = 2400;

        public const decimal MALPercentRetired = 0.3M;
        public const decimal MALPercentEmployed = 0.4M;
        public const decimal MinimumFund = 50;
        public const decimal MaximumRequiredLimitPercentage = 1.5M;
        public const decimal MaximumCBKLimit = 0;

        public const string InvalidInternalStatus = "P,D,E,F,G,H,I,X,T,Z";

        public const bool IsEnabledSupplementaryFeature = true;
        public const int MaximumSupplementaryIssueByPrimaryCard = 5;
        public const int MaximumSupplementaryReceiveByCustomer = 2;
        public const int SupplementaryCardHolderAge = 15;
        public const int DualityFlag = 7;

        public const decimal MaximumPercentage = 1.5M;

        public const int DelinquentForNotTransfer = 30;

        public const decimal MinimumSalaryForCardTransfer = 250;
        public const int ApplicationID = 15;
        

        public static int BranchId { get; set; } = 995;
        public static string REJECT_REASON => "REJECT_REASON";

        public static CreditCardStatus[] PendingStatuses => new[] {
            CreditCardStatus.Pending,
            CreditCardStatus.ChargeOff,
            CreditCardStatus.Rejected,
            CreditCardStatus.AccountBoardingStarted,
            CreditCardStatus.CardUpgradeStarted,
            CreditCardStatus.PendingForCreditCheckingReview,
            CreditCardStatus.CreditCheckingReviewed,
            CreditCardStatus.CreditCheckingReviewRejected,
            CreditCardStatus.CreditCheckingRejected};

        public static CreditCardStatus[] ClosedStatuses => new[] {
            CreditCardStatus.Closed,
            CreditCardStatus.TemporaryClosed,
            CreditCardStatus.TemporaryClosedbyCustomer};

        public static string CardReplacementTrack => $"{reportRootPath}/CardReplacementTrack.trdp";
        public static string SingleReport => $"{reportRootPath}/SingleReport.trdp";
        public static string ChangeLimitReport => $"{reportRootPath}/StatisticalChangeLimitHistoryReport.trdp";
        public static string EODBranchReport => $"{reportRootPath}/EODBranchReport.trdp";
        public static string EODStaffReport => $"{reportRootPath}/EODStaffReport.trdp";
        public static string VoucherDebitReportPath => $"{reportRootPath}/VoucherDebit.trdp";

        public static string CardPaymentVoucherReportPath => $"{reportRootPath}/CardPayment.trdp";
        public static string VoucherDepositReportPath => $"{reportRootPath}/VoucherDeposit.trdp";

        private static string reportRootPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "Report");
        private static string rootPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot");

        public static string? DecimalFormat = "0.000";

        public static string MQ_CARD_STATUS_ACTIVE = " ";

        public static string TayseerCardFees = "service_tayseer_card_fees";
        public static string VisaCardFees = "service_charge_card_visa_fees";
        public static string MasterCardFees = "service_charge_card_master_fees";
        public static string Yes = "Yes";
        public static string TemporaryStopStatus = "X";
        public static string YouthCardTypes = "52"; // 52 => TAM Prepaid
        public static string YouthAccountTypes = "142";


        public static DateTime UnlimitedEndDate = new DateTime(2050, 12, 31);
        public static string StatementDateFormat = "d MMMM, yyyy";

    
        public static string INTERNAL_TRANSFER_DEPOSIT_SERVICE_NAME = "internal_transfer_deposit";

        public static string INTERNAL_TRANSFER_DEPOSIT_ChannelId = "8911";
        public static string INTERNAL_TRANSFER_MARGIN_SERVICE_NAME = "internal_transfer_margin";
        public static int CreateCustomerAccountEmpId => 4080;

        public static string AllowedCreditCardsStatus => "0,1,5,11,12,19,20,21,26";

        public static string AccountType => "AcctType_";

        public static string FileNameFormat => "^[a-zA-Z0-9\\@\\-\\s\\\\_]{1,200}\\.[a-zA-Z0-9]{1,10}$";
        public const int MaxFileSizeInMB = 2;
    }
}
