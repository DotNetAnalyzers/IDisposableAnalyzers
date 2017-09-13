namespace IDisposableAnalyzers.Benchmarks.Benchmarks
{
    public class IDISP008DontMixInjectedAndCreatedForMember : Analyzer
    {
        public IDISP008DontMixInjectedAndCreatedForMember()
            : base(new IDisposableAnalyzers.IDISP008DontMixInjectedAndCreatedForMember())
        {
        }
    }
}