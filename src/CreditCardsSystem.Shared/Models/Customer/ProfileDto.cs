using System.ComponentModel.DataAnnotations;

namespace CreditCardsSystem.Domain.Models.Customer;

public class ProfileDto
{

    public string CivilId { get; set; } = null!;

    [Required]
    public int? Title { get; set; } = null!;
    public string? FullName { get; set; }
    public string? ArabicName { get; set; }
    public string? OtherName { get; set; }

    [StringLength(maximumLength: 26)]
    public string HolderName => $"{FirstName} {LastName}";

    public int Gender { get; set; }
    [Required]
    public DateTime Birth { get; set; }
    public int? Country { get; set; }

    [Required]
    public int? Nationality { get; set; }

    [Required]
    public string EmployerName { get; set; }


    [Required]
    public string? FirstName { get; set; }


    public string? MiddleName { get; set; }

    [Required]
    public string? LastName { get; set; }


    public string? ArabicFirstName { get; set; }
    public string? ArabicMiddleName { get; set; }

    public string? ArabicLastName { get; set; }


    public string? FileNo { get; set; }

    [Required]
    public string? Email { get; set; }

    [Required]
    public string? Area { get; set; }

    [Required]
    public string? Street { get; set; }

    [Required]
    public string? BlockNo { get; set; }

    [Required]
    public string? Buildno { get; set; }

    [Required]
    public string? Flatno { get; set; }


    [Required]
    public string? EmployerSection { get; set; }


    public string? EmployerDepartment { get; set; }


    public string? EmployerArea { get; set; }


    public string? DurationService { get; set; }


    public string? SecondaryCivilId { get; set; }


    public string? CustomerNo { get; set; }


    public string? YouthCusttomerNo { get; set; }

    [Required]
    public DateTime? CIDExpiryDate { get; set; }

    public bool? IsAUB { get; set; }

}
