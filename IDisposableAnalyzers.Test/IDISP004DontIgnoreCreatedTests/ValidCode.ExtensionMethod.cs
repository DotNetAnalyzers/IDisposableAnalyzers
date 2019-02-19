namespace IDisposableAnalyzers.Test.IDISP004DontIgnoreCreatedTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal partial class ValidCode
    {
        [Test]
        public void Simple()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        public void Bar(int i)
        {
            using (new Disposable().AsDisposable())
            {
            }
        }
    }
}";
            var extCode = @"
namespace RoslynSandbox
{
    using System;

    public static class Ext
    {
        public static IDisposable AsDisposable(this IDisposable d) => new WrappingDisposable(d);
    }
}";

            var wrappingDisposableCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class WrappingDisposable : IDisposable
    {
        private readonly IDisposable inner;

        public WrappingDisposable(IDisposable inner)
        {
            this.inner = inner;
        }

        public void Dispose()
        {
#pragma warning disable IDISP007 // Don't dispose injected.
            this.inner.Dispose();
#pragma warning restore IDISP007 // Don't dispose injected.
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode, extCode, DisposableCode, wrappingDisposableCode);
        }

        [Test]
        public void SimpleWithArg()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        public void Bar(int i)
        {
            using (new Disposable().AsDisposable(1))
            {
            }
        }
    }
}";
            var extCode = @"
namespace RoslynSandbox
{
    using System;

    public static class Ext
    {
        public static IDisposable AsDisposable(this IDisposable d, int i) => new WrappingDisposable(d);
    }
}";

            var wrappingDisposableCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class WrappingDisposable : IDisposable
    {
        private readonly IDisposable inner;

        public WrappingDisposable(IDisposable inner)
        {
            this.inner = inner;
        }

        public void Dispose()
        {
#pragma warning disable IDISP007 // Don't dispose injected.
            this.inner.Dispose();
#pragma warning restore IDISP007 // Don't dispose injected.
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode, extCode, DisposableCode, wrappingDisposableCode);
        }

        [Test]
        public void SimpleWhenArg()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        public void Bar(int i)
        {
            using (1.AsDisposable(new Disposable()))
            {
            }
        }
    }
}";
            var extCode = @"
namespace RoslynSandbox
{
    using System;

    public static class Ext
    {
        public static IDisposable AsDisposable(this int i, IDisposable d) => new WrappingDisposable(d);
    }
}";

            var wrappingDisposableCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class WrappingDisposable : IDisposable
    {
        private readonly IDisposable inner;

        public WrappingDisposable(IDisposable inner)
        {
            this.inner = inner;
        }

        public void Dispose()
        {
#pragma warning disable IDISP007 // Don't dispose injected.
            this.inner.Dispose();
#pragma warning restore IDISP007 // Don't dispose injected.
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode, extCode, DisposableCode, wrappingDisposableCode);
        }

        [Test]
        public void Chained()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        public void Bar(int i)
        {
            using (i.AsDisposable().AsDisposable())
            {
            }
        }
    }
}";
            var extCode = @"
namespace RoslynSandbox
{
    using System;

    public static class Ext
    {
        public static IDisposable AsDisposable(this int i) => new Disposable();

        public static IDisposable AsDisposable(this IDisposable d) => new WrappingDisposable(d);
    }
}";

            var wrappingDisposableCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class WrappingDisposable : IDisposable
    {
        private readonly IDisposable inner;

        public WrappingDisposable(IDisposable inner)
        {
            this.inner = inner;
        }

        public void Dispose()
        {
#pragma warning disable IDISP007 // Don't dispose injected.
            this.inner.Dispose();
#pragma warning restore IDISP007 // Don't dispose injected.
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode, extCode, DisposableCode, wrappingDisposableCode);
        }
    }
}
