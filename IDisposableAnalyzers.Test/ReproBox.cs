namespace IDisposableAnalyzers.Test
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Diagnostics;

    using NUnit.Framework;

    [Explicit("For harvesting test cases only.")]
    public class ReproBox
    {
        // ReSharper disable once UnusedMember.Local
        private static readonly IReadOnlyList<DiagnosticAnalyzer> AllAnalyzers =
            typeof(KnownSymbol).Assembly.GetTypes()
                               .Where(typeof(DiagnosticAnalyzer).IsAssignableFrom)
                               .Select(t => (DiagnosticAnalyzer)Activator.CreateInstance(t))
                               .ToArray();

        private static readonly Solution Solution = CodeFactory.CreateSolution(
            new FileInfo("C:\\Git\\Gu.Reactive\\Gu.Reactive\\Gu.Reactive.csproj"),
            AllAnalyzers,
            AnalyzerAssert.MetadataReferences);

        [TestCaseSource(nameof(AllAnalyzers))]
        public void SolutionRepro(DiagnosticAnalyzer analyzer)
        {
            AnalyzerAssert.Valid(analyzer, Solution);
        }

        [TestCaseSource(nameof(AllAnalyzers))]
        public void Repro(DiagnosticAnalyzer analyzer)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        public Foo()
        {
        }
    }
}";
            AnalyzerAssert.Valid(analyzer, testCode);
        }
    }
}
