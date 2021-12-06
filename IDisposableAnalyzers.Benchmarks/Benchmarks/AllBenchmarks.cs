// ReSharper disable RedundantNameQualifier
namespace IDisposableAnalyzers.Benchmarks.Benchmarks
{
    [BenchmarkDotNet.Attributes.MemoryDiagnoser]
    public class AllBenchmarks
    {
        private static readonly Gu.Roslyn.Asserts.Benchmark ArgumentAnalyzerBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.ValidCodeProject, new IDisposableAnalyzers.ArgumentAnalyzer());

        private static readonly Gu.Roslyn.Asserts.Benchmark AssignmentAnalyzerBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.ValidCodeProject, new IDisposableAnalyzers.AssignmentAnalyzer());

        private static readonly Gu.Roslyn.Asserts.Benchmark ClassDeclarationAnalyzerBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.ValidCodeProject, new IDisposableAnalyzers.ClassDeclarationAnalyzer());

        private static readonly Gu.Roslyn.Asserts.Benchmark CreationAnalyzerBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.ValidCodeProject, new IDisposableAnalyzers.CreationAnalyzer());

        private static readonly Gu.Roslyn.Asserts.Benchmark DisposeCallAnalyzerBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.ValidCodeProject, new IDisposableAnalyzers.DisposeCallAnalyzer());

        private static readonly Gu.Roslyn.Asserts.Benchmark DisposeMethodAnalyzerBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.ValidCodeProject, new IDisposableAnalyzers.DisposeMethodAnalyzer());

        private static readonly Gu.Roslyn.Asserts.Benchmark FieldAndPropertyDeclarationAnalyzerBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.ValidCodeProject, new IDisposableAnalyzers.FieldAndPropertyDeclarationAnalyzer());

        private static readonly Gu.Roslyn.Asserts.Benchmark FinalizerAnalyzerBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.ValidCodeProject, new IDisposableAnalyzers.FinalizerAnalyzer());

        private static readonly Gu.Roslyn.Asserts.Benchmark LocalDeclarationAnalyzerBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.ValidCodeProject, new IDisposableAnalyzers.LocalDeclarationAnalyzer());

        private static readonly Gu.Roslyn.Asserts.Benchmark MethodReturnValuesAnalyzerBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.ValidCodeProject, new IDisposableAnalyzers.MethodReturnValuesAnalyzer());

        private static readonly Gu.Roslyn.Asserts.Benchmark ReturnValueAnalyzerBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.ValidCodeProject, new IDisposableAnalyzers.ReturnValueAnalyzer());

        private static readonly Gu.Roslyn.Asserts.Benchmark SemanticModelCacheAnalyzerBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.ValidCodeProject, new IDisposableAnalyzers.SemanticModelCacheAnalyzer());

        private static readonly Gu.Roslyn.Asserts.Benchmark SuppressFinalizeAnalyzerBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.ValidCodeProject, new IDisposableAnalyzers.SuppressFinalizeAnalyzer());

        private static readonly Gu.Roslyn.Asserts.Benchmark UsingStatementAnalyzerBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.ValidCodeProject, new IDisposableAnalyzers.UsingStatementAnalyzer());

        [BenchmarkDotNet.Attributes.Benchmark]
        public void ArgumentAnalyzer()
        {
            ArgumentAnalyzerBenchmark.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void AssignmentAnalyzer()
        {
            AssignmentAnalyzerBenchmark.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void ClassDeclarationAnalyzer()
        {
            ClassDeclarationAnalyzerBenchmark.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void CreationAnalyzer()
        {
            CreationAnalyzerBenchmark.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void DisposeCallAnalyzer()
        {
            DisposeCallAnalyzerBenchmark.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void DisposeMethodAnalyzer()
        {
            DisposeMethodAnalyzerBenchmark.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void FieldAndPropertyDeclarationAnalyzer()
        {
            FieldAndPropertyDeclarationAnalyzerBenchmark.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void FinalizerAnalyzer()
        {
            FinalizerAnalyzerBenchmark.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void LocalDeclarationAnalyzer()
        {
            LocalDeclarationAnalyzerBenchmark.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void MethodReturnValuesAnalyzer()
        {
            MethodReturnValuesAnalyzerBenchmark.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void ReturnValueAnalyzer()
        {
            ReturnValueAnalyzerBenchmark.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void SemanticModelCacheAnalyzer()
        {
            SemanticModelCacheAnalyzerBenchmark.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void SuppressFinalizeAnalyzer()
        {
            SuppressFinalizeAnalyzerBenchmark.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void UsingStatementAnalyzer()
        {
            UsingStatementAnalyzerBenchmark.Run();
        }
    }
}
