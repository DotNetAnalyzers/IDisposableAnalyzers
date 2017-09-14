namespace IDisposableAnalyzers.Test.IDISP004DontIgnoreReturnValueOfTypeIDisposableTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal partial class HappyPath
    {
        internal class Using
        {
            [Test]
            public void FileOpenRead()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        public void Bar()
        {
            using (var stream = File.OpenRead(string.Empty))
            {
            }
        }
    }
}";
                AnalyzerAssert.NoDiagnostics<IDISP004DontIgnoreReturnValueOfTypeIDisposable>(testCode);
            }

            [Test]
            public void NewStreamReader()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        public void Bar()
        {
            using (var reader = new StreamReader(File.OpenRead(string.Empty)))
            {
            }
        }
    }
}";
                AnalyzerAssert.NoDiagnostics<IDISP004DontIgnoreReturnValueOfTypeIDisposable>(testCode);
            }

            [Test]
            public void SampleWithAwait()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public static class Foo
    {
        public static async Task<T> WithCancellation<T>(this Task<T> task, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            using (cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).TrySetResult(true), tcs))
            {
                if (task != await Task.WhenAny(task, tcs.Task).ConfigureAwait(false))
                {
                    throw new OperationCanceledException(cancellationToken);
                }
            }

            return await task.ConfigureAwait(false);
        }
    }
}";
                AnalyzerAssert.NoDiagnostics<IDISP004DontIgnoreReturnValueOfTypeIDisposable>(testCode);
            }
        }
    }
}