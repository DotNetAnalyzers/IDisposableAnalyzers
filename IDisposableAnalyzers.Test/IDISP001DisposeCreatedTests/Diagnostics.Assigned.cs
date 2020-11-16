namespace IDisposableAnalyzers.Test.IDISP001DisposeCreatedTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public static partial class Diagnostics
    {
        public static class Assigned
        {
            private static readonly DiagnosticAnalyzer Analyzer = new AssignmentAnalyzer();
            private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.IDISP001DisposeCreated);

            [Test]
            public static void TryCatchIssue240()
            {
                var code = @"
namespace N
{
    using System;
    using System.IO;

    public static class C
    {
        static void M()
        {
            IDisposable diposable = null;
            try
            {
                ↓diposable = new MemoryStream();
            }
            catch
            {
                diposable.Dispose();
            }
        }
    }
}";
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
            }
        }
    }
}
