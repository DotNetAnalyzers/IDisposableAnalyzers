namespace IDisposableAnalyzers.Test.IDISP001DisposeCreatedTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public static partial class Diagnostics
    {
        public static class Local
        {
            private static readonly DiagnosticAnalyzer Analyzer = new LocalDeclarationAnalyzer();
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
            [TestCase("System.IO.File.OpenRead(string.Empty)")]
            [TestCase("new System.IO.BinaryReader(System.IO.File.OpenRead(string.Empty))")]
            [TestCase("System.IO.File.OpenRead(string.Empty) ?? null")]
            [TestCase("null ?? System.IO.File.OpenRead(string.Empty)")]
            [TestCase("true ? null : System.IO.File.OpenRead(string.Empty)")]
            [TestCase("true ? System.IO.File.OpenRead(string.Empty) : null")]
            public static void LanguageConstructs(string code)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    internal class C
    {
        internal C()
        {
            ↓var value = new Disposable();
        }
    }
}".AssertReplace("new Disposable()", code);
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, DisposableCode, testCode);
            }

            [TestCase("new BinaryReader(System.IO.File.OpenRead(string.Empty))")]
            [TestCase("new System.IO.BinaryReader(System.IO.File.OpenRead(string.Empty))")]
            public static void KnownArguments(string code)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    internal class C
    {
        internal C()
        {
            ↓var value = new System.IO.BinaryReader(System.IO.File.OpenRead(string.Empty));
        }
    }
}".AssertReplace("new System.IO.BinaryReader(System.IO.File.OpenRead(string.Empty))", code);
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, DisposableCode, testCode);
            }

            [Test]
            public static void PropertyInitializedPasswordBoxSecurePassword()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.Windows.Controls;

    public class C
    {
        public PasswordBox PasswordBox { get; } = new PasswordBox();

        public long M()
        {
            ↓var pwd = PasswordBox.SecurePassword;
            return pwd.Length;
        }
    }
}";

                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }

            [Test]
            public static void StaticPropertyInitializedPasswordBoxSecurePassword()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.Windows.Controls;

    public class C
    {
        public static PasswordBox PasswordBox { get; } = new PasswordBox();

        public long M()
        {
            ↓var pwd = PasswordBox.SecurePassword;
            return pwd.Length;
        }
    }
}";

                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }

            [Test]
            public static void FileOpenRead()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class C
    {
        public long M()
        {
            ↓var stream = File.OpenRead(string.Empty);
            return stream.Length;
        }
    }
}";

                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }

            [Test]
            public static void NewDisposable()
            {
                var testCode = @"
namespace RoslynSandbox
{
    public static class C
    {
        public static long M()
        {
            ↓var disposable = new Disposable();
            return 1;
        }
    }
}";
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, DisposableCode, testCode);
            }

            [Test]
            public static void MethodCreatingDisposable1()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public static class C
    {
        public static long M()
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
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }

            [Test]
            public static void MethodCreatingDisposable2()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public static class C
    {
        public static long M()
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
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }

            [Test]
            public static void MethodCreatingDisposableExpressionBody()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public static class C
    {
        public static long M()
        {
            ↓var stream = GetStream();
            return stream.Length;
        }

        public static Stream GetStream() => File.OpenRead(string.Empty);
    }
}";
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }

            [Test]
            public static void PropertyCreatingDisposableSimple()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public static class C
    {
        public static Stream Stream 
        {
           get { return File.OpenRead(string.Empty); }
        }

        public static long M()
        {
            ↓var stream = Stream;
            return stream.Length;
        }
    }
}";
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }

            [Test]
            public static void PropertyCreatingDisposableGetBody()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public static class C
    {
        public static Stream Stream 
        {
           get
           {
               var stream = File.OpenRead(string.Empty);
               return stream;
           }
        }

        public static long M()
        {
            ↓var stream = Stream;
            return stream.Length;
        }
    }
}";
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }

            [Test]
            public static void PropertyCreatingDisposableExpressionBody()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public static class C
    {
        public static Stream Stream => File.OpenRead(string.Empty);

        public static long M()
        {
            ↓var stream = Stream;
            return stream.Length;
        }
    }
}";
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }
        }
    }
}
