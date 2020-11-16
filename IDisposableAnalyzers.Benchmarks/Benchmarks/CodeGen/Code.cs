namespace IDisposableAnalyzers.Benchmarks.Benchmarks
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;
    using System.Net;
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

        private static readonly IReadOnlyList<DiagnosticAnalyzer> AllAnalyzers = typeof(KnownSymbols).Assembly
                                                                                                    .GetTypes()
                                                                                                    .Where(typeof(DiagnosticAnalyzer).IsAssignableFrom)
                                                                                                    .Select(t => (DiagnosticAnalyzer)Activator.CreateInstance(t))
                                                                                                    .ToArray();

        public static string ProjectDirectory { get; } = ProjectFile.Find("IDisposableAnalyzers.Benchmarks.csproj").DirectoryName;

        public static string BenchmarksDirectory { get; } = Path.Combine(ProjectDirectory, "Benchmarks");

        public static Solution ValidCodeProject { get; } = CodeFactory.CreateSolution(
            ProjectFile.Find("ValidCode.csproj"),
            AllAnalyzers,
            MetadataReferences);
    }
}
