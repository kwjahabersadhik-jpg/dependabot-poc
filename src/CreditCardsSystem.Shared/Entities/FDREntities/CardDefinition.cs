using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CreditCardsSystem.Data.Models;

[Table("CARD_DEF")]
public partial class CardDefinition
{
    [Key]
    [Column("CARD_TYPE")]
    public int CardType { get; set; }

    [Column("NAME")]
    [StringLength(50)]
    public string Name { get; set; } = null!;

    [Column("BIN_NO")]
    public int BinNo { get; set; }

    [Column("SYSTEM_NO")]
    public int SystemNo { get; set; }

    [Column("DUALITY")]
    public int Duality { get; set; }

    [Column("MERCHANT_ACCT")]
    [StringLength(19)]
    public string? MerchantAcct { get; set; }

    [Column("MIN_LIMIT")]
    [StringLength(10)]
    public string? MinLimit { get; set; }

    [Column("MAX_LIMIT")]
    [StringLength(10)]
    public string? MaxLimit { get; set; }

    [Column("FEES", TypeName = "NUMBER")]
    public decimal? Fees { get; set; }

    [Column("MONTHLY_MAX_DUE", TypeName = "NUMBER")]
    public decimal? MonthlyMaxDue { get; set; }

    [Column("INSTALLMENTS", TypeName = "NUMBER")]
    public decimal? Installments { get; set; }

    [Column("ISLOCKED")]
    [Precision(1)]
    public bool? Islocked { get; set; }

    [InverseProperty("CardTypeNavigation")]
    public virtual ICollection<CardDefinitionExtention> CardDefExts { get; set; } = new List<CardDefinitionExtention>();
}
