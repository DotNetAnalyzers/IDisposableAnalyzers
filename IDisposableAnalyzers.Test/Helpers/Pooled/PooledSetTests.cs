// ReSharper disable UnusedVariable
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
        public void UsingBorrowBorrowOrIncrementUsage()
        {
            using (var set = PooledSet<int>.Borrow())
            {
                using (var meh = PooledSet.IncrementUsage(set))
                {
                    using (var meh1 = PooledSet.IncrementUsage(meh))
                    {
                    }
                }
            }
        }

        [Test]
        public void UsingBorrowOrIncrementUsageNull()
        {
            using (var set = PooledSet<int>.BorrowOrIncrementUsage(null))
            {
                using (var meh = PooledSet.IncrementUsage(set))
                {
                    using (var meh1 = PooledSet.IncrementUsage(meh))
                    {
                    }
                }
            }
        }

        [Test]
        public void UseSet()
        {
            using (var set = PooledSet<int>.BorrowOrIncrementUsage(null))
            {
                UseSet(set);
                UseSet(set);
            }
        }

        [Test]
        public void UsingBorrowAddForeach()
        {
            using (var set = PooledSet<int>.Borrow())
            {
                set.Add(1);
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

        private static void UseSet(PooledSet<int> set)
        {
            using (set = PooledSet<int>.BorrowOrIncrementUsage(set))
            {
                set.Add(set.Count);
                foreach (var i in set)
                {
                    var j = Id(i);
                }
            }
        }
    }
}
