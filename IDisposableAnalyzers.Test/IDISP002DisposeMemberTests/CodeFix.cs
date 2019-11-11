namespace IDisposableAnalyzers.Test.IDISP002DisposeMemberTests
{
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
