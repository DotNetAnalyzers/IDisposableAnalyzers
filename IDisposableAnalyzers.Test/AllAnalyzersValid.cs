// ReSharper disable InconsistentNaming
namespace IDisposableAnalyzers.Test
{
    using System;
    using System.Collections.Immutable;
    using System.Linq;
    using Gu.Roslyn.AnalyzerExtensions;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public static class AllAnalyzersValid
    {
        private static readonly ImmutableArray<DiagnosticAnalyzer> AllAnalyzers =
            typeof(KnownSymbols)
            .Assembly
            .GetTypes()
            .Where(t => !t.IsAbstract && typeof(DiagnosticAnalyzer).IsAssignableFrom(t))
            .Select(t => (DiagnosticAnalyzer)Activator.CreateInstance(t))
            .ToImmutableArray();

        private static readonly Solution AnalyzersCode = CodeFactory.CreateSolution(
            ProjectFile.Find("IDisposableAnalyzers.csproj"),
            settings: Settings.Default.WithCompilationOptions(x => x.WithSuppressedDiagnostics("CS1701")));

        // ReSharper disable once InconsistentNaming
        private static readonly Solution ValidCode = CodeFactory.CreateSolution(
            ProjectFile.Find("ValidCode.csproj"),
            settings: Settings.Default.WithCompilationOptions(x => x.WithSuppressedDiagnostics("CS1701")));

        private static IDisposable cacheTransaction;

        [OneTimeSetUp]
        public static void OneTimeSetUp()
        {
            // The cache will be enabled when running in VS.
            // It speeds up the tests and makes them more realistic
            cacheTransaction = SyntaxTreeCache<SemanticModel>.Begin(null);
        }

        [OneTimeTearDown]
        public static void OneTimeTearDown()
        {
            cacheTransaction.Dispose();
        }

        [Test]
        public static void NotEmpty()
        {
            CollectionAssert.IsNotEmpty(AllAnalyzers);
        }

        [TestCaseSource(nameof(AllAnalyzers))]
        public static void AnalyzersProject(DiagnosticAnalyzer analyzer)
        {
            RoslynAssert.Valid(analyzer, AnalyzersCode);
        }

        [TestCaseSource(nameof(AllAnalyzers))]
        public static void ValidCodeProject(DiagnosticAnalyzer analyzer)
        {
            RoslynAssert.Valid(analyzer, ValidCode);
        }

        [TestCaseSource(nameof(AllAnalyzers))]
        public static void WithSyntaxErrors(DiagnosticAnalyzer analyzer)
        {
            var code = @"
namespace N
{
    using System;
    using System.IO;

    public class C : SyntaxError
    {
        private readonly Stream stream = File.SyntaxError(string.Empty);
        private bool disposed;

        protected override void Dispose(bool disposing)
        {
            if (this.syntaxError)
            {
                return;
            }

            this.disposed = true;
            if (disposing)
            {
                this.stream.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}";
            RoslynAssert.NoAnalyzerDiagnostics(analyzer, code);
        }
    }
}
