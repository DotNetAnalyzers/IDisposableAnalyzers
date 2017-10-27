namespace IDisposableAnalyzers
{
    using System;
    using System.Collections.Concurrent;

    internal static class ConcurrentQueueExt
    {
        internal static T GetOrCreate<T>(this ConcurrentQueue<T> queue, Func<T> create)
        {
            if (queue.TryDequeue(out T item))
            {
                return item;
            }

            return create();
        }
    }
}