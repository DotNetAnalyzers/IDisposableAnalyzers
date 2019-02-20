namespace IDisposableAnalyzers.Test.IDISP004DontIgnoreCreatedTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public partial class Diagnostics
    {
        public class Invocation
        {
            private static readonly DiagnosticAnalyzer Analyzer = new CreationAnalyzer();
            private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(IDISP004DontIgnoreCreated.Descriptor);

            private const string DisposableCode = @"
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

            [Test]
            public void FileOpenRead()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo
    {
        public void Meh()
        {
            ↓File.OpenRead(string.Empty);
        }
    }
}";
                AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }

            [Test]
            public void FileOpenReadPassedIntoCtor()
            {
                var barCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Bar
    {
        private readonly Stream stream;

        public Bar(Stream stream)
        {
           this.stream = stream;
        }
    }
}";
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public sealed class Foo
    {
        public Bar Meh()
        {
            return new Bar(↓File.OpenRead(string.Empty));
        }
    }
}";
                AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, barCode, testCode);
            }

            [Test]
            public void Generic()
            {
                var interfaceCode = @"
namespace RoslynSandbox
{
    using System;
 
    public interface IDisposable<T> : IDisposable
    {
    }
}";

                var disposableCode = @"
namespace RoslynSandbox
{
    public sealed class Disposable<T> : IDisposable<T>
    {
        public void Dispose()
        {
        }
    }
}";

                var factoryCode = @"
namespace RoslynSandbox
{
    public class Factory
    {
        public static IDisposable<T> Create<T>() => new Disposable<T>();
    }
}";

                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        public void Bar()
        {
            ↓Factory.Create<int>();
        }
    }
}";
                AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, interfaceCode, disposableCode, factoryCode, testCode);
            }

            [Test]
            public void MethodCreatingDisposableExpressionBodyToString()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public static class Foo
    {
        public static Stream Stream() => File.OpenRead(string.Empty);

        public static long Bar()
        {
            return ↓Stream().Length;
        }
    }
}";
                AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }

            [TestCase("Stream.Length")]
            [TestCase("this.Stream.Length")]
            public void PropertyCreatingDisposableExpressionBody(string expression)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class C
    {
        public Stream Stream => File.OpenRead(string.Empty);

        public long? M() => ↓this.Stream.Length;
    }
}".AssertReplace("this.Stream.Length", expression);
                AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }

            [TestCase("Stream.Length")]
            public void StaticPropertyCreatingDisposableExpressionBody(string expression)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public static class C
    {
        public static Stream Stream => File.OpenRead(string.Empty);

        public static long? M() => ↓Stream.Length;
    }
}".AssertReplace("Stream.Length", expression);
                AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }

            [Test]
            public void NoFixForArgument()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        internal static string Bar()
        {
            return Meh(↓File.OpenRead(string.Empty));
        }

        private static string Meh(Stream stream) => stream.ToString();
    }
}";

                AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }

            [Test]
            public void FactoryMethodNewDisposable()
            {
                var testCode = @"
namespace RoslynSandbox
{
    public sealed class Foo
    {
        public void Meh()
        {
            ↓Create();
        }

        private static Disposable Create()
        {
            return new Disposable();
        }
    }
}";
                AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, DisposableCode, testCode);
            }

            [Test]
            public void FactoryConstrainedGeneric()
            {
                var factoryCode = @"
namespace RoslynSandbox
{
    using System;

    public class Factory
    {
        public static T Create<T>() where T : IDisposable, new() => new T();
    }
}";

                var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public void Bar()
        {
            ↓Factory.Create<Disposable>();
        }
    }
}";
                AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, factoryCode, DisposableCode, testCode);
            }

            [Test]
            public void WithOptionalParameter()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections.Generic;

    public class Foo
    {
        public Foo(IDisposable disposable)
        {
            ↓Bar(disposable);
        }

        private static IDisposable Bar(IDisposable disposable, List<IDisposable> list = null)
        {
            if (list == null)
            {
                list = new List<IDisposable>();
            }

            if (list.Contains(disposable))
            {
                return new Disposable();
            }

            list.Add(disposable);
            return Bar(disposable, list);
        }
    }
}";
                AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, DisposableCode, testCode);
            }

            [Test]
            public void AddingFileOpenReadToListOfObject()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    public sealed class Foo
    {
        private List<object> streams = new List<object>();

        public Foo()
        {
            streams.Add(↓File.OpenRead(string.Empty));
        }
    }
}";
                AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }

            [Test]
            public void DiscardFileOpenRead()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo
    {
        public Foo()
        {
            _ = ↓File.OpenRead(string.Empty);
        }
    }
}";
                AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }

            [TestCase("Pair.Create(↓File.OpenRead(file1), ↓File.OpenRead(file2))")]
            [TestCase("new Pair<FileStream>(↓File.OpenRead(file1), ↓File.OpenRead(file2))")]
            public void Pair(string expression)
            {
                var staticPairCode = @"
namespace RoslynSandbox
{
    public static class Pair
    {
        public static Pair<T> Create<T>(T item1, T item2) => new Pair<T>(item1, item2);
    }
}";

                var genericPairCode = @"
namespace RoslynSandbox
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
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        public C(string file1, string file2)
        {
            var pair = Pair.Create(↓File.OpenRead(file1), ↓File.OpenRead(file2));
        }
    }
}".AssertReplace("Pair.Create(↓File.OpenRead(file1), ↓File.OpenRead(file2))", expression);

                AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, genericPairCode, staticPairCode, testCode);
            }
        }
    }
}
