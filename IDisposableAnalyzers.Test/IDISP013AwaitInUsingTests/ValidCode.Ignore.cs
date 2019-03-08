namespace IDisposableAnalyzers.Test.IDISP013AwaitInUsingTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    public partial class ValidCode
    {
        public class Ignore
        {
            [Test]
            public void NUnitAssertThrowsAsync()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using NUnit.Framework;

    public class C
    {
        public void M()
        {
            using (var stream = File.OpenRead(string.Empty))
            {
                Assert.ThrowsAsync<Exception>(() => Task.Run(() => throw new Exception()));
            }
        }
    }
}";
                AnalyzerAssert.Valid(Analyzer, testCode);
            }
        }
    }
}
