namespace IDisposableAnalyzers.Benchmarks.Benchmarks
{
    public class IDISP003DisposeBeforeReassigningBenchmarks : AnalyzerBenchmarks
    {
        public IDISP003DisposeBeforeReassigningBenchmarks()
            : base(new IDISP003DisposeBeforeReassigning())
        {
        }
    }
}