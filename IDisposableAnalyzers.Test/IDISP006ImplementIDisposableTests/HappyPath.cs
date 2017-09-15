namespace IDisposableAnalyzers.Test.IDISP006ImplementIDisposableTests
{
    internal partial class HappyPath
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