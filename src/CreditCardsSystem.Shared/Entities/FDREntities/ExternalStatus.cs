using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CreditCardsSystem.Data.Models;

[Keyless]
[Table("EXTERNAL_STATUS")]
public partial class ExternalStatus
{
    [Column("CODE")]
    [StringLength(10)]
    [Unicode(false)]
    public string Code { get; set; } = null!;

    [Column("DESCRIPTION_EN")]
    [StringLength(200)]
    [Unicode(false)]
    public string DescriptionEn { get; set; } = null!;

    [Column("DESCRIPTION_AR")]
    [StringLength(200)]
    [Unicode(false)]
    public string DescriptionAr { get; set; } = null!;

    [Column("LOCAL_STATUS_ID", TypeName = "NUMBER(22)")]
    public decimal? LocalStatusId { get; set; }
}
