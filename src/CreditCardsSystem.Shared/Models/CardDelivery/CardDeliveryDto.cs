namespace CreditCardsSystem.Domain.Models.CardDelivery
{
    public class CardDeliveryDto
    {
        public long FileReference_ID { get; set; }
        public string? CardNumber { get; set; }
        public string? CivilID { get; set; }
        public int? CardType { get; set; }
        public DateTime? LoadDate { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string? DeliveryStatusEN { get; set; }
        public string? DeliveryStatusAR { get; set; }
        public string? CourierNameEN { get; set; }
        public string? CourierNameAR { get; set; }
        public string? BranchName { get; set; }
        public string? NameEN { get; set; }
        public string? NameAR { get; set; }
        public int CardActionClassification_ID { get; set; }
        public int? CardDeliveryMethod_ID { get; set; }
        public string? DeliveredByEN { get; set; }
        public string? DeliveredByAR { get; set; }
        public string? ReceivedBy { get; set; }
        public long? CCEF_ID { get; set; }
        public string? IssuanceReasonEN { get; set; }
        public DateTime? CancelDeliveryOn { get; set; }
        public DateTime? CourierReturnDate { get; set; }
        public DateTime? SendToBranchAfterReturnDate { get; set; }
        public string? SendToBranchReasonAR { get; set; }
        public string? SendToBranchReasonEN { get; set; }
        public string? CourierReturnReasonAR { get; set; }
        public string? CourierReturnReasonEN { get; set; }
        public string? DeliveryStatusAndMethodEN { get; set; }
        public string? DeliveryStatusAndMethodAr { get; set; }
    }
}
