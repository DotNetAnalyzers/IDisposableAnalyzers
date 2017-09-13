namespace IDisposableAnalyzers.Benchmarks.Benchmarks
{
    public class IDISP005ReturnTypeShouldIndicateIDisposable : Analyzer
    {
        public IDISP005ReturnTypeShouldIndicateIDisposable()
            : base(new IDisposableAnalyzers.IDISP005ReturntypeShouldIndicateIDisposable())
        {
        }
    }
}