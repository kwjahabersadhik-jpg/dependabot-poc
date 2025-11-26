namespace CreditCardsSystem.Domain.Models.CardIssuance
{
    public class ValidateSellerIdResponse
    {
        public string EmpNo { get; set; } = null!;
        public string? Gender { get; set; }
        public string? NameAr { get; set; }
        public string? NameEn { get; set; }
    }
}
