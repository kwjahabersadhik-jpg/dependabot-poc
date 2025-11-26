using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CreditCardsSystem.Data.Models;

[Table("PREREGISTERED_PAYEE_STATUS")]
public partial class PreregisteredPayeeStatus
{
    [Key]
    [Column("STATUS_ID", TypeName = "NUMBER")]
    public decimal StatusId { get; set; }

    [Column("ARABIC_DESCRIPTION")]
    [StringLength(30)]
    [Unicode(false)]
    public string? ArabicDescription { get; set; }

    [Column("ENGLISH_DESCRIPTION")]
    [StringLength(30)]
    [Unicode(false)]
    public string? EnglishDescription { get; set; }
}
