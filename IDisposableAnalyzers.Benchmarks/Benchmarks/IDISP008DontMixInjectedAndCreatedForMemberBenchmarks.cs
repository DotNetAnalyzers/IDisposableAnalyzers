namespace IDisposableAnalyzers.Benchmarks.Benchmarks
{
    public class IDISP008DontMixInjectedAndCreatedForMemberBenchmarks : AnalyzerBenchmarks
    {
        public IDISP008DontMixInjectedAndCreatedForMemberBenchmarks()
            : base(new IDISP008DontMixInjectedAndCreatedForMember())
        {
        }
    }
}