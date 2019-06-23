// ReSharper disable RedundantNameQualifier
namespace IDisposableAnalyzers.Benchmarks.Benchmarks
{
    [BenchmarkDotNet.Attributes.MemoryDiagnoser]
    public class SemanticModelCacheAnalyzerBenchmarks
    {
        private static readonly Gu.Roslyn.Asserts.Benchmark Benchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.ValidCodeProject, new IDisposableAnalyzers.SemanticModelCacheAnalyzer());

        [BenchmarkDotNet.Attributes.Benchmark]
        public void RunOnValidCodeProject()
        {
            Benchmark.Run();
        }
    }
}
