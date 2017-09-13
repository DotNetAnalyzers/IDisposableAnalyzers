namespace IDisposableAnalyzers.Benchmarks.Benchmarks
{
    public class IDISP007DontDisposeInjected : Analyzer
    {
        public IDISP007DontDisposeInjected()
            : base(new IDisposableAnalyzers.IDISP007DontDisposeInjected())
        {
        }
    }
}