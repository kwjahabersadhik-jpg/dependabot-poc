using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CreditCardsSystem.Data.Models;

[Keyless]
[Table("STATEMENT_MASTER", Schema = "VPBCD")]
[Index("ContractualDelinqLevel", Name = "IX_CONTRACTUAL_DELINQ_LEVEL")]
[Index("FdAcctNo", "StatementDate", Name = "IX_FD_ACCT_NO_STMNTDATE")]
[Index("UserCode3", Name = "IX_USER_CODE_3")]
public partial class StatementMaster
{
    [Column("FD_ACCT_NO")]
    [StringLength(19)]
    [Unicode(false)]
    public string FdAcctNo { get; set; } = null!;

    [Column("ZIP_SORT_CODE")]
    [StringLength(4)]
    [Unicode(false)]
    public string? ZipSortCode { get; set; }

    [Column("NAME_ADDR_LINE_1")]
    [StringLength(48)]
    [Unicode(false)]
    public string? NameAddrLine1 { get; set; }

    [Column("NAME_ADDR_LINE_2")]
    [StringLength(48)]
    [Unicode(false)]
    public string? NameAddrLine2 { get; set; }

    [Column("NAME_ADDR_LINE_3")]
    [StringLength(48)]
    [Unicode(false)]
    public string? NameAddrLine3 { get; set; }

    [Column("NAME_ADDR_LINE_4")]
    [StringLength(48)]
    [Unicode(false)]
    public string? NameAddrLine4 { get; set; }

    [Column("NAME_ADDR_LINE_5")]
    [StringLength(48)]
    [Unicode(false)]
    public string? NameAddrLine5 { get; set; }

    [Column("NAME_ADDR_LINE_6")]
    [StringLength(48)]
    [Unicode(false)]
    public string? NameAddrLine6 { get; set; }

    [Column("CITY")]
    [StringLength(48)]
    [Unicode(false)]
    public string? City { get; set; }

    [Column("STATE")]
    [StringLength(3)]
    [Unicode(false)]
    public string? State { get; set; }

    [Column("POSTAL_CODE")]
    [StringLength(10)]
    [Unicode(false)]
    public string? PostalCode { get; set; }

    [Column("STATEMENT_DATE")]
    [StringLength(4)]
    [Unicode(false)]
    public string? StatementDate { get; set; }

    [Column("PAYMENT_DATE", TypeName = "DATE")]
    public DateTime? PaymentDate { get; set; }

    [Column("CURRENT_BALANCE", TypeName = "NUMBER(21,6)")]
    public decimal? CurrentBalance { get; set; }

    [Column("MINIMUM_PAYMENT_DUE", TypeName = "NUMBER(21,6)")]
    public decimal? MinimumPaymentDue { get; set; }

    [Column("PAST_DUE_AMOUNT", TypeName = "NUMBER(21,6)")]
    public decimal? PastDueAmount { get; set; }

    [Column("TOTAL_DUE_AMOUNT", TypeName = "NUMBER(21,6)")]
    public decimal? TotalDueAmount { get; set; }

    [Column("CARD_SCHEME")]
    [StringLength(1)]
    [Unicode(false)]
    public string? CardScheme { get; set; }

    [Column("CASH_AMNT_DISPUTE", TypeName = "NUMBER(21,6)")]
    public decimal? CashAmntDispute { get; set; }

    [Column("AVALBL_CASH_CREDIT_LIMIT", TypeName = "NUMBER(21,6)")]
    public decimal? AvalblCashCreditLimit { get; set; }

    [Column("ACCT_BILLING_LEVEL")]
    [Precision(1)]
    public bool? AcctBillingLevel { get; set; }

    [Column("PRIMARY_ACCT_FLAG")]
    [StringLength(1)]
    [Unicode(false)]
    public string? PrimaryAcctFlag { get; set; }

    [Column("BILLING_CYCLE")]
    [Precision(2)]
    public int? BillingCycle { get; set; }

    [Column("CREDIT_LIMIT", TypeName = "NUMBER(21,6)")]
    public decimal? CreditLimit { get; set; }

    [Column("OPEN_TO_BUY", TypeName = "NUMBER(21,6)")]
    public decimal? OpenToBuy { get; set; }

    [Column("TOTAL_BEGIN_BALANCE", TypeName = "NUMBER(21,6)")]
    public decimal? TotalBeginBalance { get; set; }

    [Column("TOTAL_DEBIT_AMOUNT", TypeName = "NUMBER(21,6)")]
    public decimal? TotalDebitAmount { get; set; }

    [Column("TOTAL_CREDIT_AMOUNT", TypeName = "NUMBER(21,6)")]
    public decimal? TotalCreditAmount { get; set; }

    [Column("LAST_STMNT_DATE", TypeName = "DATE")]
    public DateTime? LastStmntDate { get; set; }

    [Column("INTERNAL_STATUS")]
    [StringLength(1)]
    [Unicode(false)]
    public string? InternalStatus { get; set; }

    [Column("BLOCK_CODE_1")]
    [StringLength(1)]
    [Unicode(false)]
    public string? BlockCode1 { get; set; }

    [Column("BLOCK_CODE_2")]
    [StringLength(1)]
    [Unicode(false)]
    public string? BlockCode2 { get; set; }

    [Column("STORE_ORG")]
    [Precision(3)]
    public int? StoreOrg { get; set; }

    [Column("TOTAL_NO_OF_DEBITS")]
    [Precision(3)]
    public int? TotalNoOfDebits { get; set; }

    [Column("TOTAL_NO_OF_DEBITS_SIGN")]
    [StringLength(1)]
    [Unicode(false)]
    public string? TotalNoOfDebitsSign { get; set; }

    [Column("TOTAL_NO_OF_CREDITS")]
    [Precision(9)]
    public int? TotalNoOfCredits { get; set; }

    [Column("TOTAL_NO_OF_CREDITS_SIGN")]
    [StringLength(1)]
    [Unicode(false)]
    public string? TotalNoOfCreditsSign { get; set; }

    [Column("FIXED_PAYMENT_AMOUNT")]
    [StringLength(21)]
    [Unicode(false)]
    public string? FixedPaymentAmount { get; set; }

    [Column("SCHEDULED_PAYMENT_AMOUNT")]
    [StringLength(21)]
    [Unicode(false)]
    public string? ScheduledPaymentAmount { get; set; }

    [Column("NO_OF_DESPUTED_ITEMS")]
    [Precision(3)]
    public int? NoOfDesputedItems { get; set; }

    [Column("NO_OF_DESPUTED_ITEMS_SIGN")]
    [StringLength(1)]
    [Unicode(false)]
    public string? NoOfDesputedItemsSign { get; set; }

    [Column("TOTAL_AMOUNT_IN_DISPUTE")]
    [StringLength(21)]
    [Unicode(false)]
    public string? TotalAmountInDispute { get; set; }

    [Column("DATE_OPEN", TypeName = "DATE")]
    public DateTime? DateOpen { get; set; }

    [Column("LAST_ACTIVITY_DATE", TypeName = "DATE")]
    public DateTime? LastActivityDate { get; set; }

    [Column("DISPUTE_FLAG")]
    [StringLength(1)]
    [Unicode(false)]
    public string? DisputeFlag { get; set; }

    [Column("DELINQUENCY_COUNTER_1")]
    [Precision(3)]
    public int? DelinquencyCounter1 { get; set; }

    [Column("DELINQUENCY_COUNTER_1_SIGN")]
    [StringLength(1)]
    [Unicode(false)]
    public string? DelinquencyCounter1Sign { get; set; }

    [Column("DELINQUENCY_COUNTER_2")]
    [Precision(3)]
    public int? DelinquencyCounter2 { get; set; }

    [Column("DELINQUENCY_COUNTER_2_SIGN")]
    [StringLength(1)]
    [Unicode(false)]
    public string? DelinquencyCounter2Sign { get; set; }

    [Column("DELINQUENCY_COUNTER_3")]
    [Precision(3)]
    public int? DelinquencyCounter3 { get; set; }

    [Column("DELINQUENCY_COUNTER_3_SIGN")]
    [StringLength(1)]
    [Unicode(false)]
    public string? DelinquencyCounter3Sign { get; set; }

    [Column("DELINQUENCY_COUNTER_4")]
    [Precision(3)]
    public int? DelinquencyCounter4 { get; set; }

    [Column("DELINQUENCY_COUNTER_4_SIGN")]
    [StringLength(1)]
    [Unicode(false)]
    public string? DelinquencyCounter4Sign { get; set; }

    [Column("DELINQUENCY_COUNTER_5")]
    [Precision(3)]
    public int? DelinquencyCounter5 { get; set; }

    [Column("DELINQUENCY_COUNTER_5_SIGN")]
    [StringLength(1)]
    [Unicode(false)]
    public string? DelinquencyCounter5Sign { get; set; }

    [Column("DELINQUENCY_COUNTER_6")]
    [Precision(3)]
    public int? DelinquencyCounter6 { get; set; }

    [Column("DELINQUENCY_COUNTER_6_SIGN")]
    [StringLength(1)]
    [Unicode(false)]
    public string? DelinquencyCounter6Sign { get; set; }

    [Column("DELINQUENCY_COUNTER_7")]
    [Precision(3)]
    public int? DelinquencyCounter7 { get; set; }

    [Column("DELINQUENCY_COUNTER_7_SIGN")]
    [StringLength(1)]
    [Unicode(false)]
    public string? DelinquencyCounter7Sign { get; set; }

    [Column("DELINQUENCY_COUNTER_8")]
    [Precision(3)]
    public int? DelinquencyCounter8 { get; set; }

    [Column("DELINQUENCY_COUNTER_8_SIGN")]
    [StringLength(1)]
    [Unicode(false)]
    public string? DelinquencyCounter8Sign { get; set; }

    [Column("CASH_CREDIT_LIMIT", TypeName = "NUMBER(21,6)")]
    public decimal? CashCreditLimit { get; set; }

    [Column("AMOUNT_IMMIDIATE_DUE", TypeName = "NUMBER(21,6)")]
    public decimal? AmountImmidiateDue { get; set; }

    [Column("DATE_NEXT_STMNT", TypeName = "DATE")]
    public DateTime? DateNextStmnt { get; set; }

    [Column("APP_USER_CODE_1")]
    [StringLength(20)]
    [Unicode(false)]
    public string? AppUserCode1 { get; set; }

    [Column("APP_USER_CODE_2")]
    [StringLength(20)]
    [Unicode(false)]
    public string? AppUserCode2 { get; set; }

    [Column("APP_USER_CODE_3")]
    [StringLength(20)]
    [Unicode(false)]
    public string? AppUserCode3 { get; set; }

    [Column("DUAL_ACCT_INDIC")]
    [Precision(1)]
    public bool? DualAcctIndic { get; set; }

    [Column("USER_CODE_1")]
    [StringLength(2)]
    [Unicode(false)]
    public string? UserCode1 { get; set; }

    [Column("USER_CODE_2")]
    [StringLength(2)]
    [Unicode(false)]
    public string? UserCode2 { get; set; }

    [Column("USER_CODE_3")]
    [StringLength(2)]
    [Unicode(false)]
    public string? UserCode3 { get; set; }

    [Column("USER_CODE_4")]
    [StringLength(2)]
    [Unicode(false)]
    public string? UserCode4 { get; set; }

    [Column("USER_CODE_5")]
    [StringLength(2)]
    [Unicode(false)]
    public string? UserCode5 { get; set; }

    [Column("USER_CODE_6")]
    [StringLength(2)]
    [Unicode(false)]
    public string? UserCode6 { get; set; }

    [Column("USER_CODE_7")]
    [StringLength(2)]
    [Unicode(false)]
    public string? UserCode7 { get; set; }

    [Column("USER_CODE_8")]
    [StringLength(2)]
    [Unicode(false)]
    public string? UserCode8 { get; set; }

    [Column("USER_CODE_9")]
    [StringLength(2)]
    [Unicode(false)]
    public string? UserCode9 { get; set; }

    [Column("USER_CODE_10")]
    [StringLength(2)]
    [Unicode(false)]
    public string? UserCode10 { get; set; }

    [Column("USER_CODE_11")]
    [StringLength(2)]
    [Unicode(false)]
    public string? UserCode11 { get; set; }

    [Column("USER_CODE_12")]
    [StringLength(2)]
    [Unicode(false)]
    public string? UserCode12 { get; set; }

    [Column("USER_CODE_13")]
    [StringLength(2)]
    [Unicode(false)]
    public string? UserCode13 { get; set; }

    [Column("USER_CODE_14")]
    [StringLength(2)]
    [Unicode(false)]
    public string? UserCode14 { get; set; }

    [Column("USER_DATE_1", TypeName = "DATE")]
    public DateTime? UserDate1 { get; set; }

    [Column("USER_DATE_2", TypeName = "DATE")]
    public DateTime? UserDate2 { get; set; }

    [Column("USER_DATE_3", TypeName = "DATE")]
    public DateTime? UserDate3 { get; set; }

    [Column("USER_DATE_4", TypeName = "DATE")]
    public DateTime? UserDate4 { get; set; }

    [Column("USER_DATE_5", TypeName = "DATE")]
    public DateTime? UserDate5 { get; set; }

    [Column("USER_DATE_6", TypeName = "DATE")]
    public DateTime? UserDate6 { get; set; }

    [Column("USER_DATE_7", TypeName = "DATE")]
    public DateTime? UserDate7 { get; set; }

    [Column("USER_DATE_8", TypeName = "DATE")]
    public DateTime? UserDate8 { get; set; }

    [Column("USER_DATE_9", TypeName = "DATE")]
    public DateTime? UserDate9 { get; set; }

    [Column("USER_DATE_10", TypeName = "DATE")]
    public DateTime? UserDate10 { get; set; }

    [Column("USER_DATE_11", TypeName = "DATE")]
    public DateTime? UserDate11 { get; set; }

    [Column("USER_DATE_12", TypeName = "DATE")]
    public DateTime? UserDate12 { get; set; }

    [Column("USER_DATE_13", TypeName = "DATE")]
    public DateTime? UserDate13 { get; set; }

    [Column("USER_DATE_14", TypeName = "DATE")]
    public DateTime? UserDate14 { get; set; }

    [Column("USER_AMOUNT_1")]
    [StringLength(21)]
    [Unicode(false)]
    public string? UserAmount1 { get; set; }

    [Column("USER_AMOUNT_2")]
    [StringLength(21)]
    [Unicode(false)]
    public string? UserAmount2 { get; set; }

    [Column("USER_AMOUNT_3")]
    [StringLength(21)]
    [Unicode(false)]
    public string? UserAmount3 { get; set; }

    [Column("USER_AMOUNT_4")]
    [StringLength(21)]
    [Unicode(false)]
    public string? UserAmount4 { get; set; }

    [Column("USER_AMOUNT_5")]
    [StringLength(21)]
    [Unicode(false)]
    public string? UserAmount5 { get; set; }

    [Column("USER_AMOUNT_6")]
    [StringLength(21)]
    [Unicode(false)]
    public string? UserAmount6 { get; set; }

    [Column("USER_AMOUNT_7")]
    [StringLength(21)]
    [Unicode(false)]
    public string? UserAmount7 { get; set; }

    [Column("USER_AMOUNT_8")]
    [StringLength(21)]
    [Unicode(false)]
    public string? UserAmount8 { get; set; }

    [Column("USER_AMOUNT_9")]
    [StringLength(21)]
    [Unicode(false)]
    public string? UserAmount9 { get; set; }

    [Column("USER_AMOUNT_10")]
    [StringLength(21)]
    [Unicode(false)]
    public string? UserAmount10 { get; set; }

    [Column("USER_AMOUNT_11")]
    [StringLength(21)]
    [Unicode(false)]
    public string? UserAmount11 { get; set; }

    [Column("USER_AMOUNT_12")]
    [StringLength(21)]
    [Unicode(false)]
    public string? UserAmount12 { get; set; }

    [Column("USER_AMOUNT_13")]
    [StringLength(21)]
    [Unicode(false)]
    public string? UserAmount13 { get; set; }

    [Column("USER_AMOUNT_14")]
    [StringLength(21)]
    [Unicode(false)]
    public string? UserAmount14 { get; set; }

    [Column("USER_MISC_1")]
    [StringLength(30)]
    [Unicode(false)]
    public string? UserMisc1 { get; set; }

    [Column("USER_MISC_2")]
    [StringLength(1)]
    [Unicode(false)]
    public string? UserMisc2 { get; set; }

    [Column("USER_MISC_3")]
    [StringLength(11)]
    [Unicode(false)]
    public string? UserMisc3 { get; set; }

    [Column("USER_MISC_4")]
    [StringLength(11)]
    [Unicode(false)]
    public string? UserMisc4 { get; set; }

    [Column("USER_MISC_5")]
    [StringLength(11)]
    [Unicode(false)]
    public string? UserMisc5 { get; set; }

    [Column("USER_MISC_6")]
    [StringLength(11)]
    [Unicode(false)]
    public string? UserMisc6 { get; set; }

    [Column("USER_MISC_7")]
    [StringLength(11)]
    [Unicode(false)]
    public string? UserMisc7 { get; set; }

    [Column("USER_MISC_8")]
    [StringLength(11)]
    [Unicode(false)]
    public string? UserMisc8 { get; set; }

    [Column("USER_MISC_9")]
    [StringLength(2)]
    [Unicode(false)]
    public string? UserMisc9 { get; set; }

    [Column("USER_MISC_10")]
    [StringLength(2)]
    [Unicode(false)]
    public string? UserMisc10 { get; set; }

    [Column("USER_MISC_11")]
    [StringLength(2)]
    [Unicode(false)]
    public string? UserMisc11 { get; set; }

    [Column("USER_MISC_12")]
    [StringLength(2)]
    [Unicode(false)]
    public string? UserMisc12 { get; set; }

    [Column("CUST_SHORT_NAME")]
    [StringLength(20)]
    [Unicode(false)]
    public string? CustShortName { get; set; }

    [Column("STMNT_NOTIFICATION_INDIC")]
    [StringLength(1)]
    [Unicode(false)]
    public string? StmntNotificationIndic { get; set; }

    [Column("HOUSE_NO")]
    [StringLength(20)]
    [Unicode(false)]
    public string? HouseNo { get; set; }

    [Column("HOUSE_NAME")]
    [StringLength(40)]
    [Unicode(false)]
    public string? HouseName { get; set; }

    [Column("PCT_ID")]
    [StringLength(3)]
    [Unicode(false)]
    public string? PctId { get; set; }

    [Column("AFFILIATE_EMBLEM")]
    [Precision(9)]
    public int? AffiliateEmblem { get; set; }

    [Column("TITLE")]
    [StringLength(20)]
    [Unicode(false)]
    public string? Title { get; set; }

    [Column("SUFFIX")]
    [StringLength(20)]
    [Unicode(false)]
    public string? Suffix { get; set; }

    [Column("COUNTRY")]
    [StringLength(48)]
    [Unicode(false)]
    public string? Country { get; set; }

    [Column("COUNTRY_CODE")]
    [StringLength(3)]
    [Unicode(false)]
    public string? CountryCode { get; set; }

    [Column("CUSTOMER_NO")]
    [StringLength(21)]
    [Unicode(false)]
    public string? CustomerNo { get; set; }

    [Column("PCT_EXPIRATION_DATE", TypeName = "DATE")]
    public DateTime? PctExpirationDate { get; set; }

    [Column("MIN_PAYMENT_AMOUNT", TypeName = "NUMBER(21,6)")]
    public decimal? MinPaymentAmount { get; set; }

    [Column("MIN_PAYMENT_AMOUNT_SIGN")]
    [StringLength(1)]
    [Unicode(false)]
    public string? MinPaymentAmountSign { get; set; }

    [Column("PRIMARY_CARD_NO")]
    [StringLength(19)]
    [Unicode(false)]
    public string? PrimaryCardNo { get; set; }

    [Column("PAYMENT_ACH_DEBIT_ACCT_NO")]
    [StringLength(19)]
    [Unicode(false)]
    public string? PaymentAchDebitAcctNo { get; set; }

    [Column("EMAIL")]
    [StringLength(60)]
    [Unicode(false)]
    public string? Email { get; set; }

    [Column("EMAIL_FLAG")]
    [Precision(1)]
    public bool? EmailFlag { get; set; }

    [Column("MOBILE_PHONE")]
    [StringLength(20)]
    [Unicode(false)]
    public string? MobilePhone { get; set; }

    [Column("MOBILE_PHONE_FLAG")]
    [Precision(1)]
    public bool? MobilePhoneFlag { get; set; }

    [Column("UNIQUE_ID")]
    [StringLength(19)]
    [Unicode(false)]
    public string? UniqueId { get; set; }

    [Column("STATEMENT_INDICATOR")]
    [Precision(1)]
    public bool? StatementIndicator { get; set; }

    [Column("BRANCH")]
    [StringLength(3)]
    [Unicode(false)]
    public string? Branch { get; set; }

    [Column("SHADOW_ACCT_NO")]
    [StringLength(19)]
    [Unicode(false)]
    public string? ShadowAcctNo { get; set; }

    [Column("ADDRESS_LINE_3")]
    [StringLength(48)]
    [Unicode(false)]
    public string? AddressLine3 { get; set; }

    [Column("ADDRESS_LINE_4")]
    [StringLength(48)]
    [Unicode(false)]
    public string? AddressLine4 { get; set; }

    [Column("SEND_TO_PRINT")]
    [StringLength(1)]
    [Unicode(false)]
    public string? SendToPrint { get; set; }

    [Column("CONTRACTUAL_DELINQ_LEVEL")]
    [Precision(1)]
    public bool? ContractualDelinqLevel { get; set; }

    [Column("REQ_ID", TypeName = "NUMBER(28)")]
    public decimal? ReqId { get; set; }
}
