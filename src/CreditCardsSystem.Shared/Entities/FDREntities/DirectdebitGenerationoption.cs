using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CreditCardsSystem.Domain.Entities.FDREntities;

[Keyless]
[Table("DIRECTDEBIT_GENERATIONOPTIONS", Schema = "VPBCD")]
public partial class DirectdebitGenerationoption
{
    [Column("ENTRY_DATE", TypeName = "DATE")]
    public DateTime EntryDate { get; set; }

    [Column("GENERATION_OPTIONS")]
    [StringLength(50)]
    [Unicode(false)]
    public string GenerationOptions { get; set; } = null!;

    [Column("IS_FILE_LOAD_REQ")]
    [StringLength(1)]
    [Unicode(false)]
    public string? IsFileLoadReq { get; set; }

    [Column("GENERATION_STATUS")]
    [StringLength(50)]
    [Unicode(false)]
    public string GenerationStatus { get; set; } = null!;

    [Column("IS_REVERSAL_PAYMENT")]
    [StringLength(1)]
    [Unicode(false)]
    public string? IsReversalPayment { get; set; }
}
