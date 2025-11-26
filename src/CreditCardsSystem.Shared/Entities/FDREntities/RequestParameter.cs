using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CreditCardsSystem.Data.Models;

//[Keyless]
[Table("REQUEST_PARAMETERS")]
[Index("ReqId", Name = "REQ_ID_IND")]
public partial class RequestParameter
{
    [Column("REQ_ID", TypeName = "NUMBER(28)")]
    [Required]
    public decimal ReqId { get; set; }

    [Column("PARAMETER")]
    [StringLength(40)]
    [Unicode(false)]
    public string Parameter { get; set; } = null!;

    [Column("VALUE")]
    [StringLength(250)]
    [Unicode(false)]
    public string Value { get; set; } = null!;


    //[ForeignKey("REQ_ID")]
    [InverseProperty("Parameters")]
    public virtual Request RequestParameterNavigation { get; set; } = null!;
}
