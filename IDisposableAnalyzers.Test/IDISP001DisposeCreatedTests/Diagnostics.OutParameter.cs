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
            public void DiscardedNewDisposableStatementBody(string expression)
            {
                var testCode = @"
namespace RoslynSandbox
{
    public static class C
    {
        public static bool M()
        {
            return TryM(↓out _);
        }

        private static bool TryM(out Disposable disposable)
        {
            disposable = new Disposable();
            return true;
        }
    }
}".AssertReplace("out _", expression);

                AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, DisposableCode, testCode);
            }

            [TestCase("out _")]
            [TestCase("out var temp")]
            [TestCase("out var _")]
            [TestCase("out Disposable temp")]
            [TestCase("out Disposable _")]
            public void DiscardedNewDisposableExpressionBody(string expression)
            {
                var testCode = @"
namespace RoslynSandbox
{
    public static class C
    {
        public static bool M() => TryM(↓out _);

        private static bool TryM(out Disposable disposable)
        {
            disposable = new Disposable();
            return true;
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
            public void DiscardedFileOpenReadStatementBody(string expression)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public static class C
    {
        public static bool M(string fileName)
        {
            return TryM(fileName, ↓out _);
        }

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

            [TestCase("out _")]
            [TestCase("out var temp")]
            [TestCase("out var _")]
            [TestCase("out FileStream temp")]
            [TestCase("out FileStream _")]
            public void DiscardedFileOpenReadExpressionBody(string expression)
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

            [TestCase("out _")]
            [TestCase("out var temp")]
            [TestCase("out var _")]
            [TestCase("out FileStream temp")]
            [TestCase("out FileStream _")]
            public void DiscardedOutAssignedWithArgumentStatementBody(string expression)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public static class C
    {
        public static bool M(string fileName)
        {
            return TryGet(File.OpenRead(fileName), ↓out _);
        }

        private static bool TryGet(FileStream arg, out FileStream result)
        {
            result = arg;
            return true;
        }
    }
}".AssertReplace("out _", expression);

                AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }

            [TestCase("out _")]
            [TestCase("out var temp")]
            [TestCase("out var _")]
            [TestCase("out FileStream temp")]
            [TestCase("out FileStream _")]
            public void DiscardedOutAssignedWithArgumentExpressionBody(string expression)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public static class C
    {
        public static bool M(string fileName) => TryGet(File.OpenRead(fileName), ↓out _);

        private static bool TryGet(FileStream arg, out FileStream result)
        {
            result = arg;
            return true;
        }
    }
}".AssertReplace("out _", expression);

                AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }
        }
    }
}
