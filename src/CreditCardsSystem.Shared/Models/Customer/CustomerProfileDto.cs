using CreditCardsSystem.Domain.Models.Customer;

namespace CreditCardsSystem.Domain.Models;

public class CustomerProfileDto
{
    public string CivilId { get; set; } = default!;
    public string FirstName { get; set; } = default!;
    public string FirstNameArabic { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string LastNameArabic { get; set; } = default!;
    public int RimNumber { get; set; }
    public string RimStatus { get; set; } = default!;
    public DateTime? DateOfBirth { get; set; }
    public string CustomerType { get; set; } = default!;
    public string RimClass { get; set; } = default!;
    public List<CustomerAddressDto>? CustomerAddresses { get; set; }
    public string Gender { get; set; } = default!;
    public string EnglishName { get; set; }
    public string? PhoneNumber { get; set; }
    public string ImageUrl { get; set; } = default!;
    public string RimCode { get; set; }
    public string? EmployeeNumber { get; set; }
    public bool IsEmployee { get; set; }
}