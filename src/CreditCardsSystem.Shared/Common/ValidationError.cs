using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace CreditCardsSystem.Domain.Common;

public class ValidationError
{
    public string? Property { get; set; }
    public string Error { get; set; } = string.Empty;
    public ValidationError(string property, string error)
    {
        Property = property; Error = error;
    }
    public ValidationError(string error)
    {
        Error = error;
    }
    [JsonConstructor]
    public ValidationError()
    {

    }
}

public class ValidateModel<T> where T : ValidateModel<T>, new()
{
    public Task ModelValidationAsync([CallerMemberName] string methodName = "")
    {
        var errors = new List<ValidationError>();
        var results = Validate(new ValidationContext(this));

        foreach (var result in results)
        {
            var propertyName = result.MemberNames.FirstOrDefault();
            errors.Add(string.IsNullOrEmpty(propertyName) ? new(result?.ErrorMessage!) : new(propertyName, result?.ErrorMessage!));
        }
        if (errors.Any()) throw new ApiException(errors, methodName, "validation error");

        return Task.CompletedTask;

        IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var errors = new List<ValidationError>();
            var results = new List<ValidationResult>();

            Validator.TryValidateObject(this, validationContext, results, true);

            if (results.Count > 0)
                foreach (var item in results)
                {
                    var propertyName = item.MemberNames.FirstOrDefault();
                    errors.Add(string.IsNullOrEmpty(propertyName) ? new(item?.ErrorMessage!) : new(propertyName, item?.ErrorMessage!));
                }


            foreach (var propertyInfo in GetType().GetProperties())
            {
                var baseclass = propertyInfo.GetValue(this, null);
                if (baseclass != null)
                {
                    var context = new ValidationContext(baseclass, validationContext, validationContext.Items);
                    Validator.TryValidateObject(baseclass, context, results, true);

                    if (results.Count > 0)
                        foreach (var item in results)
                        {
                            var propertyName = item.MemberNames.FirstOrDefault();
                            errors.Add(string.IsNullOrEmpty(propertyName) ? new(item?.ErrorMessage!) : new(propertyName, item?.ErrorMessage!));
                        }
                }

            }
            return results;
        }
    }

}

