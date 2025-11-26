using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CreditCardsSystem.Data.Models;

[PrimaryKey("AccountNo", "RecordSequence", "TransCardNo", "StatementDate")]
[Table("STATEMENT_DETAILS", Schema = "VPBCD")]
[Index("CategoryCode", Name = "IX_SD_CATEGORY_CODE")]
[Index("TransDescription", Name = "IX_SD_TRANS_DESCRIPTION")]
[Index("AccountNo", "StatementDate", Name = "IX_STMNT_FDACCTNO_STMNTDATE")]
public partial class StatementDetail
{
    [Column("RELATIONSHIP_NO")]
    [StringLength(19)]
    [Unicode(false)]
    public string? RelationshipNo { get; set; }

    [Column("ORGANIZATION_LOGO_NO")]
    [Precision(6)]
    public int? OrganizationLogoNo { get; set; }

    [Key]
    [Column("ACCOUNT_NO")]
    [StringLength(19)]
    [Unicode(false)]
    public string AccountNo { get; set; } = null!;

    [Column("ZIP_SORT_CODE")]
    [StringLength(4)]
    [Unicode(false)]
    public string? ZipSortCode { get; set; }

    [Column("DUPLICATE_STMNT_INDIC")]
    [StringLength(1)]
    [Unicode(false)]
    public string? DuplicateStmntIndic { get; set; }

    [Column("RECORD_TYPE_INDIC")]
    [StringLength(1)]
    [Unicode(false)]
    public string? RecordTypeIndic { get; set; }

    [Column("CATEGORY")]
    [Precision(1)]
    public bool? Category { get; set; }

    [Key]
    [Column("RECORD_SEQUENCE")]
    [Precision(11)]
    public long RecordSequence { get; set; }

    [Column("RECORD_SEQUENCE_SIGN")]
    [StringLength(1)]
    [Unicode(false)]
    public string? RecordSequenceSign { get; set; }

    [Column("TRANS_EFFECTIVE_DATE", TypeName = "DATE")]
    public DateTime? TransEffectiveDate { get; set; }

    [Column("TRANS_REF_NO")]
    [StringLength(23)]
    [Unicode(false)]
    public string? TransRefNo { get; set; }

    [Column("TRANS_PLAN_NO")]
    [Precision(5)]
    public short? TransPlanNo { get; set; }

    [Column("TRANS_PLAN_NO_SIGN")]
    [StringLength(1)]
    [Unicode(false)]
    public string? TransPlanNoSign { get; set; }

    [Column("TRANS_CODE")]
    [Precision(5)]
    public short? TransCode { get; set; }

    [Column("TRANS_CODE_SIGN")]
    [StringLength(1)]
    [Unicode(false)]
    public string? TransCodeSign { get; set; }

    [Column("TRANS_LOGIC_MODULE")]
    [Precision(3)]
    public byte? TransLogicModule { get; set; }

    [Column("TRANS_POST_DATE", TypeName = "DATE")]
    public DateTime? TransPostDate { get; set; }

    [Column("TRANS_DESCRIPTION")]
    [StringLength(40)]
    [Unicode(false)]
    public string? TransDescription { get; set; }

    [Column("TRANS_AMOUNT", TypeName = "NUMBER(21,6)")]
    public decimal? TransAmount { get; set; }

    [Column("TRANS_TYPE")]
    [StringLength(1)]
    [Unicode(false)]
    public string? TransType { get; set; }

    [Column("TRANS_AUTH_CODE")]
    [StringLength(6)]
    [Unicode(false)]
    public string? TransAuthCode { get; set; }

    [Column("MERCHANT_ORG")]
    [Precision(3)]
    public byte? MerchantOrg { get; set; }

    [Column("MERCHANT_ORG_SIGN")]
    [StringLength(1)]
    [Unicode(false)]
    public string? MerchantOrgSign { get; set; }

    [Column("MERCHANT_STORE")]
    [Precision(9)]
    public int? MerchantStore { get; set; }

    [Column("MERCHANT_STORE_SIGN")]
    [StringLength(1)]
    [Unicode(false)]
    public string? MerchantStoreSign { get; set; }

    [Column("CATEGORY_CODE")]
    [Precision(5)]
    public short? CategoryCode { get; set; }

    [Column("CATEGORY_CODE_SIGN")]
    [StringLength(1)]
    [Unicode(false)]
    public string? CategoryCodeSign { get; set; }

    [Column("PRODUCT")]
    [Precision(5)]
    public short? Product { get; set; }

    [Column("PRODUCT_GROUP_SIGN")]
    [StringLength(1)]
    [Unicode(false)]
    public string? ProductGroupSign { get; set; }

    [Column("VISA_TRANS_ID")]
    [Precision(15)]
    public long? VisaTransId { get; set; }

    [Column("BANK_NET_NO")]
    [StringLength(9)]
    [Unicode(false)]
    public string? BankNetNo { get; set; }

    [Column("BANK_NET_DATE", TypeName = "DATE")]
    public DateTime? BankNetDate { get; set; }

    [Column("INTERCHANGE_FEE", TypeName = "NUMBER(19,6)")]
    public decimal? InterchangeFee { get; set; }

    [Column("TRANS_TICKET_NO")]
    [StringLength(15)]
    [Unicode(false)]
    public string? TransTicketNo { get; set; }

    [Key]
    [Column("TRANS_CARD_NO")]
    [StringLength(19)]
    [Unicode(false)]
    public string TransCardNo { get; set; } = null!;

    [Column("STMNT_EXCHANGE_RATE", TypeName = "NUMBER(14,6)")]
    public decimal? StmntExchangeRate { get; set; }

    [Column("FOREIGN_CURRENCY_DESC")]
    [StringLength(40)]
    [Unicode(false)]
    public string? ForeignCurrencyDesc { get; set; }

    [Column("FOREIGN_CURRENCY_AMOUNT", TypeName = "NUMBER(21,6)")]
    public decimal? ForeignCurrencyAmount { get; set; }

    [Column("FOREIGN_CURRENCY")]
    [StringLength(3)]
    [Unicode(false)]
    public string? ForeignCurrency { get; set; }

    [Key]
    [Column("STATEMENT_DATE")]
    [StringLength(4)]
    [Unicode(false)]
    public string? StatementDate { get; set; } = null!;

    [Column("CARD_TYPE")]
    [StringLength(1)]
    [Unicode(false)]
    public string? CardType { get; set; }

    [Column("REQ_ID", TypeName = "NUMBER(28)")]
    public decimal? ReqId { get; set; }

    [Column("CARDHOLDERTYPE")]
    [StringLength(1)]
    [Unicode(false)]
    public string? Cardholdertype { get; set; }
}
