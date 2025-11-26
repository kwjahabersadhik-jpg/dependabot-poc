using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CreditCardsSystem.Data.Models;

[Table("ReportHeader")]
[Index("AccountNo", Name = "IX_AccountNo")]
[Index("ApplicationId", Name = "IX_ApplicationID")]
[Index("LocationId", Name = "IX_LocationId")]
[Index("UserId", Name = "IX_UserId")]
[Index("CivilId", Name = "Ix_CivilId")]
public partial class ReportHeader
{
    [Key]
    public long ReportHeaderId { get; set; }

    public int ApplicationId { get; set; }

    [StringLength(50)]
    public string UserId { get; set; } = null!;

    [StringLength(50)]
    public string LocationName { get; set; } = null!;

    [Column(TypeName = "datetime")]
    public DateTime PrintDate { get; set; }

    [StringLength(50)]
    public string? UserName { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? AccountNo { get; set; }

    [Column("IBAN")]
    [StringLength(50)]
    [Unicode(false)]
    public string? Iban { get; set; }

    [StringLength(255)]
    public string? AccountName { get; set; }

    public int? LocationId { get; set; }

    [StringLength(10)]
    [Unicode(false)]
    public string? AccountCurrency { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? CivilId { get; set; }

    [InverseProperty("ReportHeader")]
    public virtual ICollection<ReportDetail> ReportDetails { get; set; } = new List<ReportDetail>();
}
