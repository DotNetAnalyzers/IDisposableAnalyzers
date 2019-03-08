#pragma warning disable AvoidAsyncSuffix // Avoid Async suffix
namespace IDisposableAnalyzers.Test.IDISP013AwaitInUsingTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public class Diagnostics
    {
        private static readonly DiagnosticAnalyzer Analyzer = new ReturnValueAnalyzer();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create("IDISP013");

        [Test]
        public void WebClientDownloadStringTaskAsync()
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
                return ↓client.DownloadStringTaskAsync(string.Empty);
            }
        }
    }
}";
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public void ValueTask()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Threading.Tasks;

    public static class C
    {
        public static ValueTask<int> MAsync()
        {
            using (System.IO.File.OpenRead(string.Empty))
            {
                return ↓InnerAsync();
            }
        }

        private static async ValueTask<int> InnerAsync()
        {
            await Task.Delay(10).ConfigureAwait(false);
            return 1;
        }
    }
}";
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public void LocalTask()
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
                var task = client.DownloadStringTaskAsync(string.Empty);
                return ↓task;
            }
        }
    }
}";
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public void TaskCompletionSourceTask()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Threading.Tasks;

    public class C
    {
        static Task M()
        {
            var tcs = new TaskCompletionSource<bool>();
            using (var disposable = new Disposable())
            {
                return tcs.Task;
            }
        }
    }
}";
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }
    }
}
