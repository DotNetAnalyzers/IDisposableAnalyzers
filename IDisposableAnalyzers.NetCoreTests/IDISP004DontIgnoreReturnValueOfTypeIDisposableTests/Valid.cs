// ReSharper disable InconsistentNaming
namespace IDisposableAnalyzers.NetCoreTests.IDISP004DontIgnoreReturnValueOfTypeIDisposableTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public static class Valid
    {
        private static readonly DiagnosticAnalyzer Analyzer = new CreationAnalyzer();

        private const string Disposable = @"
namespace N
{
    using System;
    using System.Threading.Tasks;

    public sealed class Disposable : IDisposable, IAsyncDisposable
    {
        public void Dispose()
        {
        }

        public ValueTask DisposeAsync()
        {
            throw new NotImplementedException();
        }
    }
}";

        [Test]
        public static void AwaitUsing()
        {
            var asyncDisposable = @"
namespace N
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    public class AsyncDisposable : IAsyncDisposable
    {
        private readonly IAsyncDisposable disposable = File.OpenRead(string.Empty);

        public async ValueTask DisposeAsync()
        {
            await this.disposable.DisposeAsync().ConfigureAwait(false);
        }
    }
}";
            var code = @"
namespace N
{
    using System.Threading.Tasks;

    class C
    {
        public async Task M()
        {
            await using var asyncDisposable = new AsyncDisposable();
        }
    }
}
";
            RoslynAssert.Valid(Analyzer, asyncDisposable, code);
        }

        [Test]
        public static void ILoggerFactoryAddApplicationInsights()
        {
            var code = @"
namespace N
{
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Logging;

    public class Foo
    {
        public void Configure(
            IApplicationBuilder app,
            IHostingEnvironment env,
            ILoggerFactory loggerFactory)
        {
            loggerFactory.AddApplicationInsights(app.ApplicationServices, LogLevel.Warning);
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [TestCase("var disposable = serviceProvider.GetRequiredService<Disposable>();")]
        [TestCase("_ = serviceProvider.GetRequiredService<Disposable>();")]
        [TestCase("var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();")]
        [TestCase("_ = serviceProvider.GetRequiredService<ILoggerFactory>();")]
        public static void IServiceProviderGetRequiredService(string statement)
        {
            var code = @"
namespace N
{
    using System;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    public class C
    {
        public C(IServiceProvider serviceProvider)
        {
            var disposable = serviceProvider.GetRequiredService<Disposable>();
        }

        public sealed class Disposable : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}".AssertReplace("var disposable = serviceProvider.GetRequiredService<Disposable>();", statement);
            RoslynAssert.Valid(Analyzer, code);
        }

        [TestCase("var disposable = this.serviceProvider.GetRequiredService<Disposable>();")]
        [TestCase("_ = this.serviceProvider.GetRequiredService<Disposable>();")]
        [TestCase("var loggerFactory = this.serviceProvider.GetRequiredService<ILoggerFactory>();")]
        [TestCase("_ = this.serviceProvider.GetRequiredService<ILoggerFactory>();")]
        public static void IServiceProviderGetRequiredServiceField(string statement)
        {
            var code = @"
namespace N
{
    using System;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    public class C
    {
        private readonly IServiceProvider serviceProvider;

        public C(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
            var disposable = this.serviceProvider.GetRequiredService<Disposable>();
        }

        public sealed class Disposable : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}".AssertReplace("var disposable = this.serviceProvider.GetRequiredService<Disposable>();", statement);
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void HostBuildRun()
        {
            var code = @"
namespace N
{
    using Microsoft.Extensions.Hosting;

    public class Program
    {
        public static void Main(string[] args)
        {
            Host.CreateDefaultBuilder(args).Build().Run();
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void HostBuildRunAsync()
        {
            var code = @"
namespace N
{
    using System.Threading.Tasks;
    using Microsoft.Extensions.Hosting;

    public class Program
    {
        public static async Task Main(string[] args)
        {
            await Host.CreateDefaultBuilder(args).Build().RunAsync();
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [TestCase("response.RegisterForDispose(new Disposable())")]
        [TestCase("response.RegisterForDisposeAsync(new Disposable())")]
        public static void RegisterForDispose(string expression)
        {
            var code = @"
namespace N
{
    using Microsoft.AspNetCore.Http;

    public class C
    {
        public void M(HttpResponse response)
        {
            response.RegisterForDispose(new Disposable());
        }
    }
}".AssertReplace("response.RegisterForDispose(new Disposable())", expression);

            var nullableContextOptions = CodeFactory.DefaultCompilationOptions(Analyzer, null).WithNullableContextOptions(NullableContextOptions.Enable);
            RoslynAssert.Valid(Analyzer, new[] { Disposable, code }, compilationOptions: nullableContextOptions);
        }
    }
}
