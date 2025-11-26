namespace CreditCardsSystem.Domain.Models;

public class CreditCardProfileDto
{
    public string CivilId { get; set; } = default!;

    public string ArabicName { get; set; } = default!;

    public string Area { get; set; } = default!;

    public DateTime Birth { get; set; }

    public bool BirthFieldSpecified { get; set; }

    public string BlockNo { get; set; } = default!;

    public string BuildNo { get; set; } = default!;

    public int Country { get; set; }

    public bool CountryFieldSpecified { get; set; }

    public string DurationService { get; set; } = default!;

    public string Email { get; set; } = default!;

    public string EmployerArea { get; set; } = default!;

    public string EmployerDepartment { get; set; } = default!;

    public string EmployerName { get; set; } = default!;

    public string EmployerSection { get; set; } = default!;

    public string FileNo { get; set; } = default!;

    public string FirstName { get; set; } = default!;

    public string FlatNo { get; set; } = default!;

    public string FullName { get; set; } = default!;

    public int Gender { get; set; }

    public bool GenderFieldSpecified { get; set; }

    public string HolderName { get; set; } = default!;

    public string LastName { get; set; } = default!;

    public string MiddleName { get; set; } = default!;

    public int Nationality { get; set; }

    public bool NationalityFieldSpecified { get; set; }

    public string OtherName { get; set; } = default!;

    public string SecondaryCivilId { get; set; } = default!;

    public string Street { get; set; } = default!;

    public string Title { get; set; } = default!;

    public string CustomerNumber { get; set; } = default!;
}