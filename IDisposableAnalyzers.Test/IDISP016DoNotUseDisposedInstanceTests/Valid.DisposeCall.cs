namespace IDisposableAnalyzers.Test.IDISP016DoNotUseDisposedInstanceTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis;
    using NUnit.Framework;

    public static class Valid
    {
        public static class DisposeCall
        {
            private static readonly DisposeCallAnalyzer Analyzer = new();
            private static readonly DiagnosticDescriptor Descriptor = Descriptors.IDISP016DoNotUseDisposedInstance;

            [Test]
            public static void CreateTouchDispose()
            {
                var code = @"
namespace N
{
    using System.IO;

    public sealed class C
    {
        public void M()
        {
            var stream = File.OpenRead(string.Empty);
            var b = stream.ReadByte();
            stream.Dispose();
        }
    }
}";
                RoslynAssert.Valid(Analyzer, Descriptor, code);
            }

            [Test]
            public static void UsingFileOpenRead()
            {
                var code = @"
namespace N
{
    using System.IO;

    public class C
    {
        public void M()
        {
            using (var stream = File.OpenRead(string.Empty))
            {
                 var b = stream.ReadByte();
            }
        }
    }
}";
                RoslynAssert.Valid(Analyzer, Descriptor, code);
            }

            [Test]
            public static void DisposeInUsing()
            {
                // this is weird but should not warn I think
                var code = @"
namespace N
{
    using System.IO;

    public class C
    {
        public void M()
        {
            using (var stream = File.OpenRead(string.Empty))
            {
                stream.Dispose();
            }
        }
    }
}";
                RoslynAssert.Valid(Analyzer, Descriptor, code);
            }

            [Test]
            public static void IfDisposeReturn()
            {
                var code = @"
namespace N
{
    using System.IO;

    public sealed class C
    {
        public void M(bool b)
        {
            var stream = File.OpenRead(string.Empty);
            if (b)
            {
                stream.Dispose();
                return;
            }

            var bb = stream.ReadByte();
            stream.Dispose();
        }
    }
}";
                RoslynAssert.Valid(Analyzer, Descriptor, code);
            }

            [Test]
            public static void IfDisposeThrow()
            {
                var code = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C
    {
        public void M(bool b)
        {
            var stream = File.OpenRead(string.Empty);
            if (b)
            {
                stream.Dispose();
                throw new Exception();
            }

            var bb = stream.ReadByte();
            stream.Dispose();
        }
    }
}";
                RoslynAssert.Valid(Analyzer, Descriptor, code);
            }

            [Test]
            public static void ReassignAfterDispose()
            {
                var code = @"
namespace N
{
    using System.IO;

    public sealed class C
    {
        public void M()
        {
            var stream = File.OpenRead(string.Empty);
            var b = stream.ReadByte();
            stream.Dispose();
            stream = File.OpenRead(string.Empty);
            b = stream.ReadByte();
            stream.Dispose();
        }
    }
}";
                RoslynAssert.Valid(Analyzer, Descriptor, code);
            }

            [Test]
            public static void ReassignViaOutAfterDispose()
            {
                var code = @"
namespace N
{
    using System.IO;

    public sealed class C
    {
        public void M()
        {
            Stream stream;
            Create(out stream);
            var b = stream.ReadByte();
            stream.Dispose();
            Create(out stream);
            b = stream.ReadByte();
            stream.Dispose();
        }

        private static void Create(out Stream stream)
        {
            stream = File.OpenRead(string.Empty);
        }
    }
}";
                RoslynAssert.Valid(Analyzer, Descriptor, code);
            }

            [TestCase("Tuple.Create(File.OpenRead(file1), File.OpenRead(file2))")]
            [TestCase("new Tuple<FileStream, FileStream>(File.OpenRead(file1), File.OpenRead(file2))")]
            public static void Tuple(string expression)
            {
                var code = @"
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

                RoslynAssert.Valid(Analyzer, code);
            }

            [TestCase("Tuple.Create(File.OpenRead(file), File.OpenRead(file))")]
            [TestCase("new Tuple<FileStream, FileStream>(File.OpenRead(file), File.OpenRead(file))")]
            public static void LocalTuple(string expression)
            {
                var code = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C
    {
        public C(string file)
        {
            var tuple = Tuple.Create(File.OpenRead(file), File.OpenRead(file));
            tuple.Item1.Dispose();
            tuple.Item2.Dispose();
        }
    }
}".AssertReplace("Tuple.Create(File.OpenRead(file), File.OpenRead(file))", expression);

                RoslynAssert.Valid(Analyzer, code);
            }

            [TestCase("Tuple.Create(File.OpenRead(file), File.OpenRead(file))")]
            [TestCase("new Tuple<FileStream, FileStream>(File.OpenRead(file), File.OpenRead(file))")]
            public static void ListOfTuple(string expression)
            {
                var code = @"
namespace N
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    sealed class C : IDisposable
    {
        private readonly List<Tuple<FileStream, FileStream>> xs = new List<Tuple<FileStream, FileStream>>();

        public C(string file)
        {
            this.xs.Add(Tuple.Create(File.OpenRead(file), File.OpenRead(file)));
        }

        public void Dispose()
        {
            foreach (var tuple in this.xs)
            {
                tuple.Item1.Dispose();
                tuple.Item2.Dispose();
            }
        }
    }
}".AssertReplace("Tuple.Create(File.OpenRead(file), File.OpenRead(file))", expression);

                RoslynAssert.Valid(Analyzer, code);
            }

            [Test]
            public static void ListOfValueTuple()
            {
                var code = @"
namespace N
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    sealed class C : IDisposable
    {
        private readonly List<(FileStream, FileStream)> xs = new List<(FileStream, FileStream)>();

        public C(string file)
        {
            this.xs.Add((File.OpenRead(file), File.OpenRead(file)));
        }

        public void Dispose()
        {
            foreach (var tuple in this.xs)
            {
                tuple.Item1.Dispose();
                tuple.Item2.Dispose();
            }
        }
    }
}";

                RoslynAssert.Valid(Analyzer, code);
            }

            [Test]
            public static void LeaveOpenLocals()
            {
                var code = @"
namespace N
{
    using System.IO;
    using System.Text;

    public class C
    {
        public C(string fileName)
        {
            using (var stream = File.OpenRead(fileName))
            {
                using (var reader = new StreamReader(stream, new UTF8Encoding(), true, 1024, leaveOpen: true))
                {
                    _ = reader.ReadLine();
                }

                _ = stream.ReadByte();
            }
        }
    }
}";

                RoslynAssert.Valid(Analyzer, code);
            }

            [Test]
            public static void LeaveOpenFields()
            {
                var code = @"
namespace N
{
    using System;
    using System.IO;
    using System.Text;

    public sealed class C : IDisposable
    {
        private readonly FileStream stream;
        private readonly StreamReader reader;

        public C(string fileName)
        {
            this.stream = File.OpenRead(fileName);
            this.reader = new StreamReader(stream, new UTF8Encoding(), true, 1024, leaveOpen: true);
        }

        public string? ReadLine() => this.reader.ReadLine();
 
        public int ReadByte() => this.stream.ReadByte();

        public void Dispose()
        {
            this.stream.Dispose();
            this.reader.Dispose();
        }
    }
}";

                RoslynAssert.Valid(Analyzer, code);
            }
        }
    }
}
