using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CreditCardsSystem.Data;

[Table("GROUP_ATTRIBUTES", Schema = "PROMO")]
public partial class GroupAttribute
{
    [Key]
    [Column("ATTRIBUTE_ID")]
    [Precision(4)]
    public int AttributeId { get; set; }

    [Column("GROUP_ID")]
    [Precision(4)]
    public int GroupId { get; set; }

    [Column("ATTRIBUTE_TYPE")]
    [StringLength(50)]
    [Unicode(false)]
    public string AttributeType { get; set; } = null!;

    [Column("ATTRIBUTE_VALUE")]
    [StringLength(50)]
    [Unicode(false)]
    public string AttributeValue { get; set; } = null!;

    [Column("ISLOCKED")]
    [Precision(1)]
    public bool? Islocked { get; set; }
}
