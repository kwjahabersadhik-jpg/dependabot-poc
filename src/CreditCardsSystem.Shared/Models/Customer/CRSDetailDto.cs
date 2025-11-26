namespace CreditCardsSystem.Domain.Models.Customer;

public class CrsDetailDto
{
    public string CivilId { get; set; } = default!;
    public string Country { get; set; } = default!;
    public DateTime RequestedDate { get; set; }
}
