using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CreditCardsSystem.Data.Models;

[Table("CARDTYPE_ELIGIBILITY_MATIX", Schema = "PROMO")]
public partial class CardtypeEligibilityMatix
{
    [Key]
    [Column("ID", TypeName = "NUMBER")]
    public int Id { get; set; }

    [Column("CARD_TYPE", TypeName = "NUMBER")]
    [Precision(2)]
    public int CardType { get; set; }

    [Column("ALLOWED_BRANCHES")]
    [StringLength(20)]
    public string? AllowedBranches { get; set; }

    [Column("IS_DISABLED")]
    public bool? IsDisabled { get; set; }

    [Column("ALLOWED_CLASS_CODE")]
    [StringLength(20)]
    public string? AllowedClassCode { get; set; }

    [Column("IS_COBRAND_PREPAID")]
    public bool? IsCobrandPrepaid { get; set; }

    [Column("ALLOWED_NON_KFH")]
    public bool? AllowedNonKfh { get; set; }

    [Column("IS_CORPORATE")]
    public bool? IsCorporate { get; set; }

    [Column("IS_COBRAND_CREDIT")]
    public bool? IsCobrandCredit { get; set; }

    [Column("ISLOCKED")]
    public bool? Islocked { get; set; }
}
