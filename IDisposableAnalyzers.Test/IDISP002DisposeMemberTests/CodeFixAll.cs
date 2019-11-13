namespace IDisposableAnalyzers.Test.IDISP002DisposeMemberTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public static class CodeFixAll
    {
        private static readonly DiagnosticAnalyzer Analyzer = new FieldAndPropertyDeclarationAnalyzer();
        private static readonly CodeFixProvider Fix = new DisposeMemberFix();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create("IDISP002");

        [Test]
        public static void NotDisposingFieldAssignedInCtor()
        {
            var before = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        ↓private readonly Stream stream;

        public C()
        {
            this.stream = File.OpenRead(string.Empty);
        }

        public void Dispose()
        {
        }
    }
}";

            var after = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        private readonly Stream stream;

        public C()
        {
            this.stream = File.OpenRead(string.Empty);
        }

        public void Dispose()
        {
            this.stream?.Dispose();
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
            RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        [Ignore("Order is random due to async.")]
        public static void NotDisposingFieldsAssignedInCtor()
        {
            var before = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        ↓private readonly Stream stream1;
        ↓private readonly Stream stream2;

        public C()
        {
            this.stream1 = File.OpenRead(string.Empty);
            this.stream2 = File.OpenRead(string.Empty);
        }

        public void Dispose()
        {
        }
    }
}";

            var after = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        private readonly Stream stream1;
        private readonly Stream stream2;

        public C()
        {
            this.stream1 = File.OpenRead(string.Empty);
            this.stream2 = File.OpenRead(string.Empty);
        }

        public void Dispose()
        {
            this.stream1?.Dispose();
            this.stream2?.Dispose();
        }
    }
}";
            RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }
    }
}
