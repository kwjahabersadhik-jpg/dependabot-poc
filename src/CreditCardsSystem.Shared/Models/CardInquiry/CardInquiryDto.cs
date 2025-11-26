namespace CreditCardsSystem.Domain.Models.CardDelivery
{
    public class CardInquiryDto
    {

        public string? CardNumber { get; set; }
        public decimal? AmountDelinquent { get; set; }
        public int DaysDelinquent { get; set; }
        public decimal? PaymentDue { get; set; }

        public decimal? CurrentBalance { get; set; }
        public decimal? CardFees { get; set; }
        public decimal? ApprovedLimit { get; set; }
        public int CardType { get; set; }
        public string? CivilId { get; set; }

    }
}
