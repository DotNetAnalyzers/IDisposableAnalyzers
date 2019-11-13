namespace IDisposableAnalyzers.Test.IDISP015DoNotReturnCachedAndCreatedTest
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public static class Diagnostics
    {
        private static readonly DiagnosticAnalyzer Analyzer = new MethodReturnValuesAnalyzer();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.IDISP015DoNotReturnCachedAndCreated);

        private const string DisposableCode = @"
namespace N
{
    using System;

    public class Disposable : IDisposable
    {
        public Disposable(string _)
            : this()
        {
        }

        public Disposable()
        {
        }

        public void Dispose()
        {
        }
    }
}";

        [Test]
        public static void Ternary()
        {
            var code = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C
    {
        private readonly IDisposable disposable;

        public C(IDisposable disposable)
        {
            this.disposable = disposable;
        }

        public IDisposable ↓M(bool b) => b ? new Disposable() : this.disposable;
    }
}";
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, DisposableCode, code);
        }

        [Test]
        public static void NullCoalesce()
        {
            var code = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C
    {
        private readonly IDisposable disposable;

        public C(IDisposable disposable)
        {
            this.disposable = disposable;
        }

        public IDisposable ↓P() => this.disposable ?? new Disposable();
    }
}";
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, DisposableCode, code);
        }

        [Test]
        public static void ReturnFileOpenReadFromUsing()
        {
            var code = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C
    {
        private readonly IDisposable disposable;

        public C(IDisposable disposable)
        {
            this.disposable = disposable;
        }

        public IDisposable ↓M(bool condition)
        {
            if (condition)
            {
                return this.disposable;
            }

            return File.OpenRead(string.Empty);
        }
    }
}";
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }
    }
}
