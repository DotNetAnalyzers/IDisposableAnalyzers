namespace IDisposableAnalyzers.Test.IDISP003DisposeBeforeReassigningTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    // ReSharper disable once UnusedTypeParameter
    public static partial class Valid<T>
    {
        [TestCase("this.stream == null")]
        [TestCase("this.stream == null && file != null")]
        [TestCase("file != null && this.stream == null")]
        [TestCase("this.stream is null")]
        [TestCase("ReferenceEquals(this.stream, null)")]
        [TestCase("Equals(this.stream, null)")]
        [TestCase("object.ReferenceEquals(this.stream, null)")]
        [TestCase("object.Equals(this.stream, null)")]
        public static void WhenNullCheckBefore(string nullCheck)
        {
            var code = @"
namespace N
{
    using System.IO;

    public class C
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
}".AssertReplace("this.stream == null", nullCheck);
            RoslynAssert.Valid(Analyzer, DisposableCode, code);
        }
    }
}
