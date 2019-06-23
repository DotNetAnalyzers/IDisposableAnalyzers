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

    public class C
    {
        public async Task<string> M()
        {
            using (var client = new WebClient())
            {
                return await client.DownloadStringTaskAsync(string.Empty);
            }
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void UsingAwaited()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;
    using System.Threading.Tasks;

    public class C
    {
        public static async Task<string> MAsync()
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
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void TaskFromResult()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;
    using System.Threading.Tasks;

    public class C
    {
        public Task<int> M()
        {
            using (var stream = File.OpenRead(string.Empty))
            {
                return Task.FromResult(1);
            }
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void TaskCompletedTask()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;
    using System.Threading.Tasks;

    public class C
    {
        public Task M()
        {
            using (var stream = File.OpenRead(string.Empty))
            {
                return Task.CompletedTask;
            }
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void UsingNewMTaskRun()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Threading.Tasks;

    public struct M : IDisposable
    {
        public M(Task task)
        {
            this.Task = task;
        }

        public Task Task { get; }

        public void Dispose()
        {
        }
    }

    public sealed class C
    {
        public C()
        {
            using (new M(Task.Run(() => 1)))
            {
            }
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void UsingNewMLocalTaskRun()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Threading.Tasks;

    public struct M : IDisposable
    {
        public M(Task task)
        {
            this.Task = task;
        }

        public Task Task { get; }

        public void Dispose()
        {
        }
    }

    public sealed class C
    {
        public C()
        {
            using (var bar = new M(Task.Run(() => 1)))
            {
            }
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void UsingNewMLocalFuncTask()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Threading.Tasks;

    public struct M : IDisposable
    {
        public M(Func<Task> task)
        {
            this.Task = task();
        }

        public Task Task { get; }

        public void Dispose()
        {
        }
    }

    public sealed class C
    {
        public C()
        {
            var fromResult = Task.FromResult(1);
            using (var bar = new M(() => fromResult))
            {
            }
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void ReturnNullAfterAwaitIssue89()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Net;
    using System.Threading.Tasks;

    public class C
    {
        public async Task<string> M()
        {
            using (var client = new WebClient())
            {
                await Task.Delay(0);
                return null;
            }
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void ReturnNullIssue89()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Net;
    using System.Threading.Tasks;

    public class C
    {
        public Task<string> M()
        {
            using (var client = new WebClient())
            {
                return null;
            }
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
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

    public class C
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
            RoslynAssert.Valid(Analyzer, testCode);
        }
    }
}
