namespace IDisposableAnalyzers
{
    internal static class PooledSet
    {
        internal static PooledSet<T> BorrowOrIncrementUsage<T>(PooledSet<T> set)
        {
            return PooledSet<T>.BorrowOrIncrementUsage(set);
        }
    }
}
