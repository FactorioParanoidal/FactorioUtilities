namespace FactorioParanoidal.FactorioMods;

internal static class ExtensionMethods {
    public static TValue GetOrCreate<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TValue> createExpression) {
        if (!dictionary.TryGetValue(key, out var value)) {
            value = dictionary[key] = createExpression();
        }

        return value;
    }
}