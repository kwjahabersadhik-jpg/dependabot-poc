namespace CreditCardsSystem.Domain.Models.CardIssuance
{

    public class EligibleCardRequest
    {
        public string? CivilId { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? RimCode { get; set; }
        public string? CustomerType { get; set; }
        public int? KfhId { get; set; }

    }

    public class EligibleCardRequestByCivilId
    {
        public string? CivilId { get; set; }
        public int? KfhId { get; set; }
    }
}
