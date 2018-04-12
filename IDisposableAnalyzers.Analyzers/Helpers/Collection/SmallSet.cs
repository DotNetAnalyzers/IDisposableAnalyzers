namespace IDisposableAnalyzers
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;

    [DebuggerTypeProxy(typeof(SmallSetDebugView<>))]
    [DebuggerDisplay("Count = {this.Count}, refCount = {this.refCount}")]
    internal class SmallSet<T> : IReadOnlyList<T>
    {
        private readonly List<T> inner = new List<T>();

        public int Count => this.inner.Count;

        public T this[int index] => this.inner[index];

        public IEnumerator<T> GetEnumerator() => this.inner.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)this.inner).GetEnumerator();

        public void RemoveAll(Predicate<T> match) => this.inner.RemoveAll(match);

        public void Clear() => this.inner.Clear();

        public bool Contains(T item) => this.inner.Contains(item);

        public bool Add(T item)
        {
            if (!this.inner.Contains(item))
            {
                this.inner.Add(item);
                return true;
            }

            return false;
        }
    }
}
