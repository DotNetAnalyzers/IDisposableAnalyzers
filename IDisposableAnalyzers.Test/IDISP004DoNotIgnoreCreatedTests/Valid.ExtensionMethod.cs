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

        [Test]
        public static void ExtensionMethodWrappingStreamInStreamReader()
        {
            var code = @"
namespace N
{
    using System.IO;

    public static class C
    {
        public static StreamReader M1() => File.OpenRead(string.Empty).M2();

        private static StreamReader M2(this Stream stream) => new StreamReader(stream);
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void Issue174()
        {
            var code = @"
namespace Gu.Inject.Tests
{
    using System;
    using NUnit.Framework;

    public class Sandbox
    {
        [Test]
        public void M()
        {
            using (var kernel = new Kernel().AutoBind<int>())
            {
            }
        }
    }

    public class Kernel : IDisposable
    {
        public void Dispose()
        {
        }
    }

    public static class KernelExtensions
    {
        public static Kernel AutoBind<T>(this Kernel kernel)
        {
            return kernel;
        }
    }
}";

            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void KernelBinaryExtensionMethod()
        {
            var binaryReference = BinaryReference.Compile(@"
namespace BinaryReferencedAssembly
{
    using Gu.Inject;

    public static class Ext
    {
        public static Kernel BindEntities(this Kernel item) => item;
    }
}");

            var code = @"
namespace N
{
    using Gu.Inject;
    using BinaryReferencedAssembly;

    static class C
    {
        public static Kernel M()
        {
            return new Kernel()
                .BindEntities();
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code, metadataReferences: MetadataReferences.FromAttributes().Add(binaryReference));
        }

        [Test]
        public static void KernelExtensionMethodInOtherProject()
        {
            var ext = @"
namespace A
{
    using Gu.Inject;

    public static class Ext
    {
        public static Kernel BindEntities(this Kernel item) => item;
    }
}";

            var code = @"
namespace B
{
    using Gu.Inject;
    using A;

    static class C
    {
        public static Kernel M()
        {
            return new Kernel()
                .BindEntities();
        }
    }
}";
            RoslynAssert.Valid(Analyzer, ext, code);
        }
    }
}
