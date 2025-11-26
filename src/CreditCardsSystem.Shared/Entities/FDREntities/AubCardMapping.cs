using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CreditCardsSystem.Data.Models;

[PrimaryKey("AubCardNo", "KfhCardNo")]
[Table("AUB_CARD_MAPPING", Schema = "VPBCD")]
[Index("AubCardNo", Name = "AUB_CARD_MAPPING_INDEX1")]
[Index("KfhCardNo", Name = "AUB_CARD_MAPPING_INDEX2")]
public partial class AubCardMapping
{
    [Key]
    [Column("AUB_CARD_NO")]
    [StringLength(19)]
    [Unicode(false)]
    public string AubCardNo { get; set; } = null!;

    [Key]
    [Column("KFH_CARD_NO")]
    [StringLength(19)]
    [Unicode(false)]
    public string KfhCardNo { get; set; } = null!;

    [Column("FD_ACCT_NO")]
    [StringLength(19)]
    [Unicode(false)]
    public string? FdAcctNo { get; set; }

    [Column("FD_CUSTOMER_NO")]
    [StringLength(19)]
    [Unicode(false)]
    public string? FdCustomerNo { get; set; }

    [Column("DUALITY")]
    [Precision(1)]
    public bool Duality { get; set; }

    [Column("KFH_SECONDARY_CARD_NO")]
    [StringLength(19)]
    [Unicode(false)]
    public string? KfhSecondaryCardNo { get; set; }

    [Column("PRIMARY_CIVIL_ID")]
    [StringLength(25)]
    [Unicode(false)]
    public string? PrimaryCivilId { get; set; }

    [Column("SUPPLEMENTARY_CIVIL_ID")]
    [StringLength(25)]
    [Unicode(false)]
    public string? SupplementaryCivilId { get; set; }

    [Column("IS_RENEWED")]
    [Precision(1)]
    public bool? IsRenewed { get; set; }
}
