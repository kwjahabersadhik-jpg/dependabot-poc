namespace CreditCardsSystem.Domain.Models.Customer;

public class CustomerAddressDto
{
    public int AddressId { get; set; }
    public int AddressTypeId { get; set; }
    public string State { get; set; } = default!;
    public string CountryCode { get; set; } = default!;
    public string ZipCode { get; set; } = default!;
    public string AddressLine1 { get; set; } = default!;
    public string AddressLine2 { get; set; } = default!;
    public string AddressLine3 { get; set; } = default!;
    public string PhoneNumber1 { get; set; } = default!;
    public string PhoneNumber1Extension { get; set; } = default!;
    public string PhoneNumber2 { get; set; } = default!;
    public string PhoneNumber2Extension { get; set; } = default!;
    public string PhoneNumber3 { get; set; } = default!;
    public string PhoneNumber3Extension { get; set; } = default!;
    public string FaxNumber { get; set; } = default!;
    public string EmailAddress { get; set; } = default!;
    public string District { get; set; } = default!;
    public string Region { get; set; } = default!;
    public string CityId { get; set; } = default!;
    public string CityName { get; set; } = default!;
    public string PostBoxNumber { get; set; }
    public string BlockNumber { get; set; }
    public string House { get; set; }
    public string Street { get; set; }
    public string RegionId { get; set; }
    public string FlatNumber { get; set; }
}