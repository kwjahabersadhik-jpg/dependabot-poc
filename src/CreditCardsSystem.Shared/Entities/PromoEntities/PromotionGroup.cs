using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CreditCardsSystem.Data;

[Table("PROMOTION_GROUP", Schema = "PROMO")]
public partial class PromotionGroup
{
    [Key]
    [Column("GROUP_ID")]
    [Precision(4)]
    public int GroupId { get; set; }

    [Column("PROMOTION_ID")]
    [Precision(4)]
    public int PromotionId { get; set; }

    [Column("DESCRIPTION")]
    [StringLength(100)]
    [Unicode(false)]
    public string? Description { get; set; }

    [Column("STATUS")]
    [StringLength(50)]
    [Unicode(false)]
    public string Status { get; set; } = null!;

    [Column("ISLOCKED")]
    [Precision(1)]
    public bool? Islocked { get; set; }
}
