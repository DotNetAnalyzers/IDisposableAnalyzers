namespace IDisposableAnalyzers.Test.IDISP001DisposeCreatedTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    public partial class Diagnostics
    {
        public class AsyncAwait
        {
            [Test]
            public void AwaitTaskRun()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Threading.Tasks;

    internal static class C
    {
        internal static async Task M()
        {
            ↓var disposable = await Task.Run(() => new Disposable());
        }
    }
}";
                AnalyzerAssert.Diagnostics(Analyzer, DisposableCode, testCode);
            }

            [Test]
            public void AwaitTaskFromResult()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Threading.Tasks;

    internal static class C
    {
        internal static async Task M()
        {
            ↓var disposable = await Task.FromResult(new Disposable());
        }
    }
}";
                AnalyzerAssert.Diagnostics(Analyzer, DisposableCode, testCode);
            }

            [Test]
            public void AwaitCreate()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Threading.Tasks;

    internal static class C
    {
        internal static async Task M()
        {
            ↓var stream = await CreateAsync();
        }

        internal static async Task<IDisposable> CreateAsync()
        {
            return new Disposable();
        }
    }
}";
                AnalyzerAssert.Diagnostics(Analyzer, DisposableCode, testCode);
            }

            [Test]
            public void AwaitCreateAsyncTaskFromResult()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Threading.Tasks;

    internal static class C
    {
        internal static async Task M()
        {
            ↓var stream = await CreateAsync();
        }

        internal static Task<Disposable> CreateAsync()
        {
            return Task.FromResult(new Disposable());
        }
    }
}";
                AnalyzerAssert.Diagnostics(Analyzer, DisposableCode, testCode);
            }

            [Test]
            public void AwaitRead()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;
    using System.Threading.Tasks;
  
    internal static class C
    {
        internal static async Task M()
        {
            ↓var stream = await ReadAsync(string.Empty);
        }

        internal static async Task<Stream> ReadAsync(string file)
        {
            var stream = new MemoryStream();
            using (var fileStream = File.OpenRead(file))
            {
                await fileStream.CopyToAsync(stream)
                                .ConfigureAwait(false);
            }

            stream.Position = 0;
            return stream;
        }
    }
}";
                AnalyzerAssert.Diagnostics(Analyzer, testCode);
            }
        }
    }
}
