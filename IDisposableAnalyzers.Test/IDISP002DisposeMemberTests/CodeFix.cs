namespace IDisposableAnalyzers.Test.IDISP002DisposeMemberTests
{
    public static partial class CodeFix
    {
        private const string Disposable = @"
namespace N
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
