namespace IDisposableAnalyzers.Test.IDISP024DoNotCallSuppressFinalizeTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public static class CodeFix
    {
        private static readonly DiagnosticAnalyzer Analyzer = new SuppressFinalizeAnalyzer();
        private static readonly CodeFixProvider Fix = new RemoveCallFix();

        [Test]
        public static void SealedSimple()
        {
            var before = @"
namespace N
{
    using System;

    public sealed class C : IDisposable
    {
        public void Dispose()
        {
            ↓GC.SuppressFinalize(this);
        }
    }
}";

            var after = @"
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
            RoslynAssert.CodeFix(Analyzer, Fix, before, after);
        }
    }
}
