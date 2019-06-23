// ReSharper disable RedundantNameQualifier
namespace IDisposableAnalyzers.Benchmarks.Benchmarks
{
    [BenchmarkDotNet.Attributes.MemoryDiagnoser]
    public class CreationAnalyzerBenchmarks
    {
        private static readonly Gu.Roslyn.Asserts.Benchmark Benchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.ValidCodeProject, new IDisposableAnalyzers.CreationAnalyzer());

        [BenchmarkDotNet.Attributes.Benchmark]
        public void RunOnValidCodeProject()
        {
            Benchmark.Run();
        }
    }
}
