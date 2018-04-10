namespace IDisposableAnalyzers
{
    internal static class PooledSet
    {
        /// <summary>
        /// The result from this call is meant to be used in a using.
        /// </summary>
        internal static PooledSet<T> IncrementUsage<T>(this PooledSet<T> set)
        {
            return PooledSet<T>.BorrowOrIncrementUsage(set);
        }
    }
}
