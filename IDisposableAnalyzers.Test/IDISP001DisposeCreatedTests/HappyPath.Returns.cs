namespace IDisposableAnalyzers.Test.IDISP001DisposeCreatedTests
{
    using System.Threading.Tasks;
    using NUnit.Framework;

    internal partial class HappyPath
    {
        public class Returns : NestedHappyPathVerifier<HappyPath>
        {
            [Test]
            public async Task SimpleStatementBody()
            {
                var testCode = @"
    using System.IO;

    public static class Foo
    {
        public static Stream Bar()
        {
            return File.OpenRead(string.Empty);
        }
    }";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task SimpleExpressionBody()
            {
                var testCode = @"
    using System.IO;

    public static class Foo
    {
        public static Stream Bar() => File.OpenRead(string.Empty);
    }";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task LocalFileOpenRead()
            {
                var testCode = @"
    using System.IO;

    public static class Foo
    {
        public static Stream Bar()
        {
            var stream = File.OpenRead(string.Empty);
            return stream;
        }
    }";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task LocalFileOpenReadAfterAccessingLength()
            {
                var testCode = @"
    using System.IO;

    public static class Foo
    {
        public static Stream Bar()
        {
            var stream = File.OpenRead(string.Empty);
            var length = stream.Length;
            return stream;
        }
    }";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task LocalInIfAndEnd()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public static class Foo
    {
        public static Stream Bar()
        {
            var stream = File.OpenRead(string.Empty);
            if (true)
            {
                return stream;
            }

            return stream;
        }
    }
}";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task LocalInIf()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public static class Foo
    {
        public static Stream Bar(string text)
        {
            var stream = File.OpenRead(string.Empty);
            if (text == null)
            {
                return stream;
            }

            var length = stream.Length;
            return stream;
        }
    }
}";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task LocalInStreamReaderMethodBody()
            {
                var testCode = @"
    using System.IO;

    public static class Foo
    {
        public static StreamReader Bar()
        {
            var stream = File.OpenRead(string.Empty);
            return new StreamReader(stream);
        }
    }";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task FileOpenReadIsReturnedInCompositeDisposableMethodBody()
            {
                var testCode = @"
using System.IO;
using System.Reactive.Disposables;

public static class Foo
{
    public static CompositeDisposable Bar()
    {
        var stream = File.OpenRead(string.Empty);
        return new CompositeDisposable { stream };
    }
}";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task WhenDisposableIsReturnedPropertySimple()
            {
                var testCode = @"
    using System.IO;

    public static class Foo
    {
        public static Stream Bar
        {
            get
            {
                return File.OpenRead(string.Empty);;
            }
        }
    }";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task WhenDisposableIsReturnedPropertyBody()
            {
                var testCode = @"
    using System.IO;

    public static class Foo
    {
        public static Stream Bar
        {
            get
            {
                var stream = File.OpenRead(string.Empty);
                return stream;
            }
        }
    }";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task WhenDisposableIsReturnedPropertyExpressionBody()
            {
                var testCode = @"
    using System.IO;

    public static class Foo
    {
        public static Stream Bar => File.OpenRead(string.Empty);
    }";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }
        }
    }
}