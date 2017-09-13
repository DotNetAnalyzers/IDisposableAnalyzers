namespace IDisposableAnalyzers
{
    using System.Collections.Generic;

    internal static class SetPool<T>
    {
        private static readonly Pool<HashSet<T>> Pool = new Pool<HashSet<T>>(() => new HashSet<T>(), x => x.Clear());

        public static Pool<HashSet<T>>.Pooled Create()
        {
            return Pool.GetOrCreate();
        }
    }
}