namespace IDisposableAnalyzers.Test.IDISP001DisposeCreatedTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    // ReSharper disable once UnusedTypeParameter
    public static partial class Valid<T>
    {
        [Test]
        public static void DontUseUsingWhenAssigningAField()
        {
            var code = @"
namespace N
{
    using System.IO;

    public static class C
    {
        private static readonly Stream Stream = File.OpenRead(string.Empty);

        public static long M()
        {
            var stream = Stream;
            return stream.Length;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void DontUseUsingWhenAssigningAFieldTernary()
        {
            var code = @"
namespace N
{
    using System.IO;

    public class C
    {
        private readonly Stream stream;

        public C(bool flag)
        {
            var temp = File.OpenRead(string.Empty);
            this.stream = flag 
                ? null
                : temp;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void DontUseUsingWhenAssigningAFieldInAMethod()
        {
            var code = @"
namespace N
{
    using System.IO;

    public class C
    {
        private Stream stream;

        public void M()
        {
            this.stream.Dispose();
            this.stream = File.OpenRead(string.Empty);
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void DontUseUsingWhenAssigningAFieldInAMethodLocalVariable()
        {
            var code = @"
namespace N
{
    using System.IO;

    public class C
    {
        private Stream stream;

        public void M()
        {
            var newStream = File.OpenRead(string.Empty);
            this.stream?.Dispose();
            this.stream = newStream;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void DontUseUsingWhenAddingLocalVariableToFieldList()
        {
            var code = @"
namespace N
{
    using System.Collections.Generic;
    using System.IO;

    public class C
    {
        private readonly List<Stream> streams = new List<Stream>();

        public void M()
        {
            var stream = File.OpenRead(string.Empty);
            this.streams.Add(stream);
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void DontUseUsingWhenAssigningACallThatReturnsAStaticField()
        {
            var code = @"
namespace N
{
    using System.IO;

    public static class C
    {
        private static readonly Stream Stream = File.OpenRead(string.Empty);

        public static long M()
        {
            var stream = GetStream();
            return stream.Length;
        }

        public static Stream GetStream()
        {
            return Stream;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void DontUseUsingWhenAssigningACallThatReturnsAField()
        {
            var code = @"
namespace N
{
    using System.IO;

    public class C
    {
        private readonly Stream cachedStream = File.OpenRead(string.Empty);

        public long M()
        {
            var stream = GetStream();
            return stream.Length;
        }

        public Stream GetStream()
        {
            return cachedStream;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void DontUseUsingWhenAssigningACallThatReturnsAFieldSwitch()
        {
            var code = @"
namespace N
{
    using System;
    using System.IO;

    public static class C
    {
        private static readonly Stream Stream = File.OpenRead(string.Empty);

        public static long M()
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
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void BuildCollectionThenAssignFieldIndexer()
        {
            var code = @"
namespace N
{
    public class C
    {
        private Disposable[] disposables = new Disposable[2];

        public C()
        {
            for (var i = 0; i < 2; i++)
            {
                var item = new Disposable();
                this.disposables[i] = item;
            }
        }
    }
}";

            RoslynAssert.Valid(Analyzer, Disposable, code);
        }

        [Test]
        public static void BuildCollectionThenAssignField()
        {
            var code = @"
namespace N
{
    public class C
    {
        private Disposable[] disposables;

        public C()
        {
            var items = new Disposable[2];
            for (var i = 0; i < 2; i++)
            {
                var item = new Disposable();
                items[i] = item;
            }

            this.disposables = items;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, Disposable, code);
        }
    }
}
