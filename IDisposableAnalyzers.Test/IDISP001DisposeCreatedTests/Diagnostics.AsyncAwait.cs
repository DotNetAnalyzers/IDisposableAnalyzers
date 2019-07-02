namespace IDisposableAnalyzers.Test.IDISP001DisposeCreatedTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public static partial class Diagnostics
    {
        public static class AsyncAwait
        {
            private static readonly DiagnosticAnalyzer Analyzer = new LocalDeclarationAnalyzer();
            private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(IDISP001DisposeCreated.Descriptor);

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

            [Test]
            public static void AwaitTaskRun()
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
                RoslynAssert.Diagnostics(Analyzer, DisposableCode, testCode);
            }

            [Test]
            public static void AwaitTaskFromResult()
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
                RoslynAssert.Diagnostics(Analyzer, DisposableCode, testCode);
            }

            [Test]
            public static void AwaitCreate()
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
                RoslynAssert.Diagnostics(Analyzer, DisposableCode, testCode);
            }

            [Test]
            public static void AwaitCreateAsyncTaskFromResult()
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
                RoslynAssert.Diagnostics(Analyzer, DisposableCode, testCode);
            }

            [Test]
            public static void AwaitRead()
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
                RoslynAssert.Diagnostics(Analyzer, testCode);
            }
        }
    }
}
