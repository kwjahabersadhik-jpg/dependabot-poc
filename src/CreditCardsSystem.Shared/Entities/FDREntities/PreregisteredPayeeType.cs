using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CreditCardsSystem.Data.Models;

[Table("PREREGISTERED_PAYEE_TYPE")]
public partial class PreregisteredPayeeType
{
    [Key]
    [Column("TYPE_ID", TypeName = "NUMBER")]
    public decimal TypeId { get; set; }

    [Column("ARABIC_DESCRIPTION")]
    [StringLength(30)]
    [Unicode(false)]
    public string? ArabicDescription { get; set; }

    [Column("ENGLISH_DESCRIPTION")]
    [StringLength(30)]
    [Unicode(false)]
    public string? EnglishDescription { get; set; }
}
