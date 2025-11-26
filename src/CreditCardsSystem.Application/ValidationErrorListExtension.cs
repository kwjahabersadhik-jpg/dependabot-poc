using CreditCardsSystem.Domain.Common;

namespace CreditCardsSystem.Application;

public static class ValidationErrorListExtension
{
    public static void ThrowErrorsIfAny<T>(this T validationErrors) where T : List<ValidationError>
    {
        if (validationErrors.Any())
            throw new ApiException(validationErrors, message: "Error found!");
    }

    public static void AddAndThrow<T>(this T validationErrors, ValidationError error) where T : List<ValidationError>
    {
        validationErrors.Add(error);

        if (validationErrors.Any())
            throw new ApiException(validationErrors, message: "Error found!");
    }
}
