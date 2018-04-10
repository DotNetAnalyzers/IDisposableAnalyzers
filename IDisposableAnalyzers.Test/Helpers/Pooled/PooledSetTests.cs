namespace IDisposableAnalyzers.Test.Helpers.Pooled
{
    using NUnit.Framework;

   internal class PooledSetTests
    {
        [Test]
        public void UsingBorrow()
        {
            using (PooledSet<int>.Borrow())
            {
            }
        }

        [Test]
        public void UsingBorrowAddForeach()
        {
            using (var set = PooledSet<int>.Borrow())
            {
                set.Add(1);
                // ReSharper disable once UnusedVariable
                foreach (var i in set)
                {
                }
            }
        }
    }
}
