using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CreditCardsSystem.Domain.Shared.Entities.PromoEntities;

[Table("REQUEST_ACTIVITY", Schema = "PROMO")]
[Index("ActivityStatusId", Name = "REQUEST_ACTIVITY_INDEX1")]
[Index("ActivityFormId", Name = "REQUEST_ACTIVITY_INDEX2")]
[Index("ActivityTypeId", Name = "REQUEST_ACTIVITY_INDEX3")]
public partial class RequestActivity
{
    [Key]
    [Column("REQUEST_ACTIVITY_ID", TypeName = "NUMBER(38)")]
    public decimal RequestActivityId { get; set; }

    [Column("ACTIVITY_STATUS_ID")]
    [Precision(2)]
    public byte ActivityStatusId { get; set; }

    [Column("CREATION_DATE", TypeName = "DATE")]
    public DateTime? CreationDate { get; set; }

    [Column("LAST_UPDATE_DATE", TypeName = "DATE")]
    public DateTime? LastUpdateDate { get; set; }

    [Column("MAKER_ID")]
    [Precision(7)]
    public int? MakerId { get; set; }

    [Column("CHECKER_ID")]
    [Precision(7)]
    public int? CheckerId { get; set; }

    [Column("MAKER_NAME")]
    [StringLength(100)]
    public string? MakerName { get; set; }

    [Column("CHECKER_NAME")]
    [StringLength(100)]
    public string? CheckerName { get; set; }

    [Column("ACTIVITY_FORM_ID", TypeName = "NUMBER")]
    public decimal ActivityFormId { get; set; }

    [Column("ACTIVITY_TYPE_ID", TypeName = "NUMBER")]
    public decimal ActivityTypeId { get; set; }

    [Column("REASON")]
    [StringLength(1000)]
    [Unicode(false)]
    public string? Reason { get; set; }

    [Column("ISLOCKED")]
    [Precision(1)]
    public bool? Islocked { get; set; }

    [InverseProperty("RequestActivity")]
    public virtual ICollection<RequestActivityDetail> RequestActivityDetails { get; set; } = new List<RequestActivityDetail>();
}
