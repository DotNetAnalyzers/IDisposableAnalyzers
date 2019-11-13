namespace IDisposableAnalyzers.Test.IDISP001DisposeCreatedTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    public static partial class Valid<T>
    {
        [TestCase("Tuple.Create(File.OpenRead(file), new object())")]
        [TestCase("Tuple.Create(File.OpenRead(file), File.OpenRead(file))")]
        [TestCase("new Tuple<FileStream, object>(File.OpenRead(file), new object())")]
        [TestCase("new Tuple<FileStream, FileStream>(File.OpenRead(file), File.OpenRead(file))")]
        public static void LocalTupleThatIsDisposed(string expression)
        {
            var testCode = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C
    {
        public C(string file)
        {
            var tuple = Tuple.Create(File.OpenRead(file), new object());
            tuple.Item1.Dispose();
            (tuple.Item2 as IDisposable)?.Dispose();
        }
    }
}".AssertReplace("Tuple.Create(File.OpenRead(file), new object())", expression);

            RoslynAssert.Valid(Analyzer, testCode);
        }

        [TestCase("(File.OpenRead(file), new object())")]
        [TestCase("(File.OpenRead(file), File.OpenRead(file))")]
        public static void LocalValueTupleThatDisposed(string expression)
        {
            var testCode = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C
    {
        public C(string file)
        {
            var tuple = (File.OpenRead(file), new object());
            tuple.Item1.Dispose();
            (tuple.Item2 as IDisposable)?.Dispose();
        }
    }
}".AssertReplace("(File.OpenRead(file), new object())", expression);

            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void FieldTuple()
        {
            var testCode = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        private readonly Tuple<FileStream, FileStream> tuple;

        public C(string file1, string file2)
        {
            this.tuple = Tuple.Create(File.OpenRead(file1), File.OpenRead(file2));
        }

        public void Dispose()
        {
            this.tuple.Item1.Dispose();
            this.tuple.Item2.Dispose();
        }
    }
}";

            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void FieldTupleWithLocals()
        {
            var testCode = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        private readonly Tuple<FileStream, FileStream> tuple;

        public C(string file1, string file2)
        {
            var stream1 = File.OpenRead(file1);
            var stream2 = File.OpenRead(file2);
            this.tuple = Tuple.Create(stream1, stream2);
        }

        public void Dispose()
        {
            this.tuple.Item1.Dispose();
            this.tuple.Item2.Dispose();
        }
    }
}";

            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void FieldValueTuple()
        {
            var testCode = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        private readonly (FileStream, FileStream) tuple;

        public C(string file)
        {
            this.tuple = (File.OpenRead(file), File.OpenRead(file));
        }

        public void Dispose()
        {
            this.tuple.Item1.Dispose();
            this.tuple.Item2.Dispose();
        }
    }
}";

            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void FieldValueTupleWithLocals()
        {
            var testCode = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        private readonly (FileStream, FileStream) tuple;

        public C(string file1, string file2)
        {
            var stream1 = File.OpenRead(file1);
            var stream2 = File.OpenRead(file2);
            this.tuple = (stream1, stream2);
        }

        public void Dispose()
        {
            this.tuple.Item1.Dispose();
            this.tuple.Item2.Dispose();
        }
    }
}";

            RoslynAssert.Valid(Analyzer, testCode);
        }

        [TestCase("Pair.Create(File.OpenRead(file1), File.OpenRead(file2))")]
        [TestCase("new Pair<FileStream>(File.OpenRead(file1), File.OpenRead(file2))")]
        public static void LocalPairThatIsDisposed(string expression)
        {
            var staticPairCode = @"
namespace N
{
    public static class Pair
    {
        public static Pair<T> Create<T>(T item1, T item2) => new Pair<T>(item1, item2);
    }
}";

            var pairOfT = @"
namespace N
{
    public class Pair<T>
    {
        public Pair(T item1, T item2)
        {
            this.Item1 = item1;
            this.Item2 = item2;
        }

        public T Item1 { get; }

        public T Item2 { get; }
    }
}";

            var testCode = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C
    {
        public C(string file1, string file2)
        {
            var pair = Pair.Create(File.OpenRead(file1), File.OpenRead(file2));
            pair.Item1.Dispose();
            pair.Item2.Dispose();
        }
    }
}".AssertReplace("Pair.Create(File.OpenRead(file1), File.OpenRead(file2))", expression);

            RoslynAssert.Valid(Analyzer, pairOfT, staticPairCode, testCode);
        }

        [TestCase("Tuple.Create(File.OpenRead(file1), File.OpenRead(file2))")]
        [TestCase("new Tuple<FileStream, FileStream>(File.OpenRead(file1), File.OpenRead(file2))")]
        public static void FieldTupleThatIsDisposed(string expression)
        {
            var testCode = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        private readonly Tuple<FileStream, FileStream> tuple;

        public C(string file1, string file2)
        {
            this.tuple = Tuple.Create(File.OpenRead(file1), File.OpenRead(file2));
        }

        public void Dispose()
        {
            this.tuple.Item1.Dispose();
            this.tuple.Item2.Dispose();
        }
    }
}".AssertReplace("Tuple.Create(File.OpenRead(file1), File.OpenRead(file2))", expression);

            RoslynAssert.Valid(Analyzer, testCode);
        }

        [TestCase("(File.OpenRead(file1), File.OpenRead(file2))")]
        public static void FieldValueTupleThatIsDisposed(string expression)
        {
            var testCode = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        private readonly (FileStream, FileStream) tuple;

        public C(string file1, string file2)
        {
            this.tuple = (File.OpenRead(file1), File.OpenRead(file2));
        }

        public void Dispose()
        {
            this.tuple.Item1.Dispose();
            this.tuple.Item2.Dispose();
        }
    }
}".AssertReplace("(File.OpenRead(file1), File.OpenRead(file2))", expression);

            RoslynAssert.Valid(Analyzer, testCode);
        }

        [TestCase("Pair.Create(File.OpenRead(file1), File.OpenRead(file2))")]
        [TestCase("new Pair<FileStream>(File.OpenRead(file1), File.OpenRead(file2))")]
        public static void FieldPairThatIsDisposed(string expression)
        {
            var staticPairCode = @"
namespace N
{
    public static class Pair
    {
        public static Pair<T> Create<T>(T item1, T item2) => new Pair<T>(item1, item2);
    }
}";

            var pairOfT = @"
namespace N
{
    public class Pair<T>
    {
        public Pair(T item1, T item2)
        {
            this.Item1 = item1;
            this.Item2 = item2;
        }

        public T Item1 { get; }

        public T Item2 { get; }
    }
}";

            var testCode = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        private readonly Pair<FileStream> pair;

        public C(string file1, string file2)
        {
            this.pair = Pair.Create(File.OpenRead(file1), File.OpenRead(file2));
        }

        public void Dispose()
        {
            this.pair.Item1.Dispose();
            this.pair.Item2.Dispose();
        }
    }
}".AssertReplace("Pair.Create(File.OpenRead(file1), File.OpenRead(file2))", expression);

            RoslynAssert.Valid(Analyzer, pairOfT, staticPairCode, testCode);
        }

        [TestCase("Tuple.Create(File.OpenRead(file1), File.OpenRead(file2))")]
        [TestCase("new Tuple<FileStream, FileStream>(File.OpenRead(file1), File.OpenRead(file2))")]
        public static void FieldTuple(string expression)
        {
            var testCode = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        private readonly Tuple<FileStream, FileStream> tuple;

        public C(string file1, string file2)
        {
            this.tuple = Tuple.Create(File.OpenRead(file1), File.OpenRead(file2));
        }

        public void Dispose()
        {
            this.tuple.Item1.Dispose();
            this.tuple.Item2.Dispose();
        }
    }
}".AssertReplace("Tuple.Create(File.OpenRead(file1), File.OpenRead(file2))", expression);

            RoslynAssert.Valid(Analyzer, testCode);
        }

        [TestCase("(File.OpenRead(file1), File.OpenRead(file2))")]
        public static void FieldValueTuple(string expression)
        {
            var testCode = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        private readonly (FileStream, FileStream) tuple;

        public C(string file1, string file2)
        {
            this.tuple = (File.OpenRead(file1), File.OpenRead(file2));
        }

        public void Dispose()
        {
            this.tuple.Item1.Dispose();
            this.tuple.Item2.Dispose();
        }
    }
}".AssertReplace("(File.OpenRead(file1), File.OpenRead(file2))", expression);

            RoslynAssert.Valid(Analyzer, testCode);
        }

        [TestCase("StaticPair.Create(File.OpenRead(file1), File.OpenRead(file2))")]
        [TestCase("new Pair<FileStream>(File.OpenRead(file1), File.OpenRead(file2))")]
        public static void Pair(string expression)
        {
            var staticPair = @"
namespace N
{
    public static class StaticPair
    {
        public static Pair<T> Create<T>(T item1, T item2) => new Pair<T>(item1, item2);
    }
}";

            var pairOfT = @"
namespace N
{
    public class Pair<T>
    {
        public Pair(T item1, T item2)
        {
            this.Item1 = item1;
            this.Item2 = item2;
        }

        public T Item1 { get; }

        public T Item2 { get; }
    }
}";

            var testCode = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        private readonly Pair<FileStream> pair;

        public C(string file1, string file2)
        {
            this.pair = StaticPair.Create(File.OpenRead(file1), File.OpenRead(file2));
        }

        public void Dispose()
        {
            this.pair.Item1.Dispose();
            this.pair.Item2.Dispose();
        }
    }
}".AssertReplace("StaticPair.Create(File.OpenRead(file1), File.OpenRead(file2))", expression);

            RoslynAssert.Valid(Analyzer, pairOfT, staticPair, testCode);
        }

        [TestCase("Create(File.OpenRead(file1), File.OpenRead(file2))")]
        [TestCase("new Pair<FileStream>(File.OpenRead(file1), File.OpenRead(file2))")]
        public static void DisposingPair(string expression)
        {
            var testCode = @"
namespace N
{
    using System;
    using System.IO;

    internal sealed class C : IDisposable
    {
        private readonly Pair<FileStream> pair;

        public C(string file1, string file2)
        {
            this.pair = Create(File.OpenRead(file1), File.OpenRead(file2));
        }

        public void Dispose()
        {
            this.pair.Dispose();
        }

        private static Pair<T> Create<T>(T x, T y) where T : IDisposable => new Pair<T>(x, y);

        private class Pair<T> : IDisposable
            where T : IDisposable
        {
            private readonly T item1;
            private readonly T item2;

            public Pair(T item1, T item2)
            {
                this.item1 = item1;
                this.item2 = item2;
            }

            public void Dispose()
            {
                this.item1.Dispose();
                (this.item2 as IDisposable)?.Dispose();
            }
        }
    }
}".AssertReplace("Create(File.OpenRead(file1), File.OpenRead(file2))", expression);

            RoslynAssert.Valid(Analyzer, testCode);
        }
    }
}
