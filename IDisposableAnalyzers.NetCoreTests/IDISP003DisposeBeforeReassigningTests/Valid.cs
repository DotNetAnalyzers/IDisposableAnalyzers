namespace IDisposableAnalyzers.NetCoreTests.IDISP003DisposeBeforeReassigningTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public static class Valid
    {
        private static readonly DiagnosticAnalyzer Analyzer = new AssignmentAnalyzer();

        [Test]
        public static void FieldDisposeAsyncInDisposeAsync()
        {
            var code = @"
namespace N
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public class C : IAsyncDisposable
    {
        private Timer _timer;

        public async Task ResetTimerAsync()
        {
            if (_timer != null)
            {
                await _timer.DisposeAsync();
                _timer = null; // Warns with IDISP003: Dispose previous before re-assigning.
            }
        }

        public async ValueTask DisposeAsync()
        {
            await _timer.DisposeAsync();
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }
    }
}
