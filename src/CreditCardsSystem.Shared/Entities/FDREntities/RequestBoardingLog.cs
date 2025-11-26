using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CreditCardsSystem.Data.Models;

[Table("REQUEST_BOARDING_LOG", Schema = "VPBCD")]
public partial class RequestBoardingLog
{
    [Key]
    [Column("REQ_ID", TypeName = "NUMBER(28)")]
    public decimal ReqId { get; set; }

    [Column("CLIENT_NAME")]
    [StringLength(100)]
    [Unicode(false)]
    public string? ClientName { get; set; }

    [Column("CHANNEL_ID")]
    [StringLength(200)]
    [Unicode(false)]
    public string? ChannelId { get; set; }

    [Column("LOG_DATE")]
    [Precision(6)]
    public DateTime? LogDate { get; set; }
}
