using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CreditCardsSystem.Data.Models;

[Table("CBK_CARDS")]
public partial class CbkCard
{
    [Column("CID")]
    [StringLength(12)]
    [Unicode(false)]
    public string? Cid { get; set; }

    [Key]
    [Column("REF_NO")]
    [StringLength(15)]
    [Unicode(false)]
    public string RefNo { get; set; } = null!;

    [Column("CREDIT_LIMIT_AMOUNT", TypeName = "NUMBER")]
    public decimal? CreditLimitAmount { get; set; }

    [Column("GROSS_BALANCE", TypeName = "NUMBER")]
    public decimal? GrossBalance { get; set; }

    [Column("ISSUE_DATE", TypeName = "DATE")]
    public DateTime? IssueDate { get; set; }

    [Column("EXPIRY_DATE", TypeName = "DATE")]
    public DateTime? ExpiryDate { get; set; }

    [Column("DELINQUENT_AMOUNT", TypeName = "NUMBER")]
    public decimal? DelinquentAmount { get; set; }

    [Column("DELINQUENT_DAYS", TypeName = "NUMBER")]
    public decimal? DelinquentDays { get; set; }

    [Column("EXTERNAL_STATUS")]
    [StringLength(1)]
    [Unicode(false)]
    public string? ExternalStatus { get; set; }

    [Column("PAYMENT_DUE", TypeName = "NUMBER")]
    public decimal? PaymentDue { get; set; }

    [Column("LAST_CHANGE_DATE", TypeName = "DATE")]
    public DateTime? LastChangeDate { get; set; }

    [Column("COLLATERAL_ID")]
    [StringLength(12)]
    [Unicode(false)]
    public string? CollateralId { get; set; }

    [Column("CARD_TYPE")]
    [StringLength(2)]
    [Unicode(false)]
    public string? CardType { get; set; }

    [Column("REQ_ID", TypeName = "NUMBER")]
    public decimal? ReqId { get; set; }

    [Column("ISSUING_OPTION")]
    [StringLength(50)]
    [Unicode(false)]
    public string? IssuingOption { get; set; }

    [Column("GROSS_BALANCE_SIGN")]
    [StringLength(1)]
    [Unicode(false)]
    public string? GrossBalanceSign { get; set; }

    [Column("PROFIT_RATIO", TypeName = "NUMBER")]
    public decimal? ProfitRatio { get; set; }

    [Column("INSTALLMENTS", TypeName = "NUMBER")]
    public decimal? Installments { get; set; }

    [Column("FILE_DATE", TypeName = "DATE")]
    public DateTime? FileDate { get; set; }

    [Column("REALCARDNUMBER")]
    [StringLength(20)]
    [Unicode(false)]
    public string? Realcardnumber { get; set; }
}
