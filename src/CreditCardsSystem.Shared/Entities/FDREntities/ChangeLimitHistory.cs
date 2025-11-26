using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CreditCardsSystem.Data.Models;

[Table("CHANGE_LIMIT_HISTORY")]
[Index("ReqId", Name = "CHANGE_LIMIT_HISTORY_IX_REQ_ID")]
public partial class ChangeLimitHistory
{
    [Key]
    [Column("ID", TypeName = "NUMBER(38)")]
    public decimal Id { get; set; }

    [Column("REQ_ID")]
    [StringLength(20)]
    [Unicode(false)]
    public string ReqId { get; set; } = null!;

    [Column("OLD_LIMIT", TypeName = "NUMBER(38)")]
    public decimal OldLimit { get; set; }

    [Column("NEW_LIMIT", TypeName = "NUMBER(38)")]
    public decimal NewLimit { get; set; }

    [Column("IS_TEMP_LIMIT_CHANGE")]
    [StringLength(1)]
    [Unicode(false)]
    public string IsTempLimitChange { get; set; } = null!;

    [Column("LOG_DATE", TypeName = "DATE")]
    public DateTime LogDate { get; set; }

    [Column("STATUS")]
    [StringLength(20)]
    [Unicode(false)]
    public string Status { get; set; } = null!;

    [Column("REFUSE_REASON")]
    [StringLength(250)]
    [Unicode(false)]
    public string? RefuseReason { get; set; }

    [Column("INITIATOR_ID")]
    [StringLength(20)]
    [Unicode(false)]
    public string InitiatorId { get; set; } = null!;

    [Column("APPROVER_ID")]
    [StringLength(20)]
    [Unicode(false)]
    public string? ApproverId { get; set; }

    [Column("APPROVE_DATE", TypeName = "DATE")]
    public DateTime? ApproveDate { get; set; }

    [Column("REJECT_DATE", TypeName = "DATE")]
    public DateTime? RejectDate { get; set; }

    [Column("PURGE_DAYS", TypeName = "NUMBER(38)")]
    public decimal? PurgeDays { get; set; }

    [Column("USER_COMMENTS")]
    [StringLength(250)]
    [Unicode(false)]
    public string? UserComments { get; set; }

    [Column("MARGIN_ACCOUNT")]
    [StringLength(20)]
    [Unicode(false)]
    public string? MarginAccount { get; set; }

    [Column("DEPOSIT_ACCOUNT")]
    [StringLength(20)]
    [Unicode(false)]
    public string? DepositAccount { get; set; }

    [Column("DEPOSIT_NUMBER", TypeName = "NUMBER")]
    public decimal? DepositNumber { get; set; }

    [Column("KFH_SALARY", TypeName = "FLOAT")]
    public decimal? KfhSalary { get; set; }
}
