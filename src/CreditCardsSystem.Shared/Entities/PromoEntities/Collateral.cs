using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CreditCardsSystem.Domain.Shared.Entities.PromoEntities;

[Table("COLLATERAL", Schema = "PROMO")]
public partial class Collateral
{
    [Key]
    [Column("COLLATERALID")]
    [Precision(4)]
    public byte Collateralid { get; set; }

    [Column("ISSUINGOPTION")]
    [StringLength(50)]
    [Unicode(false)]
    public string? Issuingoption { get; set; }

    [Column("DESCRIPTIONAR")]
    [StringLength(100)]
    [Unicode(false)]
    public string? Descriptionar { get; set; }

    [Column("DESCRIPTIONEN")]
    [StringLength(100)]
    [Unicode(false)]
    public string? Descriptionen { get; set; }
}
