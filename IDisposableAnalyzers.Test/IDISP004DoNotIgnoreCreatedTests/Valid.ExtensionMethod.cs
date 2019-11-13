namespace IDisposableAnalyzers.Test.IDISP004DoNotIgnoreCreatedTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    public static partial class Valid
    {
        [Test]
        public static void Simple()
        {
            var code = @"
namespace N
{
    using System;

    public class C
    {
        public void M(int i)
        {
            using (new Disposable().AsDisposable())
            {
            }
        }
    }
}";
            var extCode = @"
namespace N
{
    using System;

    public static class Ext
    {
        public static IDisposable AsDisposable(this IDisposable d) => new WrappingDisposable(d);
    }
}";

            var wrappingDisposableCode = @"
namespace N
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
            RoslynAssert.Valid(Analyzer, code, extCode, DisposableCode, wrappingDisposableCode);
        }

        [Test]
        public static void SimpleWithArg()
        {
            var code = @"
namespace N
{
    using System;

    public class C
    {
        public void M(int i)
        {
            using (new Disposable().AsDisposable(1))
            {
            }
        }
    }
}";
            var extCode = @"
namespace N
{
    using System;

    public static class Ext
    {
        public static IDisposable AsDisposable(this IDisposable d, int i) => new WrappingDisposable(d);
    }
}";

            var wrappingDisposableCode = @"
namespace N
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
            RoslynAssert.Valid(Analyzer, code, extCode, DisposableCode, wrappingDisposableCode);
        }

        [Test]
        public static void SimpleWhenArg()
        {
            var code = @"
namespace N
{
    using System;

    public class C
    {
        public void M(int i)
        {
            using (1.AsDisposable(new Disposable()))
            {
            }
        }
    }
}";
            var extCode = @"
namespace N
{
    using System;

    public static class Ext
    {
        public static IDisposable AsDisposable(this int i, IDisposable d) => new WrappingDisposable(d);
    }
}";

            var wrappingDisposableCode = @"
namespace N
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
            RoslynAssert.Valid(Analyzer, code, extCode, DisposableCode, wrappingDisposableCode);
        }

        [Test]
        public static void Chained()
        {
            var code = @"
namespace N
{
    using System;

    public class C
    {
        public void M(int i)
        {
            using (i.AsDisposable().AsDisposable())
            {
            }
        }
    }
}";
            var extCode = @"
namespace N
{
    using System;

    public static class Ext
    {
        public static IDisposable AsDisposable(this int i) => new Disposable();

        public static IDisposable AsDisposable(this IDisposable d) => new WrappingDisposable(d);
    }
}";

            var wrappingDisposableCode = @"
namespace N
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
            RoslynAssert.Valid(Analyzer, code, extCode, DisposableCode, wrappingDisposableCode);
        }
    }
}
