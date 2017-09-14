namespace IDisposableAnalyzers.Test.IDISP001DisposeCreatedTests
{
    using System.Threading.Tasks;
    using NUnit.Framework;

    internal partial class HappyPath : HappyPathVerifier<IDISP001DisposeCreated>
    {
        private static readonly string DisposableCode = @"
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
        [TestCase("await Task.Run(() => 1).ConfigureAwait(false)")]
        [TestCase("await Task.Run(() => new object())")]
        [TestCase("await Task.Run(() => new object()).ConfigureAwait(false)")]
        [TestCase("await Task.Run(() => Type.GetType(string.Empty))")]
        [TestCase("await Task.Run(() => Type.GetType(string.Empty)).ConfigureAwait(false)")]
        [TestCase("await Task.Run(() => this.GetType())")]
        [TestCase("await Task.Run(() => this.GetType()).ConfigureAwait(false)")]
        public async Task LanguageConstructs(string code)
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
            await this.VerifyHappyPathAsync(DisposableCode, testCode).ConfigureAwait(false);
        }

        [Test]
        public async Task WhenDisposingVariable()
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

            await this.VerifyHappyPathAsync(DisposableCode, testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task UsingFileStream()
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
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task UsingNewDisposable()
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
            await this.VerifyHappyPathAsync(testCode, disposableCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task Awaiting()
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
                                .ConfigureAwait(false);
            }

            stream.Position = 0;
            return stream;
        }
    }
}";

            await this.VerifyHappyPathAsync(testCode)
            .ConfigureAwait(false);
        }

        [Test]
        public async Task AwaitingMethodReturningString()
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

            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task AwaitDownloadDataTaskAsync()
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
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task FactoryMethod()
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
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [TestCase("disposables.First();")]
        [TestCase("disposables.First(x => x != null);")]
        [TestCase("disposables.Where(x => x != null);")]
        [TestCase("disposables.Single();")]
        [TestCase("Enumerable.Empty<IDisposable>();")]
        public async Task IgnoreLinq(string linq)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Linq;

    public sealed class Foo
    {
        public Foo(IDisposable[] disposables)
        {
            var first = disposables.First();
        }
    }
}";
            testCode = testCode.AssertReplace("disposables.First();", linq);
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task InjectedDbConnectionCreateCommand()
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
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task InjectedMemberDbConnectionCreateCommand()
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
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }
    }
}