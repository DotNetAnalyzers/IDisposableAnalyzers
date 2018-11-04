// ReSharper disable InconsistentNaming
namespace IDisposableAnalyzers.Test.IDISP013AwaitInUsingTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public partial class ValidCode
    {
        private static readonly DiagnosticAnalyzer Analyzer = new ReturnValueAnalyzer();

        [Test]
        public void AwaitWebClientDownloadStringTaskAsyncInUsing()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Net;
    using System.Threading.Tasks;

    public class Foo
    {
        public async Task<string> Bar()
        {
            using (var client = new WebClient())
            {
                return await client.DownloadStringTaskAsync(string.Empty);
            }
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void UsingAwaited()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;
    using System.Threading.Tasks;

    public class Foo
    {
        public static async Task<string> BarAsync()
        {
            using (var stream = await ReadAsync(string.Empty).ConfigureAwait(false))
            {
                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadLine();
                }
            }
        }

        private static async Task<Stream> ReadAsync(string fileName)
        {
            var stream = new MemoryStream();
            using (var fileStream = File.OpenRead(fileName))
            {
                await fileStream.CopyToAsync(stream)
                                .ConfigureAwait(false);
            }

            stream.Position = 0;
            return stream;
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void TaskFromResult()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;
    using System.Threading.Tasks;

    public class Foo
    {
        public Task<int> Bar()
        {
            using (var stream = File.OpenRead(string.Empty))
            {
                return Task.FromResult(1);
            }
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void TaskCompletedTask()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;
    using System.Threading.Tasks;

    public class Foo
    {
        public Task Bar()
        {
            using (var stream = File.OpenRead(string.Empty))
            {
                return Task.CompletedTask;
            }
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void UsingNewBarTaskRun()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Threading.Tasks;

    public struct Bar : IDisposable
    {
        public Bar(Task task)
        {
            this.Task = task;
        }

        public Task Task { get; }

        public void Dispose()
        {
        }
    }

    public sealed class Foo
    {
        public Foo()
        {
            using (new Bar(Task.Run(() => 1)))
            {
            }
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void UsingNewBarLocalTaskRun()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Threading.Tasks;

    public struct Bar : IDisposable
    {
        public Bar(Task task)
        {
            this.Task = task;
        }

        public Task Task { get; }

        public void Dispose()
        {
        }
    }

    public sealed class Foo
    {
        public Foo()
        {
            using (var bar = new Bar(Task.Run(() => 1)))
            {
            }
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void UsingNewBarLocalFuncTask()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Threading.Tasks;

    public struct Bar : IDisposable
    {
        public Bar(Func<Task> task)
        {
            this.Task = task();
        }

        public Task Task { get; }

        public void Dispose()
        {
        }
    }

    public sealed class Foo
    {
        public Foo()
        {
            var fromResult = Task.FromResult(1);
            using (var bar = new Bar(() => fromResult))
            {
            }
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void ReturnNullAfterAwaitIssue89()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Net;
    using System.Threading.Tasks;

    public class Foo
    {
        public async Task<string> Bar()
        {
            using (var client = new WebClient())
            {
                await Task.Delay(0);
                return null;
            }
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void ReturnNullIssue89()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Net;
    using System.Threading.Tasks;

    public class Foo
    {
        public Task<string> Bar()
        {
            using (var client = new WebClient())
            {
                return null;
            }
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void EarlyReturnNullIssue89()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;

    public class Foo
    {
        private async Task<string> Retrieve(HttpClient client, Uri location)
        {
            using (HttpResponseMessage response = await client.GetAsync(location))
            {
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    return null;
                }

                return await response.Content.ReadAsStringAsync();
            }
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }
    }
}
