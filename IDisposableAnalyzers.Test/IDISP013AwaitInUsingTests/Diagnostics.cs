namespace IDisposableAnalyzers.Test.IDISP013AwaitInUsingTests;

using Gu.Roslyn.Asserts;
using NUnit.Framework;

public static class Diagnostics
{
    private static readonly ReturnValueAnalyzer Analyzer = new();
    private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.IDISP013AwaitInUsing);

    [Test]
    public static void WebClientDownloadStringTaskAsync()
    {
        var code = @"
#pragma warning disable SYSLIB0014
namespace N
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
        RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
    }

    [Test]
    public static void ValueTask()
    {
        var code = @"
namespace N
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
        RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
    }

    [Test]
    public static void UsingDeclaration()
    {
        var code = @"
namespace N
{
    using System.Threading.Tasks;

    public static class C
    {
        public static ValueTask<int> MAsync()
        {
            using var file = System.IO.File.OpenRead(string.Empty);
            return ↓InnerAsync();
        }

        private static async ValueTask<int> InnerAsync()
        {
            await Task.Delay(10).ConfigureAwait(false);
            return 1;
        }
    }
}";
        RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
    }

    [Test]
    public static void UsingDeclarationInner()
    {
        var code = @"
namespace N
{
    using System.Threading.Tasks;

    public static class C
    {
        public static ValueTask<int> MAsync()
        {
            using var file = System.IO.File.OpenRead(string.Empty);
            while (true)
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
        RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
    }

    [Test]
    public static void LocalTask()
    {
        var code = @"
#pragma warning disable SYSLIB0014
namespace N
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
        RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
    }

    [Test]
    public static void TaskCompletionSourceTask()
    {
        var code = @"
#pragma warning disable SYSLIB0014
namespace N
{
    using System.Net;
    using System.Threading.Tasks;

    public class C
    {
        static Task M()
        {
            var tcs = new TaskCompletionSource<bool>();
            using (var client = new WebClient())
            {
                return ↓tcs.Task;
            }
        }
    }
}";
        RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
    }
}
