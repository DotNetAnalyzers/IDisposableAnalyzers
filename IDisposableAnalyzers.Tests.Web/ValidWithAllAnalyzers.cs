// ReSharper disable InconsistentNaming
namespace IDisposableAnalyzers.Tests.Web
{
    using System;
    using System.Collections.Immutable;
    using System.Linq;
    using Gu.Roslyn.AnalyzerExtensions;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public static class ValidWithAllAnalyzers
    {
        private static readonly ImmutableArray<DiagnosticAnalyzer> AllAnalyzers = typeof(KnownSymbols)
            .Assembly
            .GetTypes()
            .Where(t => !t.IsAbstract && typeof(DiagnosticAnalyzer).IsAssignableFrom(t))
            .Select(t => (DiagnosticAnalyzer)Activator.CreateInstance(t)!)
            .ToImmutableArray();

        // ReSharper disable once InconsistentNaming
        private static readonly Solution ValidCodeProjectSln = CodeFactory.CreateSolution(
            ProjectFile.Find("ValidCode.Web.csproj"));

        private static IDisposable? cacheTransaction;

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
            cacheTransaction?.Dispose();
        }

        [Test]
        public static void NotEmpty()
        {
            CollectionAssert.IsNotEmpty(AllAnalyzers);
        }

        [TestCaseSource(nameof(AllAnalyzers))]
        public static void ValidCodeProject(DiagnosticAnalyzer analyzer)
        {
            RoslynAssert.Valid(analyzer, ValidCodeProjectSln);
        }
    }
}
