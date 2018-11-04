namespace IDisposableAnalyzers.Test.IDISP006ImplementIDisposableTests
{
    using NUnit.Framework;

    [TestFixture]
    public partial class CodeFix
    {
        private static readonly string DisposableCode = @"
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
