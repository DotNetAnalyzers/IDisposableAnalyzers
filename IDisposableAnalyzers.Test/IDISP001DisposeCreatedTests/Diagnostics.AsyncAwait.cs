namespace IDisposableAnalyzers.Test.IDISP001DisposeCreatedTests;

using Gu.Roslyn.Asserts;
using NUnit.Framework;

public static partial class Diagnostics
{
    public static class AsyncAwait
    {
        private static readonly LocalDeclarationAnalyzer Analyzer = new();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.IDISP001DisposeCreated);

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

        [Test]
        public static void AwaitTaskRun()
        {
            var code = @"
namespace N
{
    using System.Threading.Tasks;

    internal static class C
    {
        internal static async Task M()
        {
            ↓var disposable = await Task.Run(() => new Disposable());
        }
    }
}";
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, Disposable, code);
        }

        [Test]
        public static void AwaitTaskFromResult()
        {
            var code = @"
namespace N
{
    using System.Threading.Tasks;

    internal static class C
    {
        internal static async Task M()
        {
            ↓var disposable = await Task.FromResult(new Disposable());
        }
    }
}";
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, Disposable, code);
        }

        [Test]
        public static void AwaitCreate()
        {
            var code = @"
namespace N
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
            await Task.Delay(10);
            return new Disposable();
        }
    }
}";
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, Disposable, code);
        }

        [Test]
        public static void AwaitCreateAsyncTaskFromResult()
        {
            var code = @"
namespace N
{
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
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, Disposable, code);
        }

        [Test]
        public static void AwaitRead()
        {
            var code = @"
namespace N
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
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }
    }
}
