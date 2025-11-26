using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CreditCardsSystem.Data.Models;

[Table("MEMBERSHIP_DELETE_REQUEST")]
[Index("ClubMembershipId", Name = "MEMBERSHIP_DELETE_REQUEST_IX1")]
[Index("CompanyId", Name = "MEMBERSHIP_DELETE_REQUEST_IX2")]
[Index("Status", Name = "MEMBERSHIP_DELETE_REQUEST_IX3")]
[Index("CivilId", Name = "MEMBERSHIP_DELETE_REQUEST_IX4")]
public partial class MembershipDeleteRequest
{
    [Key]
    [Column("ID")]
    [Precision(18)]
    public long Id { get; set; }

    [Column("CIVIL_ID")]
    [StringLength(12)]
    [Unicode(false)]
    public string CivilId { get; set; } = null!;

    [Column("CLUB_MEMBERSHIP_ID")]
    [StringLength(30)]
    [Unicode(false)]
    public string ClubMembershipId { get; set; } = null!;

    [Column("COMPANY_ID")]
    [Precision(2)]
    public int CompanyId { get; set; }

    [Column("REQUESTED_BY")]
    [Precision(6)]
    public int RequestedBy { get; set; }

    [Column("APPROVED_BY")]
    [Precision(6)]
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
    public string RequestorReason { get; set; } = null!;

    [Column("APPROVER_REASON")]
    [StringLength(250)]
    [Unicode(false)]
    public string? ApproverReason { get; set; }

    [Column("STATUS")]
    [Precision(2)]
    public int? Status { get; set; }
}
