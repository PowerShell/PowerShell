using System;
using System.Collections.Generic;
using System.Linq;

internal static class DictionaryExtensions
{
    public static void Add<TKey, TValue>(this IDictionary<TKey, List<TValue>> dictionary, TKey key, TValue value)
    {
        dictionary.GetOrAdd(key).Add(value);
    }

    public static bool Contains<TKey, TValue>(this IDictionary<TKey, List<TValue>> dictionary, TKey key, TValue value, IEqualityComparer<TValue> valueComparer)
    {
        List<TValue> values;

        if (!dictionary.TryGetValue(key, out values))
            return false;

        return values.Contains(value, valueComparer);
    }

    public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
        where TValue : new()
    {
        return dictionary.GetOrAdd<TKey, TValue>(key, () => new TValue());
    }

    public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TValue> newValue)
    {
        TValue result;

        if (!dictionary.TryGetValue(key, out result))
        {
            result = newValue();
            dictionary[key] = result;
        }

        return result;
    }
}
