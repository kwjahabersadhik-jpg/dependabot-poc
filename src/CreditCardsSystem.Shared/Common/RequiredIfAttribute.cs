using System.ComponentModel.DataAnnotations;

namespace CreditCardsSystem.Domain.Shared.Common;

public class RequiredIfAttribute : ValidationAttribute
{
    public string PropertyName { get; set; }
    public object? Value { get; set; }

    public RequiredIfAttribute(string propertyName, object? value, string errorMessage = "")
    {
        PropertyName = propertyName;
        Value = value;
        ErrorMessage = errorMessage;
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {

        if (string.IsNullOrEmpty(PropertyName))
            throw new Exception("Property Name is missing!");


        var propertyValue = GetPropertyValue(validationContext);

        if (IsPropertyHasValue(propertyValue) && (value == null || value.ToString() == ""))
            return new ValidationResult(ErrorMessage);
        else
            return ValidationResult.Success;
    }

    private object? GetPropertyValue(ValidationContext validationContext)
    {
        var propertyObject = validationContext.ObjectInstance;
        var propertyType = propertyObject.GetType();
        return propertyType?.GetProperty(PropertyName)?.GetValue(propertyObject);
    }


    private bool IsPropertyHasValue(object? propertyValue)
    {
        if (Value != null)
            return Value.ToString() == propertyValue?.ToString();
        else
            return propertyValue?.ToString() == "";
    }
}
