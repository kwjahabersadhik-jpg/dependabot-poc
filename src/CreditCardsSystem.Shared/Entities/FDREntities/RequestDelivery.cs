using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CreditCardsSystem.Data.Models;

[Table("REQUEST_DELIVERY")]
public partial class RequestDelivery
{
    [Key]
    [Column("REQUEST_DELIVERY_ID", TypeName = "NUMBER(28)")]
    public decimal RequestDeliveryId { get; set; }

    [Column("CREATE_DATE", TypeName = "DATE")]
    public DateTime CreateDate { get; set; }

    [Column("ACTUAL_DELIVERY_DATE", TypeName = "DATE")]
    public DateTime? ActualDeliveryDate { get; set; }

    [Column("REQUEST_ID", TypeName = "NUMBER(28)")]
    public decimal RequestId { get; set; }

    [Column("REQUEST_DELIVERY_STATUS_ID", TypeName = "NUMBER")]
    public int RequestDeliveryStatusId { get; set; }

    [Column("DELIVERY_TYPE")]
    [StringLength(100)]
    public string DeliveryType { get; set; } = null!;

    [Column("DELIVERY_BRANCH_ID")]
    [StringLength(20)]
    public string? DeliveryBranchId { get; set; }

    [Column("DELIVERY_BRANCH_NAME")]
    [StringLength(200)]
    public string? DeliveryBranchName { get; set; }

    [Column("DELIVERY_BRANCH_STAFF_ID")]
    [StringLength(20)]
    public string? DeliveryBranchStaffId { get; set; }

    [Column("DELIVERY_BRANCH_STAFF_NAME")]
    [StringLength(200)]
    public string? DeliveryBranchStaffName { get; set; }

    [Column("JIRA_KEY")]
    [StringLength(20)]
    [Unicode(false)]
    public string? JiraKey { get; set; }

    [ForeignKey("RequestId")]
    public Request Request { get; set; }
}
