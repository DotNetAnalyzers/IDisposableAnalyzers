namespace IDisposableAnalyzers.Test.IDISP003DisposeBeforeReassigningTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal partial class HappyPath<T>
    {
        [TestCase("this.stream == null")]
        ////[TestCase("this.stream == null && file != null")]
        ////[TestCase("this.disposable is null")]
        [TestCase("ReferenceEquals(this.stream, null)")]
        [TestCase("Equals(this.stream, null)")]
        [TestCase("object.ReferenceEquals(this.stream, null)")]
        [TestCase("object.Equals(this.stream, null)")]
        public void WhenNullCheckBefore(string nullCheck)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        private Stream stream;

        public void Update(string file)
        {
            if (this.stream == null)
            {
                this.stream = File.OpenRead(file);
            }
        }
    }
}";
            testCode = testCode.AssertReplace("this.stream == null", nullCheck);
            AnalyzerAssert.Valid(Analyzer, DisposableCode, testCode);
        }

    }
}
