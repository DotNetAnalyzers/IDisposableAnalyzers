namespace IDisposableAnalyzers.Test.IDISP001DisposeCreatedTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public partial class Diagnostics
    {
        public class OutParameter
        {
            private static readonly DiagnosticAnalyzer Analyzer = new ArgumentAnalyzer();
            private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(IDISP001DisposeCreated.Descriptor);

            private const string DisposableCode = @"
namespace RoslynSandbox
{
    using System;

    public class Disposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}";

            [TestCase("out _")]
            [TestCase("out var temp")]
            [TestCase("out var _")]
            [TestCase("out Disposable temp")]
            [TestCase("out Disposable _")]
            public void DiscardedNewDisposable(string expression)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public static class C
    {
        public static bool M() => TryM(↓out _);

        private static bool TryM(out Disposable disposable)
        {
            disposable = new Disposable();
        }
    }
}".AssertReplace("out _", expression);

                AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, DisposableCode, testCode);
            }

            [TestCase("out _")]
            [TestCase("out var temp")]
            [TestCase("out var _")]
            [TestCase("out FileStream temp")]
            [TestCase("out FileStream _")]
            public void DiscardedFileOpenRead(string expression)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public static class C
    {
        public static bool M(string fileName) => TryM(fileName, ↓out _);

        private static bool TryM(string fileName, out FileStream stream)
        {
            if (File.Exists(fileName)
            {
                stream = File.OpenRead(string.Empty);
                return true;
            }

            stream = null;
            return false;
        }
    }
}".AssertReplace("out _", expression);

                AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }
        }
    }
}
