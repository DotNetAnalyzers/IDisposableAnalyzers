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
                //// ReSharper disable once UnusedVariable
                foreach (var i in set)
                {
                }
            }
        }

        [Test]
        public void UsingBorrowAddForeachCallId()
        {
            using (var set = PooledSet<int>.Borrow())
            {
                set.Add(1);
                foreach (var i in set)
                {
                    var j = Id(i);
                }
            }
        }

        private static int Id(int n) => n;
    }
}
