using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CreditCardsSystem.Data.Models;

[PrimaryKey("CompanyId", "CardType")]
[Table("COMPANY")]
public partial class Company
{
    [Key]
    [Column("COMPANY_ID")]
    [Precision(2)]
    public int CompanyId { get; set; }

    [Column("COMPANY_NAME")]
    [StringLength(50)]
    [Unicode(false)]
    public string CompanyName { get; set; } = null!;

    [Key]
    [Column("CARD_TYPE")]
    [Precision(2)]
    public int CardType { get; set; }

    [Column("CLUB_NAME")]
    [StringLength(20)]
    [Unicode(false)]
    public string? ClubName { get; set; }

    [Column("BONUS_POINTS")]
    [Precision(10)]
    public int? BonusPoints { get; set; }

    [Column("BONUS_EQUIVALENT_AMOUNT", TypeName = "NUMBER(15,5)")]
    public decimal? BonusEquivalentAmount { get; set; }

    [Column("COMPANY_LETTER")]
    [StringLength(20)]
    [Unicode(false)]
    public string? CompanyLetter { get; set; }

    [Column("BONUS")]
    [StringLength(1)]
    [Unicode(false)]
    public string? Bonus { get; set; }

    [Column("ANNUAL")]
    [StringLength(1)]
    [Unicode(false)]
    public string? Annual { get; set; }

    [Column("CARD_DESC")]
    [StringLength(20)]
    [Unicode(false)]
    public string? CardDesc { get; set; }
}
