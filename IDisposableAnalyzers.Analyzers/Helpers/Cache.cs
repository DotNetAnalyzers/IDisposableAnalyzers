namespace IDisposableAnalyzers
{
    using System;

    internal static class Cache
    {
        internal static TValue GetOrAdd<TKey, TValue>(TKey key, Func<TKey, TValue> valueFactory) => Cache<TKey, TValue>.GetOrAdd(key, valueFactory);
    }
}
