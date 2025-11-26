using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CreditCardsSystem.Data.Models;


[Keyless]
[Table("RELATIONSHIP")]
public partial class Relationship
{
    [Column("ID", TypeName = "NUMBER")]
    public decimal Id { get; set; }

    [Column("NAME_EN")]
    [StringLength(100)]
    [Unicode(false)]
    public string NameEn { get; set; } = null!;

    [Column("NAME_AR")]
    [StringLength(100)]
    public string NameAr { get; set; } = null!;
}
