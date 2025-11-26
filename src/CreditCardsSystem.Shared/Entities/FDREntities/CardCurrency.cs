using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CreditCardsSystem.Data.Models;

[PrimaryKey("CurrencyId", "Org", "CurrencyIsoCode")]
[Table("CARD_CURRENCY")]
public partial class CardCurrency
{
    [Key]
    [Column("CURRENCY_ID")]
    [StringLength(20)]
    [Unicode(false)]
    public string CurrencyId { get; set; } = string.Empty;

    [Key]
    [Column("CURRENCY_ISO_CODE")]
    [StringLength(20)]
    [Unicode(false)]
    public string CurrencyIsoCode { get; set; } = string.Empty;

    [Key]
    [Column("ORG")]
    [StringLength(20)]
    [Unicode(false)]
    public string Org { get; set; } = string.Empty;

    [Column("CURRENCY_DECIMAL_PLACES")]
    [Precision(1)]
    public int? CurrencyDecimalPlaces { get; set; }

    [Column("CURRENCY_SHORT_NAME")]
    [StringLength(26)]
    [Unicode(false)]
    public string? CurrencyShortName { get; set; }
}
