namespace IDisposableAnalyzers.Benchmarks.Benchmarks
{
    public class IDISP001DisposeCreatedBenchmarks : AnalyzerBenchmarks
    {
        public IDISP001DisposeCreatedBenchmarks()
            : base(new IDISP001DisposeCreated())
        {
        }
    }
}