namespace IDisposableAnalyzers.Benchmarks.Benchmarks
{
    using System.IO;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis;

    public static class Code
    {
        public static string ProjectDirectory { get; } = ProjectFile.Find("IDisposableAnalyzers.Benchmarks.csproj").DirectoryName;

        public static string BenchmarksDirectory { get; } = Path.Combine(ProjectDirectory, "Benchmarks");

        public static Solution ValidCodeProject { get; } = CodeFactory.CreateSolution(
            ProjectFile.Find("ValidCode.csproj"));
    }
}
