namespace IDisposableAnalyzers.Test.IDISP001DisposeCreatedTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    // ReSharper disable once UnusedTypeParameter
    internal partial class HappyPath<T>
    {
        [Test]
        public void StaticConcurrentDictionaryGetOrAdd()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Collections.Concurrent;
    using System.IO;

    public static class Foo
    {
        private static readonly ConcurrentDictionary<int, Stream> Cache = new ConcurrentDictionary<int, Stream>();

        public static long Bar()
        {
            var stream = Cache.GetOrAdd(1, _ => File.OpenRead(string.Empty));
            return stream.Length;
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void ConcurrentDictionaryGetOrAdd()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Collections.Concurrent;
    using System.IO;

    public class Foo
    {
        private readonly ConcurrentDictionary<int, Stream> Cache = new ConcurrentDictionary<int, Stream>();

        public long Bar()
        {
            var stream = Cache.GetOrAdd(1, _ => File.OpenRead(string.Empty));
            return stream.Length;
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void ConcurrentDictionaryTryGetValue()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Collections.Concurrent;
    using System.IO;

    public static class Foo
    {
        private static readonly ConcurrentDictionary<int, Stream> Cache = new ConcurrentDictionary<int, Stream>();

        public static long Bar()
        {
            Stream stream;
            if (Cache.TryGetValue(1, out stream))
            {
                return stream.Length;
            }

            return 0;
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void ConcurrentDictionaryTryGetValueVarOut()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Collections.Concurrent;
    using System.IO;

    public static class Foo
    {
        private static readonly ConcurrentDictionary<int, Stream> Cache = new ConcurrentDictionary<int, Stream>();

        public static long Bar()
        {
            if (Cache.TryGetValue(1, out var stream))
            {
                return stream.Length;
            }

            return 0;
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void ConditionalWeakTableTryGetValue()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;
    using System.Runtime.CompilerServices;

    public static class Foo
    {
        private static readonly ConditionalWeakTable<string, Stream> Cache = new ConditionalWeakTable<string, Stream>();

        public static long Bar()
        {
            Stream stream;
            if (Cache.TryGetValue(""1"", out stream))
            {
                return stream.Length;
            }

            return 0;
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void ConditionalWeakTableTryGetValueVarOut()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;
    using System.Runtime.CompilerServices;

    public static class Foo
    {
        private static readonly ConditionalWeakTable<string, Stream> Cache = new ConditionalWeakTable<string, Stream>();

        public static long Bar()
        {
            if (Cache.TryGetValue(""1"", out var stream))
            {
                return stream.Length;
            }

            return 0;
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void CustomCacheWrappingDictionary()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections.Generic;

    internal sealed class Foo : IDisposable
    {
        private readonly Cache cache = new Cache();

        private Foo()
        {
        }

        public void Dispose()
        {
            this.cache.Clear();
        }

        public string Bar(int location)
        {
            if (this.cache.TryGetValue(location, out var foo))
            {
                return foo.ToString();
            }

            return null;
        }

        private class Cache
        {
            private readonly Dictionary<int, Foo> map = new Dictionary<int, Foo>();

            public bool TryGetValue(int location, out Foo walker)
            {
                return this.map.TryGetValue(location, out walker);
            }

            public void Clear()
            {
                foreach (var value in this.map.Values)
                {
                    value.Dispose();
                }

                this.map.Clear();
            }
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void PooledConcurrentQueueTryDequeue()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    using System.Collections.Concurrent;
    using System.Diagnostics;

    internal class Foo : IDisposable
    {
        private static readonly ConcurrentQueue<Foo> Cache = new ConcurrentQueue<Foo>();
        private int refCount;

        public void Dispose()
        {
            this.Dispose(true);
        }

        protected static Foo Borrow(Func<Foo> create)
        {
            if (!Cache.TryDequeue(out var walker))
            {
                walker = create();
            }

            walker.refCount = 1;
            return walker;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.refCount--;
                Debug.Assert(this.refCount >= 0, ""refCount>= 0"");
                if (this.refCount == 0)
                {
                    Cache.Enqueue(this);
                }
            }
        }

        [Conditional(""DEBUG"")]
        protected void ThrowIfDisposed()
        {
            if (this.refCount <= 0)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void PooledConcurrentQueueTryDequeue2()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    using System.Collections.Concurrent;
    using System.Diagnostics;

    internal class Foo : IDisposable
    {
        private static readonly ConcurrentQueue<Foo> Cache = new ConcurrentQueue<Foo>();
        private int refCount;

        public void Dispose()
        {
            this.Dispose(true);
        }

        protected static Foo BorrowAndVisit(Func<Foo> create)
        {
            var walker = Borrow(create);
            return walker;
        }

        protected static Foo Borrow(Func<Foo> create)
        {
            if (!Cache.TryDequeue(out var walker))
            {
                walker = create();
            }

            walker.refCount = 1;
            return walker;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.refCount--;
                Debug.Assert(this.refCount >= 0, ""refCount>= 0"");
                if (this.refCount == 0)
                {
                    Cache.Enqueue(this);
                }
            }
        }

        [Conditional(""DEBUG"")]
        protected void ThrowIfDisposed()
        {
            if (this.refCount <= 0)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void TryGetRecursive()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections.Generic;

    internal sealed class Foo : IDisposable
    {
        private readonly RecursiveFoos recursiveFoos = new RecursiveFoos();

        private Foo()
        {
        }

        public void Dispose()
        {
        }

        public bool Try(int location)
        {
            return this.TryGetRecursive(location, out var walker);
        }

        private bool TryGetRecursive(int location, out Foo walker)
        {
            if (this.recursiveFoos.TryGetValue(location, out walker))
            {
                return false;
            }

            walker = new Foo();
            this.recursiveFoos.Add(location, walker);
            return true;
        }

        private class RecursiveFoos
        {
            private readonly Dictionary<int, Foo> map = new Dictionary<int, Foo>();

            public void Add(int location, Foo walker)
            {
                this.map.Add(location, walker);
            }

            public bool TryGetValue(int location, out Foo walker)
            {
                return this.map.TryGetValue(location, out walker);
            }
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }
    }
}
