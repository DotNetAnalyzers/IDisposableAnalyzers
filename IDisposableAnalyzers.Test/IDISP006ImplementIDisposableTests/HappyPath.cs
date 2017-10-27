#pragma warning disable SA1203 // Constants must appear before fields
namespace IDisposableAnalyzers.Test.IDISP006ImplementIDisposableTests
{
    internal partial class HappyPath
    {
        private static readonly IDISP006ImplementIDisposable Analyzer = new IDISP006ImplementIDisposable();

        private const string DisposableCode = @"
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