namespace IDisposableAnalyzers.Test.IDISP007DoNotDisposeInjectedTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public static partial class Diagnostics
    {
        public static class Using
        {
            private static readonly DiagnosticAnalyzer Analyzer = new UsingStatementAnalyzer();
            private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create("IDISP007");

            [Test]
            public static void UsingField1()
            {
                var code = @"
namespace N
{
    using System;

    public class C
    {
        private readonly IDisposable disposable;

        public C(IDisposable disposable)
        {
            this.disposable = disposable;
            using (↓disposable)
            {
            }
        }
    }
}";
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
            }

            [Test]
            public static void UsingField2()
            {
                var code = @"
namespace N
{
    using System;

    public class C
    {
        private readonly IDisposable disposable;

        public C(IDisposable disposable)
        {
            this.disposable = disposable;
            using (var meh = ↓disposable)
            {
            }
        }
    }
}";
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
            }
        }
    }
}
