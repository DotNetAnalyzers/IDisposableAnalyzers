namespace IDisposableAnalyzers.Test.IDISP015DontReturnCachedAndCreatedTest
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    internal class Diagnostics
    {
        private static readonly DiagnosticAnalyzer Analyzer = new MethodReturnValuesAnalyzer();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(IDISP015DontReturnCachedAndCreated.Descriptor);

        private const string DisposableCode = @"
namespace RoslynSandbox
{
    using System;

    public class Disposable : IDisposable
    {
        public Disposable(string meh)
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
        public void Ternary()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo
    {
        private readonly IDisposable disposable;

        public Foo(IDisposable disposable)
        {
            this.disposable = disposable;
        }

        public IDisposable ↓M(bool b) => b ? new Disposable() : this.disposable;
    }
}";
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, DisposableCode, testCode);
        }

        [Test]
        public void NullCoalesce()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo
    {
        private readonly IDisposable disposable;

        public Foo(IDisposable disposable)
        {
            this.disposable = disposable;
        }

        public IDisposable ↓P() => this.disposable ?? new Disposable();
    }
}";
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, DisposableCode, testCode);
        }

        [Test]
        public void ReturnFileOpenReadFromUsing()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo
    {
        private readonly IDisposable disposable;

        public Foo(IDisposable disposable)
        {
            this.disposable = disposable;
        }

        public IDisposable ↓Bar(bool condition)
        {
            if (condition)
            {
                return this.disposable;
            }

            return File.OpenRead(string.Empty);
        }
    }
}";
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }
    }
}
