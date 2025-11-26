using System.ComponentModel.DataAnnotations;

namespace CreditCardsSystem.Domain.Attributes;

public class ArabicValidator : ValidationAttribute
{
    private new const string ErrorMessage = "Please remove Arabic characters";

    public ArabicValidator()
    {
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {

        if (value == null) { return ValidationResult.Success; }


        foreach (char charachter in value!.ToString()!.ToCharArray())
        {
            if (charachter >= 0x600 && charachter <= 0x6ff
             || charachter >= 0x750 && charachter <= 0x77f
             || charachter >= 0xfb50 && charachter <= 0xfc3f
            || charachter >= 0xfe70 && charachter <= 0xfefc)
            {
                return new ValidationResult(ErrorMessage);
            }
        }


        return ValidationResult.Success;
    }
}
