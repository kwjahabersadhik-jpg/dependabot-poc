using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CreditCardsSystem.Domain.Shared.Entities.PromoEntities;

[Table("PCT", Schema = "PROMO")]
public partial class Pct
{
    [Key]
    [Column("PCT_ID", TypeName = "NUMBER(28)")]
    public decimal PctId { get; set; }

    [Column("PCT_FLAG")]
    [StringLength(10)]
    public string PctFlag { get; set; } = null!;

    [Column("NO_OF_WAVED_MONTHS")]
    [Precision(10)]
    public int NoOfWavedMonths { get; set; }

    [Column("CREATE_DATE", TypeName = "DATE")]
    public DateTime CreateDate { get; set; }

    [Column("FEES", TypeName = "NUMBER(10,3)")]
    public decimal Fees { get; set; }

    [Column("DESCRIPTION")]
    [StringLength(150)]
    public string? Description { get; set; }

    [Column("IS_STAFF")]
    [Precision(1)]
    public bool? IsStaff { get; set; }

    [Column("SERVICE_ID", TypeName = "NUMBER(28)")]
    public decimal? ServiceId { get; set; }

    [Column("EARLY_CLOSURE_PERCENTAGE", TypeName = "NUMBER(10,3)")]
    public decimal? EarlyClosurePercentage { get; set; }

    [Column("EARLY_CLOSURE_FEES", TypeName = "NUMBER(10,3)")]
    public decimal? EarlyClosureFees { get; set; }

    [Column("EARLY_CLOSURE_MONTHS", TypeName = "NUMBER(10,3)")]
    public decimal? EarlyClosureMonths { get; set; }

    [Column("ISLOCKED")]
    [Precision(1)]
    public bool? Islocked { get; set; }
}
