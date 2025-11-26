namespace CreditCardsSystem.Domain.Models.RequestDelivery;

public class RequestDeliveryDto
{
    public DateTime CreateDate { get; set; }
    public decimal? RequestId { get; set; }
    public string DeliveryType { get; set; }
    public int RequestDeliveryStatusId { get; set; }
    public int? DeliveryBranchId { get; set; }
    public string? DeliveryBranchName { get; set; }
}
