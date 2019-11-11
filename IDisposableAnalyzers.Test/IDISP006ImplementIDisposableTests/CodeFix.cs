namespace IDisposableAnalyzers.Test.IDISP006ImplementIDisposableTests
{
    using NUnit.Framework;

    [TestFixture]
    public static partial class CodeFix
    {
        private const string Disposable = @"
namespace RoslynSandbox
{
    using System;

    public class Disposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}";
    }
}
