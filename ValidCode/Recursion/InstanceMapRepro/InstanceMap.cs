namespace ValidCode.Recursion.InstanceMapRepro
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;

    internal class InstanceMap<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
        where TKey : class
    {
        private readonly Dictionary<Maybe<TKey>, TValue> inner = new Dictionary<Maybe<TKey>, TValue>(KeyComparer.Default);

#pragma warning disable INPC017 // Backing field name must match.
        internal object Gate => this.inner;
#pragma warning restore INPC017 // Backing field name must match.

        internal IEnumerable<TKey> Keys => this.inner.Keys.Select(x => x.GetValueOrDefault());

        internal TValue this[TKey key]
        {
            get => this.inner[Maybe<TKey>.Some(key)];
            set => this.inner[Maybe<TKey>.Some(key)] = value;
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => this.inner.Select(
                                                                                  x => new KeyValuePair<TKey, TValue>(
                                                                                      x.Key.GetValueOrDefault(),
                                                                                      x.Value))
                                                                              .GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        internal void Add(TKey key, TValue value)
        {
            this.inner.Add(Maybe<TKey>.Some(key), value);
        }

        internal void Remove(TKey key)
        {
            this.inner.Remove(Maybe<TKey>.Some(key));
        }

        internal bool TryGetValue(TKey key, out TValue result)
        {
            return this.inner.TryGetValue(Maybe<TKey>.Some(key), out result);
        }

        internal void Clear()
        {
            this.inner.Clear();
        }

        internal bool ContainsKey(TKey key)
        {
            return this.inner.ContainsKey(Maybe<TKey>.Some(key));
        }

        private sealed class KeyComparer : IEqualityComparer<Maybe<TKey>>
        {
            internal static readonly KeyComparer Default = new KeyComparer();

            private KeyComparer()
            {
            }

            public bool Equals(Maybe<TKey> x, Maybe<TKey> y) => x.HasValue == y.HasValue &&
                                                                                 ReferenceEquals(x.Value, y.Value);

            public int GetHashCode(Maybe<TKey> obj)
            {
                if (obj.HasValue)
                {
                    return obj.GetValueOrDefault() is { } value
                        ? RuntimeHelpers.GetHashCode(value)
                        : 0;
                }

                return -1;
            }
        }
    }
}
