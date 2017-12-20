namespace IDisposableAnalyzers.Test.IDISP005ReturnTypeShouldIndicateIDisposableTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal class Diagnostics
    {
        private static readonly ReturnValueAnalyzer Analyzer = new ReturnValueAnalyzer();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create("IDISP005");

        [Test]
        public void ReturnFileOpenReadAsObject()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo
    {
        public object Meh()
        {
            return ↓File.OpenRead(string.Empty);
        }
    }
}";
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public void ReturnFileOpenReadAsDynamic()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo
    {
        public dynamic Meh()
        {
            return ↓File.OpenRead(string.Empty);
        }
    }
}";
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public void ReturnStaticFieldPasswordBoxSecurePasswordAsObject()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows.Controls;

    public sealed class Foo
    {
        private static readonly PasswordBox PasswordBox = new PasswordBox();

        public object Meh()
        {
            return ↓PasswordBox.SecurePassword;
        }
    }
}";
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public void ReturnFieldPasswordBoxSecurePasswordAsObject()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows.Controls;

    public sealed class Foo
    {
        private readonly PasswordBox PasswordBox = new PasswordBox();

        public object Meh()
        {
            return ↓PasswordBox.SecurePassword;
        }
    }
}";
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public void IndexerReturningObject()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        public void Bar()
        {
            var meh = this[0];
        }

        public object this[int index]
        {
            get
            {
                return ↓File.OpenRead(string.Empty);
            }
        }
    }
}";
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public void ReturnFileOpenReadAsObjectExpressionBody()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo
    {
        public object Meh() => ↓File.OpenRead(string.Empty);
    }
}";
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public void PropertyReturnFileOpenReadAsObjectExpressionBody()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo
    {
        public object Meh => ↓File.OpenRead(string.Empty);
    }
}";
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public void StatementLambda()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    internal static class Foo
    {
        internal static void Bar()
        {
            Func<object> f = () =>
                {
                    return ↓System.IO.File.OpenRead(null);
                };
        }
    }
}";

            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public void ParenthesizedLambdaExpression()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    internal static class Foo
    {
        internal static void Bar()
        {
            Func<object> f = () => ↓System.IO.File.OpenRead(null);
        }
    }
}";

            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public void SimpleLambdaExpression()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    internal static class Foo
    {
        internal static void Bar()
        {
            Func<int,object> f = x => ↓System.IO.File.OpenRead(null);
        }
    }
}";

            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }
    }
}
