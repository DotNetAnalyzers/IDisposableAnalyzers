namespace IDisposableAnalyzers
{
    using System;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;

    internal sealed class PooledHashSet<T> : IDisposable, IReadOnlyCollection<T>
    {
        private static readonly ConcurrentQueue<PooledHashSet<T>> Cache = new ConcurrentQueue<PooledHashSet<T>>();
        private readonly HashSet<T> inner = new HashSet<T>();

        private int refCount;

        private PooledHashSet()
        {
        }

        public int Count => this.inner.Count;

        public bool Add(T item)
        {
            this.ThrowIfDisposed();
            return this.inner.Add(item);
        }

        public IEnumerator<T> GetEnumerator() => this.inner.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)this.inner).GetEnumerator();

        public void Dispose()
        {
            this.refCount--;
            Debug.Assert(this.refCount >= 0, "refCount>= 0");
            if (this.refCount == 0)
            {
                this.inner.Clear();
                Cache.Enqueue(this);
            }
        }

        internal static PooledHashSet<T> Borrow()
        {
            if (!Cache.TryDequeue(out var set))
            {
                set = new PooledHashSet<T>();
            }

            set.refCount = 1;
            return set;
        }

        internal static PooledHashSet<T> BorrowOrIncrementUsage(PooledHashSet<T> set)
        {
            if (set == null)
            {
                return Borrow();
            }

            set.refCount++;
            return set;
        }

        [Conditional("DEBUG")]
        private void ThrowIfDisposed()
        {
            if (this.refCount <= 0)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }
    }
}
