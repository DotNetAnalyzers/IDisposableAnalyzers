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

    public sealed class C
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
            public void FileOpenReadPassedIntoCtorOfNotDisposing()
            {
                var barCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class M
    {
        private readonly Stream stream;

        public M(Stream stream)
        {
           this.stream = stream;
        }
    }
}";
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public sealed class C
    {
        public M Meh()
        {
            return new M(↓File.OpenRead(string.Empty));
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

    public class C
    {
        public void M()
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

    public static class C
    {
        public static Stream Stream() => File.OpenRead(string.Empty);

        public static long M()
        {
            return ↓Stream().Length;
        }
    }
}";
                AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }

            [TestCase("this.Stream().ReadAsync(null, 0, 0)")]
            [TestCase("this.Stream()?.ReadAsync(null, 0, 0)")]
            [TestCase("Stream().ReadAsync(null, 0, 0)")]
            [TestCase("Stream()?.ReadAsync(null, 0, 0)")]
            public void MethodCreatingDisposableExpressionBodyAsync(string expression)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;
    using System.Threading.Tasks;

    public class C
    {
        public Stream Stream() => File.OpenRead(string.Empty);

        public async Task<int> M() => await ↓Stream().ReadAsync(null, 0, 0);
    }
}".AssertReplace("Stream().ReadAsync(null, 0, 0)", expression);

                AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }

            [TestCase("Stream.Length")]
            [TestCase("Stream?.Length")]
            [TestCase("this.Stream.Length")]
            [TestCase("this.Stream?.Length")]
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

            [TestCase("this.Stream.ReadAsync(null, 0, 0)")]
            [TestCase("this.Stream?.ReadAsync(null, 0, 0)")]
            [TestCase("Stream.ReadAsync(null, 0, 0)")]
            [TestCase("Stream?.ReadAsync(null, 0, 0)")]
            public void PropertyCreatingDisposableExpressionBodyAsync(string expression)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class C
    {
        public Stream Stream => File.OpenRead(string.Empty);

        public async Task<int> M() => await ↓Stream.ReadAsync(null, 0, 0);
    }
}".AssertReplace("Stream.ReadAsync(null, 0, 0)", expression);

                AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }

            [TestCase("Stream.Length")]
            [TestCase("Stream?.Length")]
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

    public class C
    {
        internal static string M()
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
    public sealed class C
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
    public class C
    {
        public void M()
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

    public class C
    {
        public C(IDisposable disposable)
        {
            ↓M(disposable);
        }

        private static IDisposable M(IDisposable disposable, List<IDisposable> list = null)
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
            return M(disposable, list);
        }
    }
}";
                AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, DisposableCode, testCode);
            }

            [Test]
            public void DiscardFileOpenRead()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class C
    {
        public C()
        {
            _ = ↓File.OpenRead(string.Empty);
        }
    }
}";
                AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }
        }
    }
}
