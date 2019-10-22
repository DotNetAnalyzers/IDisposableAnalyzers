namespace IDisposableAnalyzers
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;

    [DebuggerTypeProxy(typeof(SmallSetDebugView<>))]
    [DebuggerDisplay("Count = {this.Count}")]
    internal class SmallSet<T> : IReadOnlyList<T>
    {
        private readonly List<T> inner = new List<T>();

        public int Count => this.inner.Count;

        public T this[int index] => this.inner[index];

        public IEnumerator<T> GetEnumerator() => this.inner.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)this.inner).GetEnumerator();

        internal void RemoveAll(Predicate<T> match) => this.inner.RemoveAll(match);

        internal void Clear() => this.inner.Clear();

        internal bool Contains(T item) => this.inner.Contains(item);

        internal bool Add(T item)
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
