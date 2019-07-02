namespace IDisposableAnalyzers.Test.IDISP005ReturntypeShouldBeIDisposableTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public static class Diagnostics
    {
        private static readonly DiagnosticAnalyzer Analyzer = new ReturnValueAnalyzer();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(IDISP005ReturntypeShouldBeIDisposable.Descriptor);

        [Test]
        public static void ReturnFileOpenReadAsObject()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class C
    {
        public object Meh()
        {
            return ↓File.OpenRead(string.Empty);
        }
    }
}";
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public static void ReturnFileOpenReadAsDynamic()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class C
    {
        public dynamic Meh()
        {
            return ↓File.OpenRead(string.Empty);
        }
    }
}";
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public static void ReturnStaticFieldPasswordBoxSecurePasswordAsObject()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows.Controls;

    public sealed class C
    {
        private static readonly PasswordBox PasswordBox = new PasswordBox();

        public object Meh()
        {
            return ↓PasswordBox.SecurePassword;
        }
    }
}";
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public static void ReturnFieldPasswordBoxSecurePasswordAsObject()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows.Controls;

    public sealed class C
    {
        private readonly PasswordBox PasswordBox = new PasswordBox();

        public object Meh()
        {
            return ↓PasswordBox.SecurePassword;
        }
    }
}";
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public static void IndexerReturningObject()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class C
    {
        public void M()
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
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public static void ReturnFileOpenReadAsObjectExpressionBody()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class C
    {
        public object Meh() => ↓File.OpenRead(string.Empty);
    }
}";
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public static void PropertyReturnFileOpenReadAsObjectExpressionBody()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class C
    {
        public object Meh => ↓File.OpenRead(string.Empty);
    }
}";
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public static void StatementLambda()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    internal static class C
    {
        internal static void M()
        {
            Func<object> f = () =>
                {
                    return ↓System.IO.File.OpenRead(null);
                };
        }
    }
}";

            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public static void ParenthesizedLambdaExpression()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    internal static class C
    {
        internal static void M()
        {
            Func<object> f = () => ↓System.IO.File.OpenRead(null);
        }
    }
}";

            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public static void SimpleLambdaExpression()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    internal static class C
    {
        internal static void M()
        {
            Func<int,object> f = x => ↓System.IO.File.OpenRead(null);
        }
    }
}";

            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }
    }
}
