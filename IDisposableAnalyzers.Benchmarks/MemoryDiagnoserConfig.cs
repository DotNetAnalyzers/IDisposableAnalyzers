[assembly: BenchmarkDotNet.Attributes.Config(typeof(IDisposableAnalyzers.Benchmarks.MemoryDiagnoserConfig))]
namespace IDisposableAnalyzers.Benchmarks
{
    using BenchmarkDotNet.Configs;
    using BenchmarkDotNet.Diagnosers;

    public class MemoryDiagnoserConfig : ManualConfig
    {
        public MemoryDiagnoserConfig()
        {
            this.Add(new MemoryDiagnoser());
        }
    }
}