namespace IDisposableAnalyzers
{
    using System;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;

    [DebuggerTypeProxy(typeof(PooledSetDebugView<>))]
    [DebuggerDisplay("Count = {this.Count}, refCount = {this.refCount}")]
    internal sealed class PooledSet<T> : IDisposable, IReadOnlyCollection<T>
    {
        private static readonly ConcurrentQueue<PooledSet<T>> Cache = new ConcurrentQueue<PooledSet<T>>();
        private readonly HashSet<T> inner = new HashSet<T>();

        private int refCount;

        private PooledSet()
        {
        }

        public int Count => this.inner.Count;

        public bool Add(T item)
        {
            this.ThrowIfDisposed();
            return this.inner.Add(item);
        }

        public IEnumerator<T> GetEnumerator() => this.inner.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        void IDisposable.Dispose()
        {
            var current = Interlocked.Decrement(ref this.refCount);
            if (current == 0)
            {
                this.inner.Clear();
                Cache.Enqueue(this);
            }
            else if (current < 0)
            {
                Debug.Assert(current >= 0, $"{nameof(IDisposable.Dispose)} set.refCount == {current}");
            }
        }

        internal static PooledSet<T> Borrow()
        {
            if (Cache.TryDequeue(out var set))
            {
                Debug.Assert(set.refCount == 0, $"{nameof(Borrow)} set.refCount == {set.refCount}");
                set.refCount = 1;
                return set;
            }

            return new PooledSet<T> { refCount = 1 };
        }

        internal static PooledSet<T> BorrowOrIncrementUsage(PooledSet<T> set)
        {
            if (set == null)
            {
                return Borrow();
            }

            var current = Interlocked.Increment(ref set.refCount);
            Debug.Assert(current >= 1, $"{nameof(BorrowOrIncrementUsage)} set.refCount == {current}");
            return set;
        }

        [Conditional("DEBUG")]
        private void ThrowIfDisposed()
        {
            if (this.refCount <= 0)
            {
                Debug.Assert(this.refCount == 0, $"{nameof(this.ThrowIfDisposed)} set.refCount == {this.refCount}");
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }
    }
}
