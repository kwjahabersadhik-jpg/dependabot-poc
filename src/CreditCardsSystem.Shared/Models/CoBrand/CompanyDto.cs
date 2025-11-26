namespace CreditCardsSystem.Domain.Models.CoBrand
{
    public class CompanyDto
    {
        public int CompanyId { get; set; }
        public string CompanyName { get; set; } = null!;
        public int CardType { get; set; }
        public string? ClubName { get; set; }
        public int? BonusPoints { get; set; }
        public decimal? BonusEquivalentAmount { get; set; }
        public string? CompanyLetter { get; set; }
        public string? Bonus { get; set; }
        public string? Annual { get; set; }
        public string? CardDesc { get; set; }
        public decimal? BonusMinspendamount { get; set; }
        public decimal? BonusMinspendmonths { get; set; }
    }

    public record CompanyLookup(int CompanyId, string CompanyName, int CardType, string? ClubName);
}

