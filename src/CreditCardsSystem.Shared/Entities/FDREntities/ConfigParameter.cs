using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CreditCardsSystem.Data.Models;

[Table("CONFIG_PARAMETERS", Schema = "PROMO")]
public partial class ConfigParameter
{
    [Key]
    [Column("ID", TypeName = "NUMBER")]
    public decimal Id { get; set; }

    [Column("PARAM_NAME")]
    [StringLength(1000)]
    [Unicode(false)]
    public string ParamName { get; set; } = null!;

    [Column("PARAM_VALUE")]
    [StringLength(1000)]
    [Unicode(false)]
    public string ParamValue { get; set; } = null!;

    [Column("PARAM_TYPE", TypeName = "NUMBER")]
    public decimal ParamType { get; set; }

    [Column("PARAM_DESC")]
    [StringLength(1000)]
    [Unicode(false)]
    public string? ParamDesc { get; set; }
}
