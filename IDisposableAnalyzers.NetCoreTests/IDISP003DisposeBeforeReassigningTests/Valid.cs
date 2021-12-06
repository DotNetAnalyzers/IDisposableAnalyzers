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
        private Timer? _timer;

        public async Task ResetTimerAsync()
        {
            if (_timer != null)
            {
                await _timer.DisposeAsync();
                _timer = null; // Warns with IDISP003: Dispose previous before re-assigning
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_timer is { } timer)
            {
                await timer.DisposeAsync();
            }
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void NullableAnnotated()
        {
            var code = @"
namespace N
{
    using System;
    using System.IO;

    class C
    {
        private IDisposable? _disposable;

        void M()
        {
            _disposable!.Dispose();
            _disposable = File.OpenRead(string.Empty);
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void IHostedService()
        {
            var code = @"
namespace N
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Hosting;

    class C : IHostedService
    {
        private IDisposable? disposable;

        public Task StartAsync(CancellationToken token)
        {
            this.disposable = File.OpenRead(string.Empty);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken token)
        {
            this.disposable?.Dispose();
            return Task.CompletedTask;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }
    }
}
