#pragma warning disable SA1203 // Constants must appear before fields
namespace IDisposableAnalyzers.Test.IDISP001DisposeCreatedTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal partial class HappyPath
    {
        private static readonly IDISP001DisposeCreated Analyzer = new IDISP001DisposeCreated();

        private const string DisposableCode = @"
namespace RoslynSandbox
{
    using System;

    public class Disposable : IDisposable
    {
        public Disposable(string meh)
            : this()
        {
        }

        public Disposable()
        {
        }

        public void Dispose()
        {
        }
    }
}";

        [TestCase("1")]
        [TestCase("new string(' ', 1)")]
        [TestCase("typeof(IDisposable)")]
        [TestCase("(IDisposable)null")]
        [TestCase("await Task.FromResult(1)")]
        [TestCase("await Task.Run(() => 1)")]
        [TestCase("await Task.Run(() => new object())")]
        [TestCase("await Task.Run(() => Type.GetType(string.Empty))")]
        [TestCase("await Task.Run(() => this.GetType())")]
        public void LanguageConstructs(string code)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    internal class Foo
    {
        internal async void Bar()
        {
            var value = new string(' ', 1);
        }
    }
}";
            testCode = testCode.AssertReplace("new string(' ', 1)", code);
            AnalyzerAssert.Valid(Analyzer, DisposableCode, testCode);
        }

        [Test]
        public void WhenDisposingVariable()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public void Meh()
        {
            var item = new Disposable();
            item.Dispose();
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, DisposableCode, testCode);
        }

        [Test]
        public void UsingFileStream()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public static class Foo
    {
        public static long Bar()
        {
            using (var stream = File.OpenRead(string.Empty))
            {
                return stream.Length;
            }
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void UsingNewDisposable()
        {
            var disposableCode = @"
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

            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public static class Foo
    {
        public static long Bar()
        {
            using (var meh = new Disposable())
            {
                return 1;
            }
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode, disposableCode);
        }

        [Test]
        public void Awaiting()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;
    using System.Threading.Tasks;
  
    internal static class Foo
    {
        internal static async Task Bar()
        {
            using (var stream = await ReadAsync(string.Empty))
            {
            }
        }

        internal static async Task<Stream> ReadAsync(string file)
        {
            var stream = new MemoryStream();
            using (var fileStream = File.OpenRead(file))
            {
                await fileStream.CopyToAsync(stream)
                                ;
            }

            stream.Position = 0;
            return stream;
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void AwaitingMethodReturningString()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;
    using System.Threading.Tasks;
  
    internal static class Foo
    {
        internal static async Task Bar()
        {
            var text = await ReadAsync(string.Empty);
        }

        internal static async Task<string> ReadAsync(string text)
        {
            return text;
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void AwaitDownloadDataTask()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Net;
    using System.Threading.Tasks;

    public class Foo
    {
        public async Task Bar()
        {
            using (var client = new WebClient())
            {
                var bytes = await client.DownloadDataTaskAsync(string.Empty);
            }
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void FactoryMethod()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Disposal : IDisposable
    {
        private Stream stream;

        public Disposal() :
            this(File.OpenRead(string.Empty))
        {
        }

        private Disposal(Stream stream)
        {
            this.stream = stream;
        }

        public static Disposal CreateNew()
        {
            Stream stream = File.OpenRead(string.Empty);
            return new Disposal(stream);
        }

        public void Dispose()
        {
            if (stream != null)
            {
                stream.Dispose();
                stream = null;
            }
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void InjectedDbConnectionCreateCommand()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Data.Common;

    public class Foo
    {
        public static void Bar(DbConnection conn)
        {
            using(var command = conn.CreateCommand())
            {
            }
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void InjectedMemberDbConnectionCreateCommand()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Data.Common;

    public class Foo
    {
        private readonly DbConnection connection;

        public Foo(DbConnection connection)
        {
            this.connection = connection;
        }

        public void Bar()
        {
            using(var command = this.connection.CreateCommand())
            {
            }
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void DisposedInEventLambda()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Diagnostics;
    using System.Threading.Tasks;

    public class Foo
    {
        static Task RunProcessAsync(string fileName)
        {
            // there is no non-generic TaskCompletionSource
            var tcs = new TaskCompletionSource<bool>();

            var process = new Process
                          {
                              StartInfo = { FileName = fileName },
                              EnableRaisingEvents = true
                          };

            process.Exited += (sender, args) =>
            {
                tcs.SetResult(true);
                process.Dispose();
            };

            process.Start();

            return tcs.Task;
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }
    }
}
