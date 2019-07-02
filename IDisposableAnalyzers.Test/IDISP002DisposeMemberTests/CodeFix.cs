namespace IDisposableAnalyzers.Test.IDISP002DisposeMemberTests
{
    public static partial class CodeFix
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
