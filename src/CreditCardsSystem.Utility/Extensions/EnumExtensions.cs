using System.ComponentModel;
using System.Reflection;

namespace CreditCardsSystem.Utility.Extensions;

public static class EnumExtensions
{

    public static string GetDescription<T>(this T source)
    {
        if (source is null) throw new ArgumentNullException("source");

        FieldInfo? fi = source.GetType().GetField(source.ToString() ?? "");

        var descriptions = fi?.GetCustomAttributes<DescriptionAttribute>(false);

        if (descriptions is null || !descriptions.Any()) return source.ToString() ?? "";

        return descriptions.Single().Description;
    }
}