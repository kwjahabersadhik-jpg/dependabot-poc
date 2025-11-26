namespace CreditCardsSystem.Domain.Models.Fees;
public class ServiceFeesRequest
{
    public string ServiceName { get; set; } = null!;
    public string DebitAccountNumber { get; set; } = null!;

}

public class PostServiceFeesRequest
{
    public string ServiceName { get; set; } = null!;
    public string DebitAccountNumber { get; set; } = null!;
    public decimal? OverwriteFeesAmount { get; set; } = null!;
    public decimal? OriginalFeesAmount { get; set; } = null!;

    public string OverwriteReason { get; set; } = null!;

    public bool? OverwriteFeesAmountSpecified { get; set; } = null!;

    public bool? OriginalFeesAmountSpecified { get; set; } = null!;
}


public class ServiceFeesResponse
{
    public string TransRefNumber { get; set; } = null!;

    public decimal Fees { get; set; }
    public bool IsVatApplicable { get; set; }
    public decimal VatPercentage { get; set; }

}
