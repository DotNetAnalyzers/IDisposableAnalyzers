namespace IDisposableAnalyzers.Test.IDISP001DisposeCreatedTests
{
    using System.Threading.Tasks;
    using NUnit.Framework;

    internal partial class HappyPath : HappyPathVerifier<IDISP001DisposeCreated>
    {
        internal class Assigns : NestedHappyPathVerifier<HappyPath>
        {
            [Test]
            public async Task DontUseUsingWhenAssigningAField()
            {
                var testCode = @"
    using System.IO;

    public static class Foo
    {
        private static readonly Stream Stream = File.OpenRead(string.Empty);

        public static long Bar()
        {
            var stream = Stream;
            return stream.Length;
        }
    }";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task DontUseUsingWhenAssigningAFieldTernary()
            {
                var testCode = @"
    using System.IO;

    public class Foo
    {
        private readonly Stream stream;

        public Foo()
        {
            var temp = File.OpenRead(string.Empty);
            this.stream = true 
                ? temp
                : temp;
        }
    }";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task DontUseUsingWhenAssigningAFieldInAMethod()
            {
                var testCode = @"
    using System.IO;

    public class Foo
    {
        private Stream stream;

        public void Bar()
        {
            this.stream = File.OpenRead(string.Empty);
        }
    }";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task DontUseUsingWhenAssigningAFieldInAMethodLocalVariable()
            {
                var testCode = @"
    using System.IO;

    public class Foo
    {
        private Stream stream;

        public void Bar()
        {
            var newStream = File.OpenRead(string.Empty);
            this.stream = newStream;
        }
    }";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task DontUseUsingWhenAddingLocalVariableToFieldList()
            {
                var testCode = @"
    using System.Collections.Generic;
    using System.IO;

    public class Foo
    {
        private readonly List<Stream> streams = new List<Stream>();

        public void Bar()
        {
            var stream = File.OpenRead(string.Empty);
            this.streams.Add(stream);
        }
    }";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task DontUseUsingWhenAssigningACallThatReturnsAStaticField()
            {
                var testCode = @"
using System.IO;

public static class Foo
{
    private static readonly Stream Stream = File.OpenRead(string.Empty);

    public static long Bar()
    {
        var stream = GetStream();
        return stream.Length;
    }

    public static Stream GetStream()
    {
        return Stream;
    }
}";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task DontUseUsingWhenAssigningACallThatReturnsAField()
            {
                var testCode = @"
using System.IO;

public class Foo
{
    private readonly Stream cachedStream = File.OpenRead(string.Empty);

    public long Bar()
    {
        var stream = GetStream();
        return stream.Length;
    }

    public Stream GetStream()
    {
        return cachedStream;
    }
}";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task DontUseUsingWhenAssigningACallThatReturnsAFieldSwitch()
            {
                var testCode = @"
using System;
using System.IO;

public static class Foo
{
    private static readonly Stream Stream = File.OpenRead(string.Empty);

    public static long Bar()
    {
        var stream = GetStream(FileAccess.Read);
        return stream.Length;
    }

    public static Stream GetStream(FileAccess fileAccess)
    {
        switch (fileAccess)
        {
            case FileAccess.Read:
                return Stream;
            case FileAccess.Write:
                return Stream;
            case FileAccess.ReadWrite:
                return Stream;
            default:
                throw new ArgumentOutOfRangeException(nameof(fileAccess), fileAccess, null);
        }
    }
}";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task BuildCollectionThenAssignFieldIndexer()
            {
                var testCode = @"
public class Foo
{
    private Disposable[] disposables = new Disposable[2];

    public Foo()
    {
        for (var i = 0; i < 2; i++)
        {
            var item = new Disposable();
            this.disposables[i] = item;
        }
    }
}";

                await this.VerifyHappyPathAsync(DisposableCode, testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task BuildCollectionThenAssignField()
            {
                var testCode = @"
public class Foo
{
    private Disposable[] disposables;

    public Foo()
    {
        var items = new Disposable[2];
        for (var i = 0; i < 2; i++)
        {
            var item = new Disposable();
            items[i] = item;
        }

        this.disposables = items;
    }
}";

                await this.VerifyHappyPathAsync(DisposableCode, testCode)
                          .ConfigureAwait(false);
            }
        }
    }
}