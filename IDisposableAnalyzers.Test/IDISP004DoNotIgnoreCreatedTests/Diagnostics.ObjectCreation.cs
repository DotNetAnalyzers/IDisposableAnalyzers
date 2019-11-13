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
                var code = @"
namespace N
{
    public sealed class C
    {
        public void M()
        {
            ↓new Disposable();
        }
    }
}";
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, DisposableCode, code);
            }

            [Test]
            public static void NewDisposablePassedIntoCtor()
            {
                var c1 = @"
namespace N
{
    using System;

    public class C1
    {
        private readonly IDisposable disposable;

        public C1(IDisposable disposable)
        {
           this.disposable = disposable;
        }
    }
}";

                var code = @"
namespace N
{
    public sealed class C
    {
        public C1 M()
        {
            return new C1(↓new Disposable());
        }
    }
}";
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, DisposableCode, c1, code);
            }

            [Test]
            public static void ReturningNewAssigningNotDisposing()
            {
                var c1 = @"
namespace N
{
    using System;

    public class C1 : IDisposable
    {
        private readonly IDisposable disposable;

        public C1(IDisposable disposable)
        {
            this.disposable = disposable;
        }

        public void Dispose()
        {
        }
    }
}";
                var code = @"
namespace N
{
    public class C
    {
        public C1 M()
        {
            return new C1(↓new Disposable());
        }
    }
}";
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, DisposableCode, c1, code);
            }

            [Test]
            public static void ReturningNewNotAssigning()
            {
                var c1 = @"
namespace N
{
    using System;

    public class C1 : IDisposable
    {
        public C1(IDisposable disposable)
        {
        }

        public void Dispose()
        {
        }
    }
}";
                var code = @"
namespace N
{
    public class C
    {
        public C1 M()
        {
            return new C1(↓new Disposable());
        }
    }
}";
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, DisposableCode, c1, code);
            }

            [Test]
            public static void StringFormatArgument()
            {
                var code = @"
namespace N
{
    public static class C
    {
        public static string M() => string.Format(""{0}"", ↓new Disposable());
    }
}";
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, DisposableCode, code);
            }

            [Test]
            public static void NewDisposableToString()
            {
                var code = @"
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
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, DisposableCode, code);
            }

            [Test]
            public static void ReturnNewDisposableToString()
            {
                var code = @"
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
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, DisposableCode, code);
            }

            [Test]
            public static void NewStandardKernelNewModuleArgument()
            {
                var module = @"
namespace N
{
    using System;
    using Ninject.Modules;

    public class Module : NinjectModule
    {
        public override void Load()
        {
            throw new NotImplementedException();
        }
    }
}";

                var code = @"
namespace N
{
    using Ninject;

    public sealed class C
    {
        public C()
        {
            using (new StandardKernel(↓new Module()))
            {
            }
        }
    }
}";
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, module, code);
            }
        }
    }
}
