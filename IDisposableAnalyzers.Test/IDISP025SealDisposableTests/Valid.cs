namespace IDisposableAnalyzers.Test.IDISP025SealDisposableTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public static class Valid
    {
        private static readonly DiagnosticAnalyzer Analyzer = new ClassDeclarationAnalyzer();

        [Test]
        public static void SealedSimple()
        {
            var code = @"
namespace N
{
    using System;

    public sealed class C : IDisposable
    {
        public void Dispose()
        {
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void SealedPartial()
        {
            var part1 = @"
namespace N
{
    using System;

    public sealed partial class C : IDisposable
    {
    }
}";

            var part2 = @"
namespace N
{
    public sealed partial class C
    {
        public void Dispose()
        {
        }
    }
}";
            RoslynAssert.Valid(Analyzer, part1, part2);
        }

        [Test]
        public static void VirtualSimple()
        {
            var code = @"
namespace N
{
    using System;

    public class C : IDisposable
    {
        public virtual void Dispose()
        {
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void VirtualPartial()
        {
            var part1 = @"
namespace N
{
    using System;

    public partial class C : IDisposable
    {
    }
}";

            var part2 = @"
namespace N
{
    public partial class C
    {
        public virtual void Dispose()
        {
        }
    }
}";
            RoslynAssert.Valid(Analyzer, part1, part2);
        }

        [Test]
        public static void ProtectedVirtualPartial()
        {
            var part1 = @"
namespace N
{
    using System;

    public partial class C : IDisposable
    {
        private bool disposed;

        public virtual void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}";

            var part2 = @"
namespace N
{
    public partial class C
    {
        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            if (disposing)
            {
            }
        }
    }
}";
            RoslynAssert.Valid(Analyzer, part1, part2);
        }
    }
}
