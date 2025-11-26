using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CreditCardsSystem.Data.Models;

[Table("CFU_ACTIVITY")]
public partial class CfuActivity
{
    [Key]
    [Column("CFU_ACTIVITY_ID", TypeName = "NUMBER(38)")]
    public decimal CfuActivityId { get; set; }

    [Column("CFU_ACTIVITY_KEY")]
    [StringLength(100)]
    [Unicode(false)]
    public string CfuActivityKey { get; set; } = null!;

    [Column("DESCRIPTION_AR")]
    [StringLength(100)]
    public string DescriptionAr { get; set; } = null!;

    [Column("DESCRIPTION_EN")]
    [StringLength(100)]
    public string DescriptionEn { get; set; } = null!;

    [Column("ENABLED")]
    [StringLength(1)]
    [Unicode(false)]
    public string? Enabled { get; set; }
}
