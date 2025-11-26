using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CreditCardsSystem.Data.Models;

[PrimaryKey("CardType", "Attribute")]
[Table("CARD_DEF_EXT")]
public partial class CardDefinitionExtention
{
    [Key]
    [Column("CARD_TYPE")]
    public int CardType { get; set; }

    [Key]
    [Column("ATTRIBUTE")]
    [StringLength(50)]
    public string Attribute { get; set; } = null!;

    [Column("VALUE")]
    [StringLength(100)]
    public string? Value { get; set; }

    [ForeignKey("CardType")]
    [InverseProperty("CardDefExts")]
    public virtual CardDefinition CardTypeNavigation { get; set; } = null!;
}
