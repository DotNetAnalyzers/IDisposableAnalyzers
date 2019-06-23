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

    public class ValidCodeWithAllAnalyzers
    {
        private static readonly ImmutableArray<DiagnosticAnalyzer> AllAnalyzers = typeof(KnownSymbol)
            .Assembly
            .GetTypes()
            .Where(typeof(DiagnosticAnalyzer).IsAssignableFrom)
            .Select(t => (DiagnosticAnalyzer)Activator.CreateInstance(t))
            .ToImmutableArray();

        private static readonly Solution AnalyzersProjectSln = CodeFactory.CreateSolution(
            ProjectFile.Find("IDisposableAnalyzers.csproj"),
            AllAnalyzers,
            RoslynAssert.MetadataReferences);

        // ReSharper disable once InconsistentNaming
        private static readonly Solution ValidCodeProjectSln = CodeFactory.CreateSolution(
            ProjectFile.Find("ValidCode.csproj"),
            AllAnalyzers,
            RoslynAssert.MetadataReferences);

        [SetUp]
        public void Setup()
        {
            // The cache will be enabled when running in VS.
            // It speeds up the tests and makes them more realistic
            Cache<SyntaxTree, SemanticModel>.Begin();
        }

        [TearDown]
        public void TearDown()
        {
            Cache<SyntaxTree, SemanticModel>.End();
        }

        [Test]
        public void NotEmpty()
        {
            CollectionAssert.IsNotEmpty(AllAnalyzers);
        }

        [TestCaseSource(nameof(AllAnalyzers))]
        public void AnalyzersProject(DiagnosticAnalyzer analyzer)
        {
            RoslynAssert.Valid(analyzer, AnalyzersProjectSln);
        }

        [TestCaseSource(nameof(AllAnalyzers))]
        public void ValidCodeProject(DiagnosticAnalyzer analyzer)
        {
            RoslynAssert.Valid(analyzer, ValidCodeProjectSln);
        }

        [TestCaseSource(nameof(AllAnalyzers))]
        public void WithSyntaxErrors(DiagnosticAnalyzer analyzer)
        {
            var testCode = @"
namespace RoslynSandbox
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
            var solution = CodeFactory.CreateSolution(testCode, CodeFactory.DefaultCompilationOptions(analyzer, RoslynAssert.SuppressedDiagnostics), RoslynAssert.MetadataReferences);
            RoslynAssert.NoDiagnostics(Analyze.GetDiagnostics(analyzer, solution));
        }
    }
}
