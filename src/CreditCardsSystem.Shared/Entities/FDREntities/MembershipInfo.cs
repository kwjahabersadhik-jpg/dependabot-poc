using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CreditCardsSystem.Data.Models;

[PrimaryKey("CivilId", "CompanyId")]
[Table("MEMBERSHIP_INFO")]
public partial class MembershipInfo
{
    [Key]
    [Column("CIVIL_ID")]
    [StringLength(12)]
    [Unicode(false)]
    public string CivilId { get; set; } = null!;

    [Column("CLUB_MEMBERSHIP_ID")]
    [StringLength(30)]
    [Unicode(false)]
    public string ClubMembershipId { get; set; } = null!;

    [Key]
    [Column("COMPANY_ID")]
    [Precision(2)]
    public int CompanyId { get; set; }

    [Column("FILE_NAME")]
    [StringLength(30)]
    [Unicode(false)]
    public string? FileName { get; set; }

    [Column("DATE_CREATED", TypeName = "DATE")]
    public DateTime? DateCreated { get; set; }

    [Column("DATE_UPDATED", TypeName = "DATE")]
    public DateTime? DateUpdated { get; set; }
}
