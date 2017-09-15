namespace IDisposableAnalyzers.Test.IDISP004DontIgnoreReturnValueOfTypeIDisposableTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal partial class HappyPath : HappyPathVerifier<IDISP004DontIgnoreReturnValueOfTypeIDisposable>
    {
        private static readonly string DisposableCode = @"
namespace RoslynSandbox
{
    using System;

    public class Disposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}";

        [Test]
        public void AssigningLocal()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo
    {
        public Foo()
        {
            var disposable = new Disposable();
        }
    }
}";
            AnalyzerAssert.Valid<IDISP004DontIgnoreReturnValueOfTypeIDisposable>(DisposableCode, testCode);
        }

        [Test]
        public void ChainedCtor()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo : IDisposable
    {
        public Foo()
            : this(new Disposable())
        {
        }

        private Foo(IDisposable disposable)
        {
            this.Disposable = disposable;
        }

        public IDisposable Disposable { get; }

        public void Dispose()
        {
            this.Disposable.Dispose();
        }
    }
}";
            AnalyzerAssert.Valid<IDISP004DontIgnoreReturnValueOfTypeIDisposable>(DisposableCode, testCode);
        }

        [Test]
        public void ChainedCtors()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo : IDisposable
    {
        private readonly int meh;

        public Foo()
            : this(new Disposable())
        {
        }

        private Foo(IDisposable disposable)
            : this(disposable, 1)
        {
        }

        private Foo(IDisposable disposable, int meh)
        {
            this.meh = meh;
            this.Disposable = disposable;
        }

        public IDisposable Disposable { get; }

        public void Dispose()
        {
            this.Disposable.Dispose();
        }
    }
}";
            AnalyzerAssert.Valid<IDISP004DontIgnoreReturnValueOfTypeIDisposable>(DisposableCode, testCode);
        }

        [Test]
        public void ChainedCtorCallsBaseCtorDisposedInThis()
        {
            var baseCode = @"
namespace RoslynSandbox
{
    using System;

    public class FooBase : IDisposable
    {
        private readonly IDisposable disposable;
        private bool disposed;

        protected FooBase(IDisposable disposable)
        {
            this.disposable = disposable;
        }

        public IDisposable Disposable => this.disposable;

        public void Dispose()
        {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
        }
    }
}";

            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo : FooBase
    {
        private bool disposed;

        public Foo()
            : this(new Disposable())
        {
        }

        private Foo(IDisposable disposable)
            : base(disposable)
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            if (disposing)
            {
                this.Disposable.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}";
            AnalyzerAssert.Valid<IDISP004DontIgnoreReturnValueOfTypeIDisposable>(DisposableCode, baseCode, testCode);
        }

        [Test]
        public void ChainedBaseCtorDisposedInThis()
        {
            var baseCode = @"
namespace RoslynSandbox
{
    using System;

    public class FooBase : IDisposable
    {
        private readonly object disposable;
        private bool disposed;

        protected FooBase(IDisposable disposable)
        {
            this.disposable = disposable;
        }

        public object Bar
        {
            get
            {
                return this.disposable;
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
        }
    }
}";

            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo : FooBase
    {
        private bool disposed;

        public Foo()
            : base(new Disposable())
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            if (disposing)
            {
                (this.Bar as IDisposable)?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}";
            AnalyzerAssert.Valid<IDISP004DontIgnoreReturnValueOfTypeIDisposable>(DisposableCode, baseCode, testCode);
        }

        [Test]
        public void RealisticExtensionMethodClass()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections.Generic;

    internal static class EnumerableExt
    {
        internal static bool TryGetAtIndex<TCollection, TItem>(this TCollection source, int index, out TItem result)
            where TCollection : IReadOnlyList<TItem>
        {
            result = default(TItem);
            if (source == null)
            {
                return false;
            }

            if (source.Count <= index)
            {
                return false;
            }

            result = source[index];
            return true;
        }

        internal static bool TryGetSingle<TCollection, TItem>(this TCollection source, out TItem result)
            where TCollection : IReadOnlyList<TItem>
        {
            if (source.Count == 1)
            {
                result = source[0];
                return true;
            }

            result = default(TItem);
            return false;
        }

        internal static bool TryGetSingle<TCollection, TItem>(this TCollection source, Func<TItem, bool> selector, out TItem result)
            where TCollection : IReadOnlyList<TItem>
        {
            foreach (var item in source)
            {
                if (selector(item))
                {
                    result = item;
                    return true;
                }
            }

            result = default(TItem);
            return false;
        }

        internal static bool TryGetFirst<TCollection, TItem>(this TCollection source, out TItem result)
            where TCollection : IReadOnlyList<TItem>
        {
            if (source.Count == 0)
            {
                result = default(TItem);
                return false;
            }

            result = source[0];
            return true;
        }

        internal static bool TryGetFirst<TCollection, TItem>(this TCollection source, Func<TItem, bool> selector, out TItem result)
            where TCollection : IReadOnlyList<TItem>
        {
            foreach (var item in source)
            {
                if (selector(item))
                {
                    result = item;
                    return true;
                }
            }

            result = default(TItem);
            return false;
        }

        internal static bool TryGetLast<TCollection, TItem>(this TCollection source, out TItem result)
            where TCollection : IReadOnlyList<TItem>
        {
            if (source.Count == 0)
            {
                result = default(TItem);
                return false;
            }

            result = source[source.Count - 1];
            return true;
        }

        internal static bool TryGetLast<TCollection, TItem>(this TCollection source, Func<TItem, bool> selector, out TItem result)
             where TCollection : IReadOnlyList<TItem>
        {
            for (var i = source.Count - 1; i >= 0; i--)
            {
                var item = source[i];
                if (selector(item))
                {
                    result = item;
                    return true;
                }
            }

            result = default(TItem);
            return false;
        }
    }
}";

            AnalyzerAssert.Valid<IDISP004DontIgnoreReturnValueOfTypeIDisposable>(testCode);
        }

        [Test]
        public void IfTry()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        private void Bar()
        {
            int value;
            if(Try(out value))
            {
            }
        }

        private bool Try(out int value)
        {
            value = 1;
            return true;
        }
    }
}";

            AnalyzerAssert.Valid<IDISP004DontIgnoreReturnValueOfTypeIDisposable>(testCode);
        }

        [Test]
        public void MatehodWithFuncTaskAsParameter()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Threading.Tasks;
    public class Foo
    {
        public void Meh()
        {
            this.Bar(() => Task.Delay(0));
        }
        public void Bar(Func<Task> func)
        {
        }
    }
}";

            AnalyzerAssert.Valid<IDISP004DontIgnoreReturnValueOfTypeIDisposable>(testCode);
        }

        [Test]
        public void MethodWithFuncStreamAsParameter()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        public void Meh()
        {
            this.Bar(() => File.OpenRead(string.Empty));
        }

        public void Bar(Func<Stream> func)
        {
        }
    }
}";

            AnalyzerAssert.Valid<IDISP004DontIgnoreReturnValueOfTypeIDisposable>(testCode);
        }
    }
}