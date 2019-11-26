namespace IDisposableAnalyzers.Test.IDISP025SealDisposableTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public static class CodeFix
    {
        private static readonly DiagnosticAnalyzer Analyzer = new ClassDeclarationAnalyzer();
        private static readonly CodeFixProvider Fix = new SealFix();

        [Test]
        public static void Simple()
        {
            var before = @"
namespace N
{
    using System;

    public class ↓C : IDisposable
    {
        public void Dispose()
        {
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
