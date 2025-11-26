using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CreditCardsSystem.Data.Models;

[Table("REQUEST")]
[Index("AcctNo", Name = "REQUEST_ACCT_IDX00001")]
[Index("BranchId", Name = "REQUEST_BRANCH_IDX0001")]
[Index("CardNo", Name = "REQUEST_CARDNO_IDX00001", IsUnique = true)]
[Index("CivilId", Name = "REQUEST_CIVILID_IDX0001")]
[Index("ReqDate", Name = "REQUEST_REQDATE_IDX0001")]
[Index("ReqStatus", Name = "REQUEST_STATUS_INDXX")]
[Index("TellerId", Name = "REQUEST_TELLER_IDX0001")]
[Index("SellerId", Name = "SELLER_REQUESTS")]
public partial class Request
{
    [Key]
    [Column("REQ_ID", TypeName = "NUMBER(28,0)")]
    [Required]
    public decimal RequestId { get; set; }

    [Column("REQ_DATE", TypeName = "DATE")]
    [Required]
    public DateTime ReqDate { get; set; }

    [Column("REQ_STATUS")]
    [Precision(2)]
    [Required]
    public int ReqStatus { get; set; }

    [NotMapped]
    public string ReqStatusString { get => ReqStatus.ToString(); }

    [Column("CARD_TYPE")]
    [Precision(2)]
    [Required]
    public int CardType { get; set; }

    [NotMapped]
    public string CardTypeString { get => CardType.ToString(); }

    [Column("LIMIT", TypeName = "NUMBER(21,6)")]
    public decimal? Limit { get; set; }

    [Column("CARD_NO")]
    [StringLength(16)]
    public string? CardNo { get; set; }

    [Column("EXPIRY")]
    [StringLength(10)]
    [Required]
    public string? Expiry { get; set; }

    [Column("ACCT_NO")]
    [StringLength(19)]
    public string? AcctNo { get; set; }

    [Column("CIVIL_ID")]
    [StringLength(12)]
    [Required]
    public string CivilId { get; set; } = null!;

    [Column("BRANCH_ID")]
    [Precision(4)]
    [Required]
    public int BranchId { get; set; }

    [Column("TELLER_ID")]
    [StringLength(10)]
    [Required]
    public string TellerId { get; set; } = null!;

    [Column("APPROVE_LIMIT", TypeName = "NUMBER(21,6)")]
    public decimal? ApproveLimit { get; set; }

    [Column("APPROVE_DATE", TypeName = "DATE")]
    public DateTime? ApproveDate { get; set; }

    [Column("SERVICE_PERIOD")]
    [Precision(2)]
    [Required]
    public byte ServicePeriod { get; set; }

    [Column("REMARK")]
    [StringLength(1000)]
    public string? Remark { get; set; }

    [Column("PHOTO")]
    [Precision(1)]
    public int Photo { get; set; }

    [Column("DEPOSIT_NO")]
    [StringLength(10)]
    public string? DepositNo { get; set; }

    [Column("DEPOSIT_AMOUNT")]
    [Precision(7)]
    public int? DepositAmount { get; set; }

    [Column("POBOX")]
    [Precision(6)]
    public int? PostOfficeBoxNumber { get; set; }

    [Column("CITY")]
    [StringLength(30)]
    [Required]
    public string City { get; set; } = null!;

    [Column("POST_CODE")]
    [Precision(6)]
    [Required]
    public int PostalCode { get; set; }

    [Column("STREET")]
    [StringLength(40)]
    [Required]
    public string Street { get; set; } = null!;

    [Column("CONTINUATION_1")]
    [StringLength(40)]
    [Required]
    public string AddressLine1 { get; set; } = null!;

    [Column("CONTINUATION_2")]
    [StringLength(40)]
    [Unicode(false)]
    [Required]
    public string AddressLine2 { get; set; } = null!;

    [Column("MOBILE")]
    [Precision(15)]
    public long? Mobile { get; set; }

    [Column("HOME_PHONE")]
    [Precision(15)]
    [Required]
    public long HomePhone { get; set; }

    [Column("WORK_PHONE")]
    [Precision(15)]
    public long? WorkPhone { get; set; }

    [Column("SALARY", TypeName = "NUMBER(8,3)")]
    public decimal? Salary { get; set; }

    [Column("MUR_INSTALLMENTS")]
    [Precision(8)]
    public int? MurInstallments { get; set; }

    [Column("RE_INSTALLMENTS")]
    [Precision(8)]
    public int? ReInstallments { get; set; }

    [Column("REQUESTED_LIMIT")]
    [Precision(5)]
    [Required]
    public short RequestedLimit { get; set; }

    [Column("SELLER_ID")]
    [Precision(10)]
    public int? SellerId { get; set; }

    [Column("FX_REF")]
    [StringLength(10)]
    [Unicode(false)]
    public string? FaxReference { get; set; }

    [Column("FD_ACCT_NO")]
    [StringLength(19)]
    [Unicode(false)]
    public string? FdAcctNo { get; set; }

    [Column("IS_AUB")]
    [Precision(1)]
    public int IsAUB { get; set; }

    [InverseProperty("RequestParameterNavigation")]
    public virtual ICollection<RequestParameter> Parameters { get; } = new List<RequestParameter>();
}
