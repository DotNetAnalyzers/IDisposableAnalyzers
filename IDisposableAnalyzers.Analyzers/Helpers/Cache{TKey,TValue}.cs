namespace IDisposableAnalyzers
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.Threading;

    internal static class Cache<TKey, TValue>
    {
        private static readonly ConcurrentDictionary<TKey, TValue> Inner = new ConcurrentDictionary<TKey, TValue>();
        //// ReSharper disable once StaticMemberInGenericType
        private static int refCount;

        public static void Begin()
        {
#pragma warning disable GU0011 // Don't ignore the return value.
            Interlocked.Increment(ref refCount);
#pragma warning restore GU0011 // Don't ignore the return value.
        }

        public static void End()
        {
#pragma warning disable GU0011 // Don't ignore the return value.
            Interlocked.Exchange(ref refCount, 0);
#pragma warning restore GU0011 // Don't ignore the return value.
            Inner.Clear();
        }

        internal static Transaction_ Transaction()
        {
#pragma warning disable GU0011 // Don't ignore the return value.
            Interlocked.Increment(ref refCount);
#pragma warning restore GU0011 // Don't ignore the return value.
            Debug.Assert(refCount > 0, "refCount > 0");
            return default(Transaction_);
        }

        internal static TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
        {
            return refCount == 0 ? valueFactory(key) : Inner.GetOrAdd(key, valueFactory);
        }

        // ReSharper disable once InconsistentNaming
        internal struct Transaction_ : IDisposable
        {
            public void Dispose()
            {
                if (Interlocked.Decrement(ref refCount) <= 0)
                {
                    Inner.Clear();
                }
            }
        }
    }
}
