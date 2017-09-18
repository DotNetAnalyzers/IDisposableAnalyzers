namespace IDisposableAnalyzers.Benchmarks.Benchmarks
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Reflection;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.Diagnostics;

    public static class Code
    {
        private static readonly IReadOnlyList<MetadataReference> MetadataReferences = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location).WithAliases(ImmutableArray.Create("global", "corlib")),
            MetadataReference.CreateFromFile(typeof(System.Diagnostics.Debug).Assembly.Location).WithAliases(ImmutableArray.Create("global", "system")),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(WebClient).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Xml.Serialization.XmlSerializer).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(CSharpCompilation).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Compilation).Assembly.Location),
        };

        public static Solution Solution { get; } = CodeFactory.CreateSolution(SlnFile, new DiagnosticAnalyzer[0], MetadataReferences);

        public static Project AnalyzersProject { get; } = Solution.Projects.First(x => x.Name == "IDisposableAnalyzers.Analyzers");

        private static FileInfo SlnFile
        {
            get
            {
                var directory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "IDisposableAnalyzers"));
                if (directory.Exists)
                {
                    // Run at 1f8febc0999f6720c8c068a649a6e2831616b15d
                    return new FileInfo(Path.Combine(directory.FullName, "IDisposableAnalyzers.sln"));
                }

                if (CodeFactory.TryFindFileInParentDirectory(
                    new FileInfo(new Uri(Assembly.GetExecutingAssembly().CodeBase, UriKind.Absolute).LocalPath).Directory,
                    "IDisposableAnalyzers.sln",
                    out FileInfo solutionFile))
                {
                    return solutionFile;
                }

                throw new InvalidOperationException("Did not find sln file.");
            }
        }
    }
}