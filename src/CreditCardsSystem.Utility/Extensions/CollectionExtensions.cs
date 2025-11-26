namespace CreditCardsSystem.Utility.Extensions;

public static class CollectionExtensions
{
    public static bool AnyWithNull<T>(this IEnumerable<T>? source) => source is null ? false : source.Any();
    public static bool AnyWithNull<T>(this IEnumerable<T>? source, Func<T, bool> predicate) => source is null ? false : source.Any(predicate);
}

public static class DictionaryExtensions
{
    public static void AddUnique<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue value)
    {
        if (dictionary.ContainsKey(key)) return;

        if (value is null) return;

        dictionary.Add(key, value);
    }
}