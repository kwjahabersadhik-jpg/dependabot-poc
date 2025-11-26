using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CreditCardsSystem.Domain.Shared.Entities.PromoEntities;

[Table("LOYALTYPOINTSSETUP", Schema = "PROMO")]
public partial class Loyaltypointssetup
{
    [Key]
    [Column("ID", TypeName = "NUMBER")]
    public decimal Id { get; set; }

    [Column("LOCAL_POINTS", TypeName = "NUMBER")]
    public decimal LocalPoints { get; set; }

    [Column("INTERNATIONAL_POINTS", TypeName = "NUMBER")]
    public decimal InternationalPoints { get; set; }

    [Column("COST_PER_POINT", TypeName = "NUMBER(12,6)")]
    public decimal? CostPerPoint { get; set; }

    [Column("LOCAL_POINTS_TEMP", TypeName = "NUMBER")]
    public decimal? LocalPointsTemp { get; set; }

    [Column("INTERNATIONAL_POINTS_TEMP", TypeName = "NUMBER")]
    public decimal? InternationalPointsTemp { get; set; }

    [Column("COST_PER_POINT_TEMP", TypeName = "NUMBER(12,6)")]
    public decimal? CostPerPointTemp { get; set; }

    [Column("MAKER_ID", TypeName = "NUMBER")]
    public decimal MakerId { get; set; }

    [Column("CHECKER_ID", TypeName = "NUMBER")]
    public decimal? CheckerId { get; set; }

    [Column("MAKED_ON", TypeName = "DATE")]
    public DateTime MakedOn { get; set; }

    [Column("CHECKED_ON", TypeName = "DATE")]
    public DateTime? CheckedOn { get; set; }
}
