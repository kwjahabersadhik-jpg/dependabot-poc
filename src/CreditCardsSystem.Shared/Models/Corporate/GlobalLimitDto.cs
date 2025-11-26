namespace CreditCardsSystem.Domain.Models.Corporate;

public class GlobalLimitDto
{
    public string? CommitmentType { get; set; }
    public string? CommitmentNo { get; set; }
    public double Amount { get; set; }
    public double UndisbursedAmount { get; set; }
    public System.DateTime MaturityDate { get; set; }
    public bool MaturityDateSpecified { get; set; }
    public string? Status { get; set; }
    public decimal totalUsedLimit { get; set; }
}
