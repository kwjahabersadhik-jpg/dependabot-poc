using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CreditCardsSystem.Domain.Entities.FDREntities;

[Table("CREDIT_REVERSE")]
[Index("ReqId", Name = "CREDIT_REVERSE_REQUEST_INDEX1")]
[Index("Status", Name = "CREDIT_REVERSE_REQUEST_INDEX2")]
public partial class CreditReverse
{
    [Key]
    [Column("ID")]
    [Precision(18)]
    public long Id { get; set; }

    [Column("REQ_ID")]
    [Precision(18)]
    public long ReqId { get; set; }

    [Column("AMOUNT", TypeName = "NUMBER(18,8)")]
    public decimal Amount { get; set; }

    [Column("AMOUNT_KD_MAKER", TypeName = "NUMBER(18,8)")]
    public decimal AmountKdMaker { get; set; }

    [Column("AMOUNT_KD_CHECKER", TypeName = "NUMBER(18,8)")]
    public decimal? AmountKdChecker { get; set; }

    [Column("RATE_MAKER", TypeName = "NUMBER(18,8)")]
    public decimal RateMaker { get; set; }

    [Column("RATE_CHECKER", TypeName = "NUMBER(18,8)")]
    public decimal? RateChecker { get; set; }

    [Column("REQUESTED_BY")]
    [Precision(10)]
    public int RequestedBy { get; set; }

    [Column("APPROVED_BY")]
    [Precision(10)]
    public int? ApprovedBy { get; set; }

    [Column("REQUEST_DATE", TypeName = "DATE")]
    public DateTime? RequestDate { get; set; }

    [Column("APPROVE_DATE", TypeName = "DATE")]
    public DateTime? ApproveDate { get; set; }

    [Column("REJECT_DATE", TypeName = "DATE")]
    public DateTime? RejectDate { get; set; }

    [Column("REQUESTOR_REASON")]
    [StringLength(250)]
    [Unicode(false)]
    public string? RequestorReason { get; set; }

    [Column("APPROVER_REASON")]
    [StringLength(250)]
    [Unicode(false)]
    public string? ApproverReason { get; set; }

    [Column("STATUS")]
    [Precision(2)]
    public byte? Status { get; set; }

    [Column("ACCT_NO")]
    [StringLength(20)]
    [Unicode(false)]
    public string AcctNo { get; set; } = null!;

    [Column("CARD_CURRENCY")]
    [StringLength(10)]
    [Unicode(false)]
    public string? CardCurrency { get; set; }

    [Column("ISLOCKED")]
    [Precision(1)]
    public bool? Islocked { get; set; }


    public void Lock()
    {
        Islocked = true;
    }

    public void UnLock()
    {
        Islocked = false;
    }
}
