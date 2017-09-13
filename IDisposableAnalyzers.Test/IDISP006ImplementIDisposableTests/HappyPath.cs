namespace IDisposableAnalyzers.Test.IDISP006ImplementIDisposableTests
{
    internal partial class HappyPath : HappyPathVerifier<IDISP006ImplementIDisposable>
    {
        private static readonly string DisposableCode = @"
using System;

public class Disposable : IDisposable
{
    public void Dispose()
    {
    }
}";
    }
}