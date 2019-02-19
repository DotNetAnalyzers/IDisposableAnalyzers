namespace IDisposableAnalyzers.Test.IDISP004DontIgnoreCreatedTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public partial class Diagnostics
    {
        public class ObjectCreation
        {
            private static readonly DiagnosticAnalyzer Analyzer = new CreationAnalyzer();
            private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create("IDISP004");

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
            public void NewDisposable()
            {
                var testCode = @"
namespace RoslynSandbox
{
    public sealed class Foo
    {
        public void Meh()
        {
            ↓new Disposable();
        }
    }
}";
                AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, DisposableCode, testCode);
            }

            [Test]
            public void NewDisposablePassedIntoCtor()
            {
                var barCode = @"
namespace RoslynSandbox
{
    using System;

    public class Bar
    {
        private readonly IDisposable disposable;

        public Bar(IDisposable disposable)
        {
           this.disposable = disposable;
        }
    }
}";

                var testCode = @"
namespace RoslynSandbox
{
    public sealed class Foo
    {
        public Bar Meh()
        {
            return new Bar(↓new Disposable());
        }
    }
}";
                AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, DisposableCode, barCode, testCode);
            }

            [Test]
            public void ReturningNewAssigningNotDisposing()
            {
                var fooCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo : IDisposable
    {
        private readonly IDisposable disposable;

        public Foo(IDisposable disposable)
        {
            this.disposable = disposable;
        }

        public void Dispose()
        {
        }
    }
}";
                var testCode = @"
namespace RoslynSandbox
{
    public class Meh
    {
        public Foo Bar()
        {
            return new Foo(↓new Disposable());
        }
    }
}";
                AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, DisposableCode, fooCode, testCode);
            }

            [Test]
            public void ReturningNewNotAssigning()
            {
                var fooCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo : IDisposable
    {
        public Foo(IDisposable disposable)
        {
        }

        public void Dispose()
        {
        }
    }
}";
                var testCode = @"
namespace RoslynSandbox
{
    public class Meh
    {
        public Foo Bar()
        {
            return new Foo(↓new Disposable());
        }
    }
}";
                AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, DisposableCode, fooCode, testCode);
            }

            [Test]
            public void StringFormatArgument()
            {
                var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public void Bar()
        {
            string.Format(""{0}"", ↓new Disposable());
        }
    }
}";
                AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, DisposableCode, testCode);
            }

            [Test]
            public void NewDisposableToString()
            {
                var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public Foo()
        {
            var text = ↓new Disposable().ToString();
        }
    }
}";
                AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, DisposableCode, testCode);
            }

            [Test]
            public void ReturnNewDisposableToString()
            {
                var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public static string Bar()
        {
            return ↓new Disposable().ToString();
        }
    }
}";
                AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, DisposableCode, testCode);
            }

            [Test]
            public void AddingNewDisposableToListOfObject()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections.Generic;

    public sealed class Foo
    {
        private List<object> disposables = new List<object>();

        public Foo()
        {
            this.disposables.Add(↓new Disposable());
        }
    }
}";
                AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, DisposableCode, testCode);
            }
        }
    }
}
