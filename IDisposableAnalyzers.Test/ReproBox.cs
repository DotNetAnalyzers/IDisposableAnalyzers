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
            new FileInfo("C:\\Git\\Gu.Reactive\\Gu.Reactive.sln"),
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
    using System;
    using System.Collections.Generic;

    internal sealed class Foo : IDisposable
    {
        private readonly RecursiveFoos recursiveFoos = new RecursiveFoos();

        private Foo()
        {
        }

        public void Dispose()
        {
        }

        public bool Try(int location)
        {
            return this.TryGetRecursive(location, out var walker);
        }

        private bool TryGetRecursive(int location, out Foo walker)
        {
            if (this.recursiveFoos.TryGetValue(location, out walker))
            {
                return false;
            }

            walker = new Foo();
            this.recursiveFoos.Add(location, walker);
            return true;
        }

        private class RecursiveFoos
        {
            private readonly Dictionary<int, Foo> map = new Dictionary<int, Foo>();

            public void Add(int location, Foo walker)
            {
                this.map.Add(location, walker);
            }

            public bool TryGetValue(int location, out Foo walker)
            {
                return this.map.TryGetValue(location, out walker);
            }
        }
    }";
            AnalyzerAssert.Valid(analyzer, testCode);
        }
    }
}
