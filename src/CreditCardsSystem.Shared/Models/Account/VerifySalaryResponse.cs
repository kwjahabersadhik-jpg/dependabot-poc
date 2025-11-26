namespace CreditCardsSystem.Domain.Models.Account;

public class VerifySalaryResponse
{
    public bool Verified { get; set; }
    public decimal Salary { get; set; }
    public string? Description { get; set; }
}
