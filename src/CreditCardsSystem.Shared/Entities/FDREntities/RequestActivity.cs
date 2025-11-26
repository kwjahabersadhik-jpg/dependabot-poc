using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CreditCardsSystem.Data.Models;

[Table("REQUEST_ACTIVITY")]
public partial class RequestActivity
{
    [Key]
    [Column("REQUEST_ACTIVITY_ID", TypeName = "NUMBER(38)")]
    public long RequestActivityId { get; set; }

    [Column("REQUEST_ACTIVITY_STATUS_ID", TypeName = "NUMBER(38)")]
    public decimal RequestActivityStatusId { get; set; }

    [Column("ARCHIVE_DATE", TypeName = "DATE")]
    public DateTime? ArchiveDate { get; set; }

    [Column("CREATION_DATE", TypeName = "DATE")]
    public DateTime? CreationDate { get; set; }

    [Column("LAST_UPDATE_DATE", TypeName = "DATE")]
    public DateTime? LastUpdateDate { get; set; }

    [Column("BRANCH_ID", TypeName = "NUMBER(38)")]
    public decimal? BranchId { get; set; }

    [Column("BRANCH_NAME")]
    [StringLength(100)]
    public string? BranchName { get; set; }

    [Column("CIVIL_ID")]
    [StringLength(20)]
    [Unicode(false)]
    public string? CivilId { get; set; }

    [Column("REQUEST_ID", TypeName = "NUMBER(38)")]
    public decimal? RequestId { get; set; }

    [Column("CUSTOMER_NAME")]
    [StringLength(100)]
    public string? CustomerName { get; set; }

    [Column("CFU_ACTIVITY_ID", TypeName = "NUMBER(38)")]
    public decimal? CfuActivityId { get; set; }

    [Column("ISSUANCE_TYPE_ID", TypeName = "NUMBER(38)")]
    public decimal? IssuanceTypeId { get; set; }

    [Column("TELLER_ID", TypeName = "NUMBER(38)")]
    public decimal? TellerId { get; set; }

    [Column("APPROVER_ID", TypeName = "NUMBER(38)")]
    public decimal? ApproverId { get; set; }

    [Column("TELLER_NAME")]
    [StringLength(100)]
    public string? TellerName { get; set; }

    [Column("APPROVER_NAME")]
    [StringLength(100)]
    public string? ApproverName { get; set; }

    [Column("ISLOCKED")]
    [Precision(1)]
    public bool? IsLocked { get; set; } = false;

    [InverseProperty("RequestActivityDetailNavigation")]
    public virtual ICollection<RequestActivityDetail> Details { get; } = new List<RequestActivityDetail>();
}

