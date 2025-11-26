using CreditCardsSystem.Utility.Crypto;

namespace CreditCardsSystem.Utility.Extensions;

public static class StringExtensions
{

    public static string ToCamelCase(this string str)
    {
        if (string.IsNullOrEmpty(str) || str.Length < 2)
            return str.ToLowerInvariant();

        if (str.Split(".").Length > 0)
            return string.Join(".", str.Split(".").Select(x => convertToCamel(x)));

        return convertToCamel(str);

        static string convertToCamel(string value) => char.ToLowerInvariant(value[0]) + value.Substring(1);
    }



    public static string IsNullOrDefault(this string? value, string defaultValue)
    {
        if (string.IsNullOrEmpty(value)) return defaultValue;

        return value;
    }

    public static T? ToEnum<T>(this string value) where T : Enum
    {
        if (Enum.TryParse(typeof(T), value, out object _enumObj))
            return (T)_enumObj;

        return default(T);
    }
    public static string Masked(this string? source, int start, int count)
    {
        try
        {
            return string.IsNullOrEmpty(source?.Trim()) ? string.Empty : source.Masked('X', start, count);
        }
        catch (Exception ex)
        {
            return source;
        } 
    }

    public static string SplitByFour(this string? source)
    {
        if (string.IsNullOrEmpty(source))
            return source;

        try
        {
            return $"{source.Substring(0, 4)} {source.Substring(4, 4)} {source.Substring(8, 4)} {source.Substring(12, 4)}";
        }
        catch (Exception)
        {
            return source;
        }

    }
    public static string Masked(this string source, char maskValue, int start, int count)
    {
        var firstPart = source[..start];
        var lastPart = source[(start + count)..];
        var middlePart = new string(maskValue, count);

        return firstPart + middlePart + lastPart;
    }

    public static bool HasArabicCharacters(this string source)
    {
        foreach (char charachter in source.ToCharArray())
        {
            if (charachter >= 0x600 && charachter <= 0x6ff) return true;
            if (charachter >= 0x750 && charachter <= 0x77f) return true;
            if (charachter >= 0xfb50 && charachter <= 0xfc3f) return true;
            if (charachter >= 0xfe70 && charachter <= 0xfefc) return true;
        }
        return false;
    }
    public static string Formed(this DateTime datetime, string format = "dd/MM/yyyy")
    {
        if (datetime == DateTime.MinValue || datetime == DateTime.MaxValue || datetime == default)
            return "";

        return datetime.ToString(format);
    }
    public static string ToMoney(this decimal value, string currency = "KWD", int? decimalPlaces = 3)
    {
        var amount = value == default ? "0" : value.ToString($"n{decimalPlaces ?? 3}");
        return $"{amount} {currency}";
    }

    public static string ToMoney(this decimal? value, string currency = "KWD", int? decimalPlaces = 3) => ToMoney(value ?? default, currency, decimalPlaces);

    public static string ToText(this bool value)
    {
        return value ? "Yes" : "No";
    }
    public static string ToExpiryDate(this string stringDate, bool saltIt = false)
    {
        if (string.IsNullOrEmpty(stringDate)) return string.Empty;

        var value = $"{stringDate.Substring(4, 2)}{stringDate.Substring(0, 4)}";

        return saltIt ? value.SaltThis() : value;
    }
    public static string ToStandingOrderFormat(this DateTime datetime)
    {
        if (datetime == DateTime.MinValue || datetime == DateTime.MaxValue)
            return "";

        var date = (datetime.Day < 10 ? "0" : "") + datetime.Day.ToString();
        var month = (datetime.Month < 10 ? "0" : "") + datetime.Month.ToString();
        var year = datetime.Year.ToString();

        return $"{date}-{month}-{year}";
    }

    //public static long? ToLong(this string? value)
    //{
    //    _ = Int64.TryParse(value, out Int64 result);
    //    return result;
    //}

    //public static int? ToInt(this string? value)
    //{
    //    _ = Int32.TryParse(value, out Int32 result);
    //    return result;
    //}
}