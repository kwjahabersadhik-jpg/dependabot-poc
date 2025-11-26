using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CreditCardsSystem.Data.Models;

[Table("PROMOTION_CARD", Schema = "PROMO")]
[Index("PromotionId", "CardType", Name = "PK_PROMOTIONS_CARDS", IsUnique = true)]
public partial class PromotionCard
{
    [Column("PROMOTION_ID")]
    public int PromotionId { get; set; }

    [Column("CARD_TYPE")]
    public int CardType { get; set; }

    [Column("PCT_ID", TypeName = "NUMBER(28)")]
    public decimal? PctId { get; set; }

    [Column("COLLATERALID")]
    public int? Collateralid { get; set; }

    [Column("ISLOCKED")]
    public bool? Islocked { get; set; }

    [Key]
    [Column("PROMOTION_CARD_ID")]
    public short PromotionCardId { get; set; }

    [ForeignKey("PromotionId")]
    [InverseProperty("PromotionCards")]
    public virtual Promotion Promotion { get; set; } = null!;
}
