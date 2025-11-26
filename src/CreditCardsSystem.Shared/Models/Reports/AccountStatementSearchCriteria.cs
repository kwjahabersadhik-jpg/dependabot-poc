namespace CreditCardsSystem.Domain.Models.Reports;

public class AccountStatementSearchCriteria
{
    public string AccountNumber { get; set; } = default!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public AccountStatementSearchCriteriaEnum SearchCriteria { get; set; }
}