namespace ValidCode
{
    using System.Collections;
    using System.Collections.Generic;

    internal class CustomCollection<T> : IList<T>
    {
        private readonly List<T> inner = new List<T>();

        public int Count => this.inner.Count;

        public bool IsReadOnly => ((IList<T>)this.inner).IsReadOnly;

        public T this[int index]
        {
            get => this.inner[index];
            set => this.inner[index] = value;
        }

        public void Add(T item) => this.inner.Add(item);

        public void Clear() => this.inner.Clear();

        public bool Contains(T item) => this.inner.Contains(item);

        public void CopyTo(T[] array, int arrayIndex) => this.inner.CopyTo(array, arrayIndex);

        public bool Remove(T item) => this.inner.Remove(item);

        public int IndexOf(T item)
        {
            return this.inner.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            this.inner.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            this.inner.RemoveAt(index);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return this.inner.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }
}
