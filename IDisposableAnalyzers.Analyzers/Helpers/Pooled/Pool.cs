namespace IDisposableAnalyzers
{
    using System;
    using System.Collections.Concurrent;

    internal class Pool<T>
        where T : class
    {
        private readonly ConcurrentQueue<T> cache = new ConcurrentQueue<T>();
        private readonly Func<T> create;
        private readonly Action<T> ondispose;

        public Pool(Func<T> create, Action<T> ondispose)
        {
            this.create = create;
            this.ondispose = ondispose;
        }

        internal Pooled GetOrCreate()
        {
            if (!this.cache.TryDequeue(out T item))
            {
                item = this.create();
            }

            return new Pooled(item, this);
        }

        internal class Pooled : IDisposable
        {
            private readonly Pool<T> pool;

            public Pooled(T item, Pool<T> pool)
            {
                this.pool = pool;
                this.Item = item;
            }

            internal T Item { get; private set; }

            public void Dispose()
            {
                var item = this.Item;
                this.Item = null;
                //// not thread safe here but good enough
                if (item != null)
                {
                    this.pool.ondispose(item);
                    this.pool.cache.Enqueue(item);
                }
            }
        }
    }
}
