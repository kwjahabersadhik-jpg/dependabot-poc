using System.ComponentModel.DataAnnotations;

namespace CreditCardsSystem.Domain.Attributes;

public class SequentialValidator : ValidationAttribute
{

    public SequentialValidator()
    {
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {

        if (value == null) { return ValidationResult.Success; }

        var IsSequenceEqual = () =>
        {
            var series = Array.ConvertAll(value!.ToString()!.ToArray(), s => int.Parse(s.ToString()));
            return Enumerable.Range(series.First(), series.Count()).SequenceEqual(series);
        };

        if (IsSequenceEqual())
            return new ValidationResult("MemberShip Id should not sequential digits");

        return ValidationResult.Success;
    }
}
