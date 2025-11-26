namespace CreditCardsSystem.Domain.Models.Migs;

public class BlackListCardDto
{
    public string CardNo { get; set; }
    public bool IsSuspicious { get; set; }
    public bool IsSelected { get; set; }
}



public class SuspiciousCards
{
    public bool IsSuspicious { get; set; }
    public string[] CardsNos { get; set; }
}

public class BlackListCardResponse
{
    public string Message { get; set; }
}
