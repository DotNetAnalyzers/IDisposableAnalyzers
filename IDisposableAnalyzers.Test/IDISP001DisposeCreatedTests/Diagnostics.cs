namespace IDisposableAnalyzers.Test.IDISP001DisposeCreatedTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    internal partial class Diagnostics
    {
        private static readonly DiagnosticAnalyzer Analyzer = new IDISP001DisposeCreated();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(IDISP001DisposeCreated.Descriptor);

        private static readonly string DisposableCode = @"
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

        [TestCase("new Disposable()")]
        [TestCase("new Disposable() as object")]
        [TestCase("(object) new Disposable()")]
        [TestCase("File.OpenRead(string.Empty) ?? null")]
        [TestCase("null ?? File.OpenRead(string.Empty)")]
        [TestCase("true ? null : File.OpenRead(string.Empty)")]
        [TestCase("true ? File.OpenRead(string.Empty) : null")]
        public void LanguageConstructs(string code)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    internal class Foo
    {
        internal Foo()
        {
            ↓var value = new Disposable();
        }
    }
}".AssertReplace("new Disposable()", code);
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, DisposableCode, testCode);
        }

        [Test]
        public void PropertyInitializedPasswordBoxSecurePassword()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows.Controls;

    public class Foo
    {
        public PasswordBox PasswordBox { get; } = new PasswordBox();

        public long Bar()
        {
            ↓var pwd = PasswordBox.SecurePassword;
            return pwd.Length;
        }
    }
}";

            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public void StaticPropertyInitializedPasswordBoxSecurePassword()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows.Controls;

    public class Foo
    {
        public static PasswordBox PasswordBox { get; } = new PasswordBox();

        public long Bar()
        {
            ↓var pwd = PasswordBox.SecurePassword;
            return pwd.Length;
        }
    }
}";

            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public void FileOpenRead()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        public long Bar()
        {
            ↓var stream = File.OpenRead(string.Empty);
            return stream.Length;
        }
    }
}";

            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public void NewDisposable()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public static class Foo
    {
        public static long Bar()
        {
            ↓var disposable = new Disposable();
            return 1;
        }
    }
}";
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, DisposableCode, testCode);
        }

        [Test]
        public void MethodCreatingDisposable1()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public static class Foo
    {
        public static long Bar()
        {
            ↓var stream = GetStream();
            return stream.Length;
        }

        public static Stream GetStream()
        {
            return File.OpenRead(string.Empty);
        }
    }
}";
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public void MethodCreatingDisposable2()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public static class Foo
    {
        public static long Bar()
        {
            ↓var stream = GetStream();
            return stream.Length;
        }

        public static Stream GetStream()
        {
            var stream = File.OpenRead(string.Empty);
            return stream;
        }
    }
}";
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public void MethodCreatingDisposableExpressionBody()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public static class Foo
    {
        public static long Bar()
        {
            ↓var stream = GetStream();
            return stream.Length;
        }

        public static Stream GetStream() => File.OpenRead(string.Empty);
    }
}";
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public void PropertyCreatingDisposableSimple()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public static class Foo
    {
        public static Stream Stream 
        {
           get { return File.OpenRead(string.Empty); }
        }

        public static long Bar()
        {
            ↓var stream = Stream;
            return stream.Length;
        }
    }
}";
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public void PropertyCreatingDisposableGetBody()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public static class Foo
    {
        public static Stream Stream 
        {
           get
           {
               var stream = File.OpenRead(string.Empty);
               return stream;
           }
        }

        public static long Bar()
        {
            ↓var stream = Stream;
            return stream.Length;
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
            ↓var stream = Stream;
            return stream.Length;
        }
    }
}";
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }
    }
}
