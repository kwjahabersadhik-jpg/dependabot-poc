using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CreditCardsSystem.Data.Models;

[PrimaryKey("CivilId", "CardNo")]
[Table("PREREGISTERED_PAYEE")]
[Index("CreationDate", Name = "IDX_CREATION_DATE")]
public partial class PreregisteredPayee
{
    [Key]
    [Column("CIVIL_ID")]
    [StringLength(12)]
    [Unicode(false)]
    public string CivilId { get; set; } = null!;

    [Key]
    [Column("CARD_NO")]
    [StringLength(28)]
    [Unicode(false)]
    public string CardNo { get; set; } = null!;

    [Column("FULL_NAME")]
    [StringLength(60)]
    [Unicode(false)]
    public string FullName { get; set; } = null!;

    [Column("DESCRIPTION")]
    [StringLength(100)]
    [Unicode(false)]
    public string? Description { get; set; }

    [Column("STATUS_ID", TypeName = "NUMBER")]
    public decimal StatusId { get; set; }

    [Column("TYPE_ID")]
    public int TypeId { get; set; }

    [Column("CREATION_DATE", TypeName = "DATE")]
    public DateTime? CreationDate { get; set; }


}
