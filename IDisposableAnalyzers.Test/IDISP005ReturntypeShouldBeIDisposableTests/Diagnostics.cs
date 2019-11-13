namespace IDisposableAnalyzers.Test.IDISP005ReturnTypeShouldBeIDisposableTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public static class Diagnostics
    {
        private static readonly DiagnosticAnalyzer Analyzer = new ReturnValueAnalyzer();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.IDISP005ReturnTypeShouldBeIDisposable);

        [Test]
        public static void ReturnFileOpenReadAsObject()
        {
            var code = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C
    {
        public object M()
        {
            return ↓File.OpenRead(string.Empty);
        }
    }
}";
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }

        [Test]
        public static void ReturnFileOpenReadAsDynamic()
        {
            var code = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C
    {
        public dynamic M()
        {
            return ↓File.OpenRead(string.Empty);
        }
    }
}";
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }

        [Test]
        public static void ReturnStaticFieldPasswordBoxSecurePasswordAsObject()
        {
            var code = @"
namespace N
{
    using System.Windows.Controls;

    public sealed class C
    {
        private static readonly PasswordBox PasswordBox = new PasswordBox();

        public object M()
        {
            return ↓PasswordBox.SecurePassword;
        }
    }
}";
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }

        [Test]
        public static void ReturnFieldPasswordBoxSecurePasswordAsObject()
        {
            var code = @"
namespace N
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
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }

        [Test]
        public static void IndexerReturningObject()
        {
            var code = @"
namespace N
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
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }

        [Test]
        public static void ReturnFileOpenReadAsObjectExpressionBody()
        {
            var code = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C
    {
        public object M() => ↓File.OpenRead(string.Empty);
    }
}";
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }

        [Test]
        public static void PropertyReturnFileOpenReadAsObjectExpressionBody()
        {
            var code = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C
    {
        public object P => ↓File.OpenRead(string.Empty);
    }
}";
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }

        [Test]
        public static void StatementLambda()
        {
            var code = @"
namespace N
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

            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }

        [Test]
        public static void ParenthesizedLambdaExpression()
        {
            var code = @"
namespace N
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

            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }

        [Test]
        public static void SimpleLambdaExpression()
        {
            var code = @"
namespace N
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

            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }
    }
}
