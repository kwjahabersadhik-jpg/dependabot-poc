using System.Linq.Expressions;

namespace CreditCardsSystem.Domain.Common;

public static class ValidationErrorExtension
{
    public static void Add(this List<ValidationError> errors, Expression<Func<object>> field, string message)
    {
        errors.Add(new(field?.Name ?? "", message));
    }
}
