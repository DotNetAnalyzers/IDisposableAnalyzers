namespace IDisposableAnalyzers.Benchmarks.Benchmarks
{
    public class IDISP004DontIgnoreReturnValueOfTypeIDisposableBenchmarks : AnalyzerBenchmarks
    {
        public IDISP004DontIgnoreReturnValueOfTypeIDisposableBenchmarks()
            : base(new IDISP004DontIgnoreReturnValueOfTypeIDisposable())
        {
        }
    }
}