// ReSharper disable RedundantNameQualifier
namespace IDisposableAnalyzers.Benchmarks.Benchmarks;

[BenchmarkDotNet.Attributes.MemoryDiagnoser]
public class AssignmentAnalyzerBenchmarks
{
    private static readonly Gu.Roslyn.Asserts.Benchmark Benchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.ValidCodeProject, new IDisposableAnalyzers.AssignmentAnalyzer());

    [BenchmarkDotNet.Attributes.Benchmark]
    public void RunOnValidCodeProject()
    {
        Benchmark.Run();
    }
}
