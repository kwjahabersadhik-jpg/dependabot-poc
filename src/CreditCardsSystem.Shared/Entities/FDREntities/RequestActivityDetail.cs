using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CreditCardsSystem.Data.Models;

//[Keyless]
[Table("REQUEST_ACTIVITY_DETAILS")]
[Index("RequestActivityId", Name = "IX_REQUEST_ACTIVITY_DETAILS_2")]
[Index("Paramter", Name = "IX_REQUEST_ACTIVIY_DETAILS1")]
public partial class RequestActivityDetail
{
    [Required]
    [Column("REQUEST_ACTIVITY_ID", TypeName = "NUMBER")]
    public long? RequestActivityId { get; set; }

    [Column("PARAMTER")]
    [StringLength(100)]
    [Unicode(false)]
    public string? Paramter { get; set; }

    [Column("VALUE")]
    [StringLength(255)]
    [Unicode(false)]
    public string? Value { get; set; }

    [InverseProperty("Details")]
    public virtual RequestActivity RequestActivityDetailNavigation { get; set; } = null!;
}
