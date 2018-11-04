namespace IDisposableAnalyzers.Test.IDISP004DontIgnoreCreatedTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    public partial class Diagnostics
    {
        public class Invocation
        {
            private static readonly IDISP004DontIgnoreCreated Analyzer = new IDISP004DontIgnoreCreated();
            private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create("IDISP004");

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

            [Test]
            public void PropertyCreatingDisposableExpressionBody()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public static class Foo
    {
        public static Stream Stream => File.OpenRead(string.Empty);

        public static long Bar()
        {
            return ↓Stream.Length;
        }
    }
}";
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
                AnalyzerAssert.Diagnostics(Analyzer, testCode);
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
                AnalyzerAssert.Diagnostics(Analyzer, testCode);
            }
        }
    }
}
