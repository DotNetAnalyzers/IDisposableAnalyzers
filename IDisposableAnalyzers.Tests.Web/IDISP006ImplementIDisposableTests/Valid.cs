namespace IDisposableAnalyzers.Tests.Web.IDISP006ImplementIDisposableTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public static class Valid
    {
        private static readonly DiagnosticAnalyzer Analyzer = new FieldAndPropertyDeclarationAnalyzer();

        [Test]
        public static void FieldDisposeAsyncInDisposeAsync()
        {
            var code = @"
namespace N
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    public class C : IAsyncDisposable
    {
        private readonly IAsyncDisposable disposable = File.OpenRead(string.Empty);

        public async ValueTask DisposeAsync()
        {
            await this.disposable.DisposeAsync();
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

        [Test]
        public static void IHostedServiceWhenAssignedInFieldInitializer()
        {
            var code = @"
namespace N
{
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Hosting;

    public class C : IHostedService
    {
        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1);

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            this.semaphore.Dispose();
            return Task.CompletedTask;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void IHostedServiceWhenAssignedInConstructor()
        {
            var code = @"
namespace N
{
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Hosting;

    public class C : IHostedService
    {
        private readonly SemaphoreSlim semaphore;

        public C()
        {
            this.semaphore = new SemaphoreSlim(1);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            this.semaphore.Dispose();
            return Task.CompletedTask;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void IHostedServiceWhenAssignedInPropertyInitializer()
        {
            var code = @"
namespace N
{
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Hosting;

    public class C : IHostedService
    {
        public SemaphoreSlim Semaphore { get; } = new SemaphoreSlim(1);

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            this.Semaphore.Dispose();
            return Task.CompletedTask;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void WhenImplementingIAsyncDisposable()
        {
            var code = @"
namespace N
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    public class C : IAsyncDisposable
    {
        private readonly IAsyncDisposable disposable = File.OpenRead(string.Empty);

        public async ValueTask DisposeAsync()
        {
            await this.disposable.DisposeAsync();
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }
    }
}
