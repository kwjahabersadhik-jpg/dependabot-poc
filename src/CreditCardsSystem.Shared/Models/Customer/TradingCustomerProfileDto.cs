namespace CreditCardsSystem.Domain.Models;

public class TradingCustomerProfileDto
{
    public string CustomerName { get; set; } = default!;
    public string CustomerNameArabic { get; set; } = default!;
    public string CivilId { get; set; } = default!;
    public string CustomerType { get; set; } = default!;
    public string Salary { get; set; } = default!;
    public decimal OtherSalary { get; set; }
    public decimal TotalSalary { get; set; }
    public string CustomerIndication { get; set; } = default!;
    public string CustomerNumber { get; set; } = default!;
    public string WorkLocation { get; set; } = default!;
    public string WorkAddress { get; set; } = default!;
}