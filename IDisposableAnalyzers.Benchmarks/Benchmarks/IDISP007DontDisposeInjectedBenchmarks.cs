namespace IDisposableAnalyzers.Benchmarks.Benchmarks
{
    public class IDISP007DontDisposeInjectedBenchmarks : AnalyzerBenchmarks
    {
        public IDISP007DontDisposeInjectedBenchmarks()
            : base(new IDISP007DontDisposeInjected())
        {
        }
    }
}