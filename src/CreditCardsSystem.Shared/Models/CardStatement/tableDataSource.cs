using CreditCardsSystem.Domain.Common;
using System.Text.Json.Serialization;

namespace CreditCardsSystem.Domain.Models.CardStatement;

public class TableDataSource
{
    public List<CustomCardTransactionsDTO>? CreditCardTransaction { get; set; }
    public string AnnualPoints { get; set; }
    public string BonusPoints { get; set; }
    public string InternationalPoints { get; set; }
    public string LocalPoints { get; set; }
    public bool IsCobrand { get; set; }
    public decimal Cashback { get; set; } = 0;
    public decimal TotalDebit => CreditCardTransaction?.Where(t => !t.isCreditField).Sum(d => d.amountField) ?? 0;
    public decimal TotalCredit => CreditCardTransaction?.Where(t => t.isCreditField).Sum(d => d.amountField) * -1 ?? 0;
    public decimal TotalDeclined => CreditCardTransaction?.Where(t => t.descriptionField.Contains("Decline")).Sum(d => d.amountField) ?? 0;
    public decimal TotalHold => CreditCardTransaction?.Where(t => t.descriptionField.Contains("AUTH CODE")).Sum(d => d.amountField) ?? 0;

    [JsonIgnore]
    public string? FromDate { get; set; }

    [JsonIgnore]
    public string? ToDate { get; set; }

    [JsonIgnore]
    public string? CardFullNumber { get; set; }

    [JsonIgnore]
    public string? CustomerName { get; set; }

    [JsonIgnore]
    public int? Count => CreditCardTransaction?.Count;

    [JsonIgnore]
    public decimal? TotalAmount => CreditCardTransaction?.Sum(x=> x.amountField);

    [JsonIgnore]
    public string? PrintDate => DateTime.Now.ToString(ConfigurationBase.DateFormat);

    public string? CardCurrency { get; set; }
}
