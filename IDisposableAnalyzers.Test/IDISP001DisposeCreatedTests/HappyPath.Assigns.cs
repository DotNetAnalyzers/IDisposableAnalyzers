namespace IDisposableAnalyzers.Test.IDISP001DisposeCreatedTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal partial class HappyPath
    {
        internal class Assigns
        {
            [Test]
            public void DontUseUsingWhenAssigningAField()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public static class Foo
    {
        private static readonly Stream Stream = File.OpenRead(string.Empty);

        public static long Bar()
        {
            var stream = Stream;
            return stream.Length;
        }
    }
}";
                AnalyzerAssert.Valid<IDISP001DisposeCreated>(testCode);
            }

            [Test]
            public void DontUseUsingWhenAssigningAFieldTernary()
            {
                var testCode = @"
namespace RoslynSandbox
{
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
    }
}";
                AnalyzerAssert.Valid<IDISP001DisposeCreated>(testCode);
            }

            [Test]
            public void DontUseUsingWhenAssigningAFieldInAMethod()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        private Stream stream;

        public void Bar()
        {
            this.stream = File.OpenRead(string.Empty);
        }
    }
}";
                AnalyzerAssert.Valid<IDISP001DisposeCreated>(testCode);
            }

            [Test]
            public void DontUseUsingWhenAssigningAFieldInAMethodLocalVariable()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        private Stream stream;

        public void Bar()
        {
            var newStream = File.OpenRead(string.Empty);
            this.stream = newStream;
        }
    }
}";
                AnalyzerAssert.Valid<IDISP001DisposeCreated>(testCode);
            }

            [Test]
            public void DontUseUsingWhenAddingLocalVariableToFieldList()
            {
                var testCode = @"
namespace RoslynSandbox
{
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
    }
}";
                AnalyzerAssert.Valid<IDISP001DisposeCreated>(testCode);
            }

            [Test]
            public void DontUseUsingWhenAssigningACallThatReturnsAStaticField()
            {
                var testCode = @"
namespace RoslynSandbox
{
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
    }
}";
                AnalyzerAssert.Valid<IDISP001DisposeCreated>(testCode);
            }

            [Test]
            public void DontUseUsingWhenAssigningACallThatReturnsAField()
            {
                var testCode = @"
namespace RoslynSandbox
{
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
    }
}";
                AnalyzerAssert.Valid<IDISP001DisposeCreated>(testCode);
            }

            [Test]
            public void DontUseUsingWhenAssigningACallThatReturnsAFieldSwitch()
            {
                var testCode = @"
namespace RoslynSandbox
{
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
    }
}";
                AnalyzerAssert.Valid<IDISP001DisposeCreated>(testCode);
            }

            [Test]
            public void BuildCollectionThenAssignFieldIndexer()
            {
                var testCode = @"
namespace RoslynSandbox
{
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
    }
}";

                AnalyzerAssert.Valid<IDISP001DisposeCreated>(DisposableCode, testCode);
            }

            [Test]
            public void BuildCollectionThenAssignField()
            {
                var testCode = @"
namespace RoslynSandbox
{
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
    }
}";
                AnalyzerAssert.Valid<IDISP001DisposeCreated>(DisposableCode, testCode);
            }
        }
    }
}