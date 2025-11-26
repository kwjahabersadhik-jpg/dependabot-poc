using CreditCardsSystem.Domain.Attributes;

namespace CreditCardsSystem.Domain.Models.Customer;

public class GenericCustomerProfileDto
{
    public string CivilId { get; set; }
    public string? RimStatus { get; set; }
    public int RimNo { get; set; }
    public string? RimClass { get; set; }
    public string? EnglishName => $"{FirstName} {LastName}";
    public string? ArabicName { get; set; }
    public string? TitleDescription { get; set; }
    public string? PhoneNumber { get; set; }
    public DateTime? BirthDate { get; set; }
    public string CustomerType { get; set; } = string.Empty;
    public string? EmployeeNumber { get; set; }
    public bool IsEmployee { get; set; }
    public DateTime? LastKYCDate { get; set; }
    public string? KYCAlerts { get; set; }
    public DateTime? CIDExpiryDate { get; set; }
    public string? FATCAStatus { get; set; }
    public string? FATCAw8 { get; set; }
    public string? FATCAw9 { get; set; }
    public int? CrsStatusId { get; set; }
    public string? CrsStatusDesc { get; set; }
    public int? CrsClassificationId { get; set; }
    public string? CrsClassificationDesc { get; set; }
    public List<CrsDetailDto>? CRSInfo { get; set; }
    public int? SICCode { get; set; }
    public string? MobileNumber { get; set; }
    public List<CustomerAddressDto>? CustomerAddresses { get; set; }
    public decimal? Income { get; set; }
    public string? disabilityType { get; set; }
    public bool PEP { get; set; }
    public string? Gender { get; set; }
    public string? MaritalStatus { get; set; }
    public bool? Blacklist { get; set; }
    public string? Profession { get; set; }
    public string? EmployerName { get; set; }
    public string? PositionID { get; set; }
    public string? Position { get; set; }
    public string? WorkAddress { get; set; }
    public string? DealingWithBankReasonID { get; set; }
    public string? DealingWithBankReason { get; set; }
    public string? TransactionType { get; set; }
    public int? RimCode { get; set; }
    public string ImageUrl { get; set; } = default!;
    public string Occupation { get; set; }
    public bool IsRetired { get; set; }
    public decimal OtherIncome { get; set; }

    public bool ZeroProfileInPhenix { get; set; }
    public bool IsKFHCustomer { get; set; } = true;
    public string EducationId { get; set; }
    public string Residency { get; set; }
    public string IncomeSource { get; set; }
    public string? HomePhoneNumber { get; set; }

    [ArabicValidator]
    public string FirstName { get; set; }

    [ArabicValidator]
    public string LastName { get; set; }
    public bool VIP { get; set; }
    public string? EmployeePositionDesc { get; set; }
    public string? NationalityId { get; set; }
    public int OpeningReasonId { get; set; }
    public string? SpecialNeed { get; set; }
    public string EmployeeDesc { get; set; }
    public string? NationalityEnglish { get; set; }
    public string? NationalityArabic { get; set; }
    public string ArabicRimType { get; set; }
    public bool AvailableInLocalDb { get; set; }
    public int? TitleId { get; set; }

    public bool IsPendingBioMetric { get; set; }
    public bool IsKYCExpired { get; set; }
}
