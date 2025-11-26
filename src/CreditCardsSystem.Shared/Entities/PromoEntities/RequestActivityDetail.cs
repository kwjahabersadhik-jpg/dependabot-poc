using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CreditCardsSystem.Domain.Shared.Entities.PromoEntities;

[Table("REQUEST_ACTIVITY_DETAILS", Schema = "PROMO")]
[Index("RequestActivityId", Name = "IX_REQUEST_ACTIVITY_DETAILS_2")]
[Index("Parameter", Name = "IX_REQUEST_ACTIVIY_DETAILS1")]
public partial class RequestActivityDetail
{
    [Column("REQUEST_ACTIVITY_ID", TypeName = "NUMBER")]
    public decimal? RequestActivityId { get; set; }

    [Column("PARAMETER")]
    [StringLength(100)]
    [Unicode(false)]
    public string? Parameter { get; set; }

    [Column("VALUE")]
    [StringLength(1000)]
    [Unicode(false)]
    public string? Value { get; set; }

    [Key]
    [Column("ID", TypeName = "NUMBER")]
    public decimal Id { get; set; }

    [ForeignKey("RequestActivityId")]
    [InverseProperty("RequestActivityDetails")]
    public virtual RequestActivity? RequestActivity { get; set; }
}
