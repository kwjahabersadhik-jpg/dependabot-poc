using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CreditCardsSystem.Data;

[Table("SERVICES", Schema = "PROMO")]
[Index("ServiceNo", Name = "SERVICES_UK1", IsUnique = true)]
public partial class Service
{
    [Column("SERVICE_NO")]
    [Precision(3)]
    public int ServiceNo { get; set; }

    [Column("NO_OF_MONTHS")]
    [Precision(3)]
    public int NoOfMonths { get; set; }

    [Column("SERVICE_DESCRIPTION")]
    [StringLength(50)]
    [Unicode(false)]
    public string? ServiceDescription { get; set; }

    [Key]
    [Column("SERVICE_ID", TypeName = "NUMBER(28)")]
    public decimal ServiceId { get; set; }

    [Column("ISLOCKED")]
    [Precision(1)]
    public bool? Islocked { get; set; }
}
