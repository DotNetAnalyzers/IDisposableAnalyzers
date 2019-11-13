namespace IDisposableAnalyzers.Test.IDISP004DoNotIgnoreCreatedTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public static partial class Diagnostics
    {
        public static class ObjectCreation
        {
            private static readonly DiagnosticAnalyzer Analyzer = new CreationAnalyzer();
            private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create("IDISP004");

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
            public static void NewDisposable()
            {
                var testCode = @"
namespace N
{
    public sealed class C
    {
        public void Meh()
        {
            ↓new Disposable();
        }
    }
}";
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, DisposableCode, testCode);
            }

            [Test]
            public static void NewDisposablePassedIntoCtor()
            {
                var barCode = @"
namespace N
{
    using System;

    public class M
    {
        private readonly IDisposable disposable;

        public M(IDisposable disposable)
        {
           this.disposable = disposable;
        }
    }
}";

                var testCode = @"
namespace N
{
    public sealed class C
    {
        public M Meh()
        {
            return new M(↓new Disposable());
        }
    }
}";
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, DisposableCode, barCode, testCode);
            }

            [Test]
            public static void ReturningNewAssigningNotDisposing()
            {
                var fooCode = @"
namespace N
{
    using System;

    public class C : IDisposable
    {
        private readonly IDisposable disposable;

        public C(IDisposable disposable)
        {
            this.disposable = disposable;
        }

        public void Dispose()
        {
        }
    }
}";
                var testCode = @"
namespace N
{
    public class Meh
    {
        public C M()
        {
            return new C(↓new Disposable());
        }
    }
}";
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, DisposableCode, fooCode, testCode);
            }

            [Test]
            public static void ReturningNewNotAssigning()
            {
                var fooCode = @"
namespace N
{
    using System;

    public class C : IDisposable
    {
        public C(IDisposable disposable)
        {
        }

        public void Dispose()
        {
        }
    }
}";
                var testCode = @"
namespace N
{
    public class Meh
    {
        public C M()
        {
            return new C(↓new Disposable());
        }
    }
}";
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, DisposableCode, fooCode, testCode);
            }

            [Test]
            public static void StringFormatArgument()
            {
                var testCode = @"
namespace N
{
    public static class C
    {
        public static string M() => string.Format(""{0}"", ↓new Disposable());
    }
}";
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, DisposableCode, testCode);
            }

            [Test]
            public static void NewDisposableToString()
            {
                var testCode = @"
namespace N
{
    public class C
    {
        public C()
        {
            var text = ↓new Disposable().ToString();
        }
    }
}";
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, DisposableCode, testCode);
            }

            [Test]
            public static void ReturnNewDisposableToString()
            {
                var testCode = @"
namespace N
{
    public class C
    {
        public static string M()
        {
            return ↓new Disposable().ToString();
        }
    }
}";
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, DisposableCode, testCode);
            }

            [Test]
            public static void NewStandardKernelNewModuleArgument()
            {
                var moduleCode = @"
namespace N
{
    using System;
    using Ninject.Modules;

    public class CModule : NinjectModule
    {
        public override void Load()
        {
            throw new NotImplementedException();
        }
    }
}";

                var testCode = @"
namespace N
{
    using Ninject;

    public sealed class C
    {
        public C()
        {
            using (new StandardKernel(↓new CModule()))
            {
            }
        }
    }
}";
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, moduleCode, testCode);
            }
        }
    }
}
