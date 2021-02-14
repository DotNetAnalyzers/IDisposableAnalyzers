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
            private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.IDISP001DisposeCreated);

            private const string Disposable = @"
namespace N
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
            [TestCase("o switch { int _ => File.OpenRead(string.Empty), _ => null }")]
            public static void LanguageConstructs(string expression)
            {
                var code = @"
namespace N
{
    using System;
    using System.IO;

    class C
    {
        void M(object o)
        {
            ↓var value = new Disposable();
        }
    }
}".AssertReplace("new Disposable()", expression);
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, Disposable, code);
            }

            [TestCase("new BinaryReader(System.IO.File.OpenRead(string.Empty))")]
            [TestCase("new System.IO.BinaryReader(System.IO.File.OpenRead(string.Empty))")]
            public static void KnownArguments(string expression)
            {
                var code = @"
namespace N
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
}".AssertReplace("new System.IO.BinaryReader(System.IO.File.OpenRead(string.Empty))", expression);
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, Disposable, code);
            }

            [Test]
            public static void PropertyInitializedPasswordBoxSecurePassword()
            {
                var code = @"
namespace N
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

                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
            }

            [Test]
            public static void StaticPropertyInitializedPasswordBoxSecurePassword()
            {
                var code = @"
namespace N
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

                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
            }

            [Test]
            public static void FileOpenRead()
            {
                var code = @"
namespace N
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

                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
            }

            [Test]
            public static void NewDisposable()
            {
                var code = @"
namespace N
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
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, Disposable, code);
            }

            [Test]
            public static void MethodCreatingDisposable1()
            {
                var code = @"
namespace N
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
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
            }

            [Test]
            public static void MethodCreatingDisposable2()
            {
                var code = @"
namespace N
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
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
            }

            [Test]
            public static void MethodCreatingDisposableExpressionBody()
            {
                var code = @"
namespace N
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
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
            }

            [Test]
            public static void PropertyCreatingDisposableSimple()
            {
                var code = @"
namespace N
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
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
            }

            [Test]
            public static void PropertyCreatingDisposableGetBody()
            {
                var code = @"
namespace N
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
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
            }

            [Test]
            public static void PropertyCreatingDisposableExpressionBody()
            {
                var code = @"
namespace N
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
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
            }

            [Test]
            public static void InterlockedExchange()
            {
                var code = @"
namespace N
{
    using System;
    using System.IO;
    using System.Threading;

    sealed class C : IDisposable
    {
        private IDisposable _disposable = new MemoryStream();

        public void Update()
        {
            ↓var oldValue = Interlocked.Exchange(ref _disposable, new MemoryStream());
        }

        public void Dispose()
        {
            var oldValue = Interlocked.Exchange(ref _disposable, null);
            oldValue?.Dispose();
        }
    }
}";
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
            }

            [Test]
            public static void ReturningIfTrueItemReturnNullAfter()
            {
                var code = @"
namespace N
{
    using System;
    using System.IO;

    sealed class C
    {
        MemoryStream M(bool condition)
        {
            ↓var item = new MemoryStream();
            if (condition)
            {
                return item;
            }

            return null;
        }
    }
}";
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
            }

            [Test]
            public static void ReturningIfTrueItemElseNull()
            {
                var code = @"
namespace N
{
    using System;
    using System.IO;

    sealed class C
    {
        MemoryStream M(bool condition)
        {
            ↓var item = new MemoryStream();
            if (condition)
            {
                return item;
            }
            else
            {
                return null;
            }
        }
    }
}";
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
            }
        }
    }
}
