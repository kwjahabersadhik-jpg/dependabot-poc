namespace CreditCardsSystem.Domain.Models.Migs;

public class FraudMonitorDto
{
    public string CardNo { get; set; }
    public bool IsSuspicious { get; set; }
    public bool IsSelected { get; set; }
}



public class FraudMonitorResponse
{
    public string Message { get; set; }
}
