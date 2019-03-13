namespace IDisposableAnalyzers.Test.IDISP001DisposeCreatedTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    // ReSharper disable once UnusedTypeParameter
    public partial class ValidCode<T>
    {
        [TestCase("stream.Dispose()")]
        [TestCase("stream?.Dispose()")]
        [TestCase("((IDisposable)stream)?.Dispose()")]
        [TestCase("(stream as IDisposable)?.Dispose()")]
        public void DisposedLocal(string expression)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class C
    {
        public void M()
        {
            var stream = File.OpenRead(string.Empty);
            stream.Dispose();
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, Descriptor, testCode);
        }
    }
}
