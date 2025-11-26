using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CreditCardsSystem.Data.Models;

[Table("CORPORATE_PROFILE")]
public partial class CorporateProfile
{
    [Key]
    [Column("CORPORATE_CIVIL_ID")]
    [StringLength(50)]
    [Unicode(false)]
    public string CorporateCivilId { get; set; } = null!;

    [Column("CORPORATE_NAME_EN")]
    [StringLength(80)]
    [Unicode(false)]
    public string CorporateNameEn { get; set; } = null!;

    [Column("CORPORATE_NAME_AR")]
    [StringLength(80)]
    [Unicode(false)]
    public string CorporateNameAr { get; set; } = null!;

    [Column("EMBOSSING_NAME")]
    [StringLength(50)]
    [Unicode(false)]
    public string EmbossingName { get; set; } = null!;

    [Column("GLOBAL_LIMIT", TypeName = "NUMBER")]
    public decimal GlobalLimit { get; set; }

    [Column("RIM_CODE", TypeName = "NUMBER")]
    public decimal RimCode { get; set; }

    [Column("RELATIONSHIP_NO")]
    [StringLength(50)]
    [Unicode(false)]
    public string? RelationshipNo { get; set; }

    [Column("CUSTOMER_NO")]
    [StringLength(50)]
    [Unicode(false)]
    public string? CustomerNo { get; set; }

    [Column("BILLING_ACCOUNT_NO")]
    [StringLength(50)]
    [Unicode(false)]
    public string? BillingAccountNo { get; set; }

    [Column("KFH_ACCOUNT_NO")]
    [StringLength(20)]
    [Unicode(false)]
    public string? KfhAccountNo { get; set; }

    [Column("ADDRESS_LINE1")]
    [StringLength(100)]
    [Unicode(false)]
    public string? AddressLine1 { get; set; }

    [Column("ADDRESS_LINE2")]
    [StringLength(100)]
    [Unicode(false)]
    public string? AddressLine2 { get; set; }
}
