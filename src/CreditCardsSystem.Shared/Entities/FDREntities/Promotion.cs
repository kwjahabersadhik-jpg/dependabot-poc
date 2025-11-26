using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CreditCardsSystem.Data.Models;

[Table("PROMOTION", Schema = "PROMO")]
public partial class Promotion
{
    [Key]
    [Column("PROMOTION_ID")]
    public int PromotionId { get; set; }

    [Column("PROMOTION_NAME")]
    [StringLength(1000)]
    public string PromotionName { get; set; } = null!;

    [Column("START_DATE", TypeName = "DATE")]
    public DateTime StartDate { get; set; }

    [Column("END_DATE", TypeName = "DATE")]
    public DateTime? EndDate { get; set; }

    [Column("PROMOTION_DESCRIPTION")]
    [StringLength(1000)]
    public string? PromotionDescription { get; set; }

    [Column("STATUS")]
    [StringLength(50)]
    public string Status { get; set; } = null!;

    [Column("USAGE_FLAG")]
    [StringLength(10)]
    public string UsageFlag { get; set; } = null!;

    [Column("ISLOCKED")]
    public bool? Islocked { get; set; }

    [InverseProperty("Promotion")]
    public virtual ICollection<PromotionCard> PromotionCards { get; } = new List<PromotionCard>();
}
