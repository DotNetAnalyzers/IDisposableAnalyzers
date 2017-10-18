namespace IDisposableAnalyzers.Benchmarks.Benchmarks
{
    public class AllBenchmarks
    {
        private static readonly Gu.Roslyn.Asserts.Benchmark IDISP001 = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new IDisposableAnalyzers.IDISP001DisposeCreated());

        private static readonly Gu.Roslyn.Asserts.Benchmark IDISP002 = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new IDisposableAnalyzers.IDISP002DisposeMember());

        private static readonly Gu.Roslyn.Asserts.Benchmark IDISP003 = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new IDisposableAnalyzers.IDISP003DisposeBeforeReassigning());

        private static readonly Gu.Roslyn.Asserts.Benchmark IDISP004 = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new IDisposableAnalyzers.IDISP004DontIgnoreReturnValueOfTypeIDisposable());

        private static readonly Gu.Roslyn.Asserts.Benchmark IDISP005 = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new IDisposableAnalyzers.IDISP005ReturntypeShouldIndicateIDisposable());

        private static readonly Gu.Roslyn.Asserts.Benchmark IDISP006 = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new IDisposableAnalyzers.IDISP006ImplementIDisposable());

        private static readonly Gu.Roslyn.Asserts.Benchmark IDISP007 = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new IDisposableAnalyzers.IDISP007DontDisposeInjected());

        private static readonly Gu.Roslyn.Asserts.Benchmark IDISP008 = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new IDisposableAnalyzers.IDISP008DontMixInjectedAndCreatedForMember());

        private static readonly Gu.Roslyn.Asserts.Benchmark IDISP009 = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new IDisposableAnalyzers.IDISP009IsIDisposable());

        [BenchmarkDotNet.Attributes.Benchmark]
        public void IDISP001DisposeCreated()
        {
            IDISP001.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void IDISP002DisposeMember()
        {
            IDISP002.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void IDISP003DisposeBeforeReassigning()
        {
            IDISP003.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void IDISP004DontIgnoreReturnValueOfTypeIDisposable()
        {
            IDISP004.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void IDISP005ReturntypeShouldIndicateIDisposable()
        {
            IDISP005.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void IDISP006ImplementIDisposable()
        {
            IDISP006.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void IDISP007DontDisposeInjected()
        {
            IDISP007.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void IDISP008DontMixInjectedAndCreatedForMember()
        {
            IDISP008.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void IDISP009IsIDisposable()
        {
            IDISP009.Run();
        }
    }
}
