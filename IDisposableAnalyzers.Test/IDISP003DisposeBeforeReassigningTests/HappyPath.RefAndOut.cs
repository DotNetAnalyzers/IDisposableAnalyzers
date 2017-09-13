namespace IDisposableAnalyzers.Test.IDISP003DisposeBeforeReassigningTests
{
    using System.Threading.Tasks;

    using NUnit.Framework;

    internal partial class HappyPath : HappyPathVerifier<IDISP003DisposeBeforeReassigning>
    {
        internal class RefAndOut : NestedHappyPathVerifier<IDISP004DontIgnoreReturnValueOfTypeIDisposableTests.HappyPath>
        {
            [Test]
            public async Task AssigningVariableViaOutParameter()
            {
                var testCode = @"
using System;
using System.IO;

public class Foo
{
    public bool Update()
    {
        Stream stream;
        return TryGetStream(out stream);
    }

    public bool TryGetStream(out Stream result)
    {
        result = File.OpenRead(string.Empty);
        return true;
    }
}";
                await this.VerifyHappyPathAsync(testCode).ConfigureAwait(false);
            }

            [Test]
            public async Task AssigningVariableViaOutParameterTwiceDisposingBetweenCalls()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        public void Bar()
        {
            Stream stream;
            TryGetStream(out stream);
            stream?.Dispose();
            TryGetStream(out stream);
        }

        public bool TryGetStream(out Stream result)
        {
            result = File.OpenRead(string.Empty);
            return true;
        }
    }
}";
                await this.VerifyHappyPathAsync(testCode).ConfigureAwait(false);
            }

            [Test]
            public async Task AssigningFieldViaConcurrentDictionaryTryGetValue()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.Collections.Concurrent;
    using System.IO;

    public class Foo
    {
        private readonly ConcurrentDictionary<int, Stream> Cache = new ConcurrentDictionary<int, Stream>();

        private Stream current;

        public bool Update(int number)
        {
            return this.Cache.TryGetValue(number, out this.current);
        }
    }
}";
                await this.VerifyHappyPathAsync(testCode).ConfigureAwait(false);
            }

            [Test]
            public async Task AssigningFieldViaConcurrentDictionaryTryGetValueTwice()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.Collections.Concurrent;
    using System.IO;

    public class Foo
    {
        private readonly ConcurrentDictionary<int, Stream> Cache = new ConcurrentDictionary<int, Stream>();

        private Stream current;

        public bool Update(int number)
        {
            return this.Cache.TryGetValue(number, out this.current);
            return this.Cache.TryGetValue(number + 1, out this.current);
        }
    }
}";
                await this.VerifyHappyPathAsync(testCode).ConfigureAwait(false);
            }

            [Test]
            public async Task AssigningFieldWithCahcedViaOutParameter()
            {
                var testCode = @"
using System;
using System.IO;

public class Foo
{
    private Stream stream;

    public bool Update()
    {
        return TryGetStream(out stream);
    }

    public bool TryGetStream(out Stream result)
    {
        result = this.stream;
        return true;
    }
}";

                await this.VerifyHappyPathAsync(testCode).ConfigureAwait(false);
            }

            [Test]
            public async Task AssigningVariableViaRefParameter()
            {
                var testCode = @"
using System;
using System.IO;

public class Foo
{
    public void Bar()
    {
        Stream stream = null;
        Assign(ref stream);
    }

    public void Assign(ref Stream result)
    {
        result = File.OpenRead(string.Empty);
    }
}";
                await this.VerifyHappyPathAsync(testCode).ConfigureAwait(false);
            }

            [Test]
            public async Task AssigningVariableViaRefParameterTwiceDisposingBetweenCalls()
            {
                var testCode = @"
using System;
using System.IO;

public class Foo
{
    public void Bar()
    {
        Stream stream = null;
        Assign(ref stream);
        stream?.Dispose();
        Assign(ref stream);
    }

    public void Assign(ref Stream result)
    {
        result = File.OpenRead(string.Empty);
    }
}";
                await this.VerifyHappyPathAsync(testCode).ConfigureAwait(false);
            }
        }
    }
}