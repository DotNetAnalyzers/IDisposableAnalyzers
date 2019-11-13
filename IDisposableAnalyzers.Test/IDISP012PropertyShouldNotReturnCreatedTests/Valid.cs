namespace IDisposableAnalyzers.Test.IDISP012PropertyShouldNotReturnCreatedTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public static class Valid
    {
        private static readonly DiagnosticAnalyzer Analyzer = new ReturnValueAnalyzer();

        private const string DisposableCode = @"
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

        [Test]
        public static void PropertyReturning1()
        {
            var testCode = @"
namespace N
{
    public sealed class C
    {
        public int Value
        {
            get
            {
                return 1;
            }
        }
    }
}";

            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void PropertyReturningBoxed1()
        {
            var testCode = @"
namespace N
{
    public sealed class C
    {
        public object Value
        {
            get
            {
                return 1;
            }
        }
    }
}";

            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void PropertyReturning1ExpressionBody()
        {
            var testCode = @"
namespace N
{
    public sealed class C
    {
        public int Value
        {
            get
            {
                return 1;
            }
        }
    }
}";

            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void PropertyReturningNewTimeSpan()
        {
            var testCode = @"
namespace N
{
    using System;

    public sealed class C
    {
        public TimeSpan Value
        {
            get
            {
                return new TimeSpan(1);
            }
        }
    }
}";

            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void PropertyReturningNewTimeSpanExpressionBody()
        {
            var testCode = @"
namespace N
{
    using System;

    public sealed class C
    {
        public TimeSpan Value => new TimeSpan(1);
    }
}";

            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void PropertyReturningBackingFieldExpressionBody()
        {
            var testCode = @"
namespace N
{
    using System;

    public sealed class C : IDisposable
    {
        private readonly IDisposable disposable = new Disposable();

        public IDisposable Value
        {
            get { return this.disposable; }
        }

        public void Dispose()
        {
            this.disposable.Dispose();
        }
    }
}";

            RoslynAssert.Valid(Analyzer, DisposableCode, testCode);
        }

        [Test]
        public static void PropertyReturningBackingField()
        {
            var testCode = @"
namespace N
{
    using System;

    public sealed class C : IDisposable
    {
        private readonly IDisposable disposable = new Disposable();

        public IDisposable Value => this.disposable;

        public void Dispose()
        {
            this.disposable.Dispose();
        }
    }
}";

            RoslynAssert.Valid(Analyzer, DisposableCode, testCode);
        }

        [Test]
        public static void PropertyReturningBackingFieldFunc()
        {
            var testCode = @"
namespace N
{
    using System;

    public sealed class C
    {
        private readonly Func<IDisposable> func;

        public C(Func<IDisposable> func)
        {
            this.func = func;
        }

        public IDisposable Disposable => this.func();
    }
}";

            RoslynAssert.Valid(Analyzer, DisposableCode, testCode);
        }
    }
}
