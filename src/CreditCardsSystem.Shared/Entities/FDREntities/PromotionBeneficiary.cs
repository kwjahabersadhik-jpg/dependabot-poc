using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CreditCardsSystem.Data.Models;

[PrimaryKey("CivilId", "PromotionId")]
[Table("PROMOTION_BENEFICIARIES", Schema = "PROMO")]
public partial class PromotionBeneficiary
{
    [Key]
    [Column("CIVIL_ID")]
    [StringLength(12)]
    public string CivilId { get; set; } = null!;

    [Key]
    [Column("PROMOTION_ID")]
    [Precision(4)]
    public int PromotionId { get; set; }

    [Column("CARD_NO")]
    [StringLength(100)]
    public string CardNo { get; set; } = null!;

    [Column("APPLICATION_DATE", TypeName = "DATE")]
    public DateTime ApplicationDate { get; set; }

    [Column("REMARKS")]
    [StringLength(100)]
    public string? Remarks { get; set; }
}
