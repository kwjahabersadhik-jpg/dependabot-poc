using System.ComponentModel.DataAnnotations;

namespace CreditCardsSystem.Domain.Models;

public class CustomerProfileSearchCriteria
{
    [RegularExpression(@"^\d{1,12}", ErrorMessage = "Invalid Civil ID")]
    public string? CivilId { get; set; }

    //TODO : Remove the below properties 
    [RegularExpression(@"^\d{1,12}", ErrorMessage = "Invalid AccountNumber")]
    public string? AccountNumber { get; set; }

    [RegularExpression(@"^\d{1,12}", ErrorMessage = "Invalid RimNumber")]
    public string? RimNumber { get; set; }

    [RegularExpression(@"^[a-zA-Z\d\s]{1,100}$", ErrorMessage = "Invalid CompanyRegistration")]
    public string? CompanyRegistration { get; set; }

    public bool IsEmpty()
    {
        return !string.IsNullOrEmpty(CivilId) && !string.IsNullOrEmpty(AccountNumber) && !string.IsNullOrEmpty(RimNumber) && !string.IsNullOrEmpty(CompanyRegistration);
    }
}