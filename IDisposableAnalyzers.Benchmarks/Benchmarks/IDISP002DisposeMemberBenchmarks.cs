namespace IDisposableAnalyzers.Benchmarks.Benchmarks
{
    public class IDISP002DisposeMemberBenchmarks : AnalyzerBenchmarks
    {
        public IDISP002DisposeMemberBenchmarks()
            : base(new IDisposableAnalyzers.IDISP002DisposeMember())
        {
        }
    }
}