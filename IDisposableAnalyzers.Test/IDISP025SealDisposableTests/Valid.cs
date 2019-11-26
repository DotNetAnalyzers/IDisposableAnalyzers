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
        public static void VrirtualSimple()
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
    }
}
