namespace CreditCardsSystem.Domain.Models.Migs;

[AttributeUsage(AttributeTargets.All)]
public class ReportHeaderAttribute(string header) : Attribute()
{
    public string ColumnHeader { get; set; } = header;
}
