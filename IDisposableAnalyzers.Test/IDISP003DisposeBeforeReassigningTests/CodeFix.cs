namespace IDisposableAnalyzers.Test.IDISP003DisposeBeforeReassigningTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    internal partial class CodeFix
    {
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

        private const string ExplicitDisposableCode = @"
namespace RoslynSandbox
{
    using System;

    public class ExplicitDisposable : IDisposable
    {
        void IDisposable.Dispose()
        {
        }
    }
}";

        private static readonly DiagnosticAnalyzer Analyzer = new AssignmentAnalyzer();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create("IDISP003");
        private static readonly DisposeBeforeAssignCodeFixProvider Fix = new DisposeBeforeAssignCodeFixProvider();

        [Test]
        public void NotDisposingVariable()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        public void Meh()
        {
            var stream = File.OpenRead(string.Empty);
            ↓stream = File.OpenRead(string.Empty);
        }
    }
}";

            // keeping it safe and doing ?.Dispose()
            // will require some work to figure out if it can be null
            var fixedCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        public void Meh()
        {
            var stream = File.OpenRead(string.Empty);
            stream?.Dispose();
            stream = File.OpenRead(string.Empty);
        }
    }
}";
            AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }

        [Test]
        public void SettingToNull()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        public void Meh()
        {
            var stream = File.OpenRead(string.Empty);
            ↓stream = null;
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        public void Meh()
        {
            var stream = File.OpenRead(string.Empty);
            stream?.Dispose();
            stream = null;
        }
    }
}";
            AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }

        [Test]
        public void WhenNullCheckAndAssignedTwice()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        public void Meh()
        {
            Stream stream = null;
            if (stream == null)
            {
                stream = File.OpenRead(string.Empty);
                ↓stream = File.OpenRead(string.Empty);
            }
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        public void Meh()
        {
            Stream stream = null;
            if (stream == null)
            {
                stream = File.OpenRead(string.Empty);
                stream?.Dispose();
                stream = File.OpenRead(string.Empty);
            }
        }
    }
}";
            AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }

        [Test]
        public void NotDisposingVariableOfTypeObject()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        public void Meh()
        {
            object stream = File.OpenRead(string.Empty);
            ↓stream = File.OpenRead(string.Empty);
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        public void Meh()
        {
            object stream = File.OpenRead(string.Empty);
            (stream as System.IDisposable)?.Dispose();
            stream = File.OpenRead(string.Empty);
        }
    }
}";
            AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }

        [Test]
        public void AssigningParameterTwice()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        public void Bar(Stream stream)
        {
            stream = File.OpenRead(string.Empty);
            ↓stream = File.OpenRead(string.Empty);
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        public void Bar(Stream stream)
        {
            stream = File.OpenRead(string.Empty);
            stream?.Dispose();
            stream = File.OpenRead(string.Empty);
        }
    }
}";
            AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }

        [Test]
        public void AssigningInIfElse()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        public void Meh()
        {
            Stream stream = File.OpenRead(string.Empty);
            if (true)
            {
                stream.Dispose();
                stream = File.OpenRead(string.Empty);
            }
            else
            {
                ↓stream = File.OpenRead(string.Empty);
            }
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        public void Meh()
        {
            Stream stream = File.OpenRead(string.Empty);
            if (true)
            {
                stream.Dispose();
                stream = File.OpenRead(string.Empty);
            }
            else
            {
                stream?.Dispose();
                stream = File.OpenRead(string.Empty);
            }
        }
    }
}";
            AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }

        [Test]
        public void NotDisposingInitializedFieldInCtor()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        private readonly Stream stream = File.OpenRead(string.Empty);

        public Foo()
        {
            ↓this.stream = File.OpenRead(string.Empty);
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        private readonly Stream stream = File.OpenRead(string.Empty);

        public Foo()
        {
            this.stream?.Dispose();
            this.stream = File.OpenRead(string.Empty);
        }
    }
}";
            AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }

        [Test]
        public void NotDisposingInitializedPropertyInCtor()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        public Foo()
        {
            ↓this.Stream = File.OpenRead(string.Empty);
        }

        public Stream Stream { get; } = File.OpenRead(string.Empty);
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        public Foo()
        {
            this.Stream?.Dispose();
            this.Stream = File.OpenRead(string.Empty);
        }

        public Stream Stream { get; } = File.OpenRead(string.Empty);
    }
}";
            AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }

        [Test]
        public void NotDisposingInitializedBackingFieldInCtor()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        private Stream stream = File.OpenRead(string.Empty);

        public Foo()
        {
            ↓this.Stream = File.OpenRead(string.Empty);
        }

        public Stream Stream
        {
            get { return this.stream; }
            private set { this.stream = value; }
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        private Stream stream = File.OpenRead(string.Empty);

        public Foo()
        {
            this.stream?.Dispose();
            this.Stream = File.OpenRead(string.Empty);
        }

        public Stream Stream
        {
            get { return this.stream; }
            private set { this.stream = value; }
        }
    }
}";
            AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }

        [Test]
        public void NotDisposingBackingFieldInCtor()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        private Stream stream;

        public Foo()
        {
            this.Stream = File.OpenRead(string.Empty);
            ↓this.Stream = File.OpenRead(string.Empty);
        }

        public Stream Stream
        {
            get { return this.stream; }
            private set { this.stream = value; }
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        private Stream stream;

        public Foo()
        {
            this.Stream = File.OpenRead(string.Empty);
            this.stream?.Dispose();
            this.Stream = File.OpenRead(string.Empty);
        }

        public Stream Stream
        {
            get { return this.stream; }
            private set { this.stream = value; }
        }
    }
}";
            AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }

        [Test]
        public void NotDisposingFieldInMethod()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        private Stream stream;

        public void Meh()
        {
            ↓this.stream = File.OpenRead(string.Empty);
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        private Stream stream;

        public void Meh()
        {
            this.stream?.Dispose();
            this.stream = File.OpenRead(string.Empty);
        }
    }
}";
            AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }

        [Test]
        public void NotDisposingFieldInLambda()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        private Stream stream;

        public Foo()
        {
            this.Bar += (o, e) => ↓this.stream = File.OpenRead(string.Empty);
        }

        public event EventHandler Bar;
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        private Stream stream;

        public Foo()
        {
            this.Bar += (o, e) =>
            {
                this.stream?.Dispose();
                this.stream = File.OpenRead(string.Empty);
            };
        }

        public event EventHandler Bar;
    }
}";
            AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }

        [Test]
        public void NotDisposingFieldAssignedInReturnStatementMethodBody()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        private Stream stream;

        public IDisposable Meh()
        {
            return ↓this.stream = File.OpenRead(string.Empty);
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        private Stream stream;

        public IDisposable Meh()
        {
            this.stream?.Dispose();
            return this.stream = File.OpenRead(string.Empty);
        }
    }
}";
            AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }

        [Test]
        public void NotDisposingFieldAssignedInExpressionBody()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        private Stream stream;

        public IDisposable Meh() => ↓this.stream = File.OpenRead(string.Empty);
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        private Stream stream;

        public IDisposable Meh()
        {
            this.stream?.Dispose();
            return this.stream = File.OpenRead(string.Empty);
        }
    }
}";

            AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }

        [Test]
        public void NotDisposingFieldAssignedInReturnStatementInPropertyStatementBody()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        private Stream stream;

        public IDisposable Meh
        {
            get
            {
                return ↓this.stream = File.OpenRead(string.Empty);
            }
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        private Stream stream;

        public IDisposable Meh
        {
            get
            {
                this.stream?.Dispose();
                return this.stream = File.OpenRead(string.Empty);
            }
        }
    }
}";
            AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }

        [Test]
        public void NotDisposingFieldAssignedInReturnStatementInPropertyExpressionBody()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        private Stream stream;

        public IDisposable Meh => ↓this.stream = File.OpenRead(string.Empty);
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        private Stream stream;

        public IDisposable Meh
        {
            get
            {
                this.stream?.Dispose();
                return this.stream = File.OpenRead(string.Empty);
            }
        }
    }
}";

            AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }

        [Test]
        public void AssigningFieldInLambdaBlock()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo : IDisposable
    {
        private readonly IDisposable subscription;
        private Disposable disposable;

        public Foo(IObservable<object> observable)
        {
            this.subscription = observable.Subscribe(_ =>
            {
                ↓this.disposable = new Disposable();
            });
        }

        public void Dispose()
        {
            this.disposable?.Dispose();
            this.subscription?.Dispose();
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo : IDisposable
    {
        private readonly IDisposable subscription;
        private Disposable disposable;

        public Foo(IObservable<object> observable)
        {
            this.subscription = observable.Subscribe(_ =>
            {
                this.disposable?.Dispose();
                this.disposable = new Disposable();
            });
        }

        public void Dispose()
        {
            this.disposable?.Dispose();
            this.subscription?.Dispose();
        }
    }
}";
            AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { DisposableCode, testCode }, fixedCode);
            AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { DisposableCode, testCode }, fixedCode);
        }

        [Test]
        public void AssigningFieldInLambdaExpression()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo : IDisposable
    {
        private readonly IDisposable subscription;
        private Disposable disposable;

        public Foo(IObservable<object> observable)
        {
            this.subscription = observable.Subscribe(_ => ↓this.disposable = new Disposable());
        }

        public void Dispose()
        {
            this.disposable?.Dispose();
            this.subscription?.Dispose();
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo : IDisposable
    {
        private readonly IDisposable subscription;
        private Disposable disposable;

        public Foo(IObservable<object> observable)
        {
            this.subscription = observable.Subscribe(_ =>
            {
                this.disposable?.Dispose();
                this.disposable = new Disposable();
            });
        }

        public void Dispose()
        {
            this.disposable?.Dispose();
            this.subscription?.Dispose();
        }
    }
}";
            AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { DisposableCode, testCode }, fixedCode);
            AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { DisposableCode, testCode }, fixedCode);
        }

        [Test]
        public void AssigningBackingFieldInLambda()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo : IDisposable
    {
        private readonly IDisposable subscription;
        private IDisposable disposable;

        public Foo(IObservable<object> observable)
        {
            this.subscription = observable.Subscribe(_ =>
            {
                ↓this.Disposable = new Disposable();
            });
        }

        public IDisposable Disposable
        {
            get { return this.disposable; }
            private set { this.disposable = value; }
        }

        public void Dispose()
        {
            this.disposable?.Dispose();
            this.subscription?.Dispose();
        }
    }
}";
            var fixedCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo : IDisposable
    {
        private readonly IDisposable subscription;
        private IDisposable disposable;

        public Foo(IObservable<object> observable)
        {
            this.subscription = observable.Subscribe(_ =>
            {
                this.disposable?.Dispose();
                this.Disposable = new Disposable();
            });
        }

        public IDisposable Disposable
        {
            get { return this.disposable; }
            private set { this.disposable = value; }
        }

        public void Dispose()
        {
            this.disposable?.Dispose();
            this.subscription?.Dispose();
        }
    }
}";
            AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { DisposableCode, testCode }, fixedCode);
            AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { DisposableCode, testCode }, fixedCode);
        }

        [Test]
        public void AssigningVariableViaOutParameterBefore()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        public void Bar()
        {
            Stream stream;
            if (this.TryGetStream(out stream))
            {
                ↓stream = File.OpenRead(string.Empty);
            }
        }

        public bool TryGetStream(out Stream result)
        {
            result = File.OpenRead(string.Empty);
            return true;
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        public void Bar()
        {
            Stream stream;
            if (this.TryGetStream(out stream))
            {
                stream?.Dispose();
                stream = File.OpenRead(string.Empty);
            }
        }

        public bool TryGetStream(out Stream result)
        {
            result = File.OpenRead(string.Empty);
            return true;
        }
    }
}";
            AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }

        [Test]
        public void WhenAssigningLocalInLambda()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        public Foo()
        {
            Disposable disposable = null;
            Console.CancelKeyPress += (_, __) =>
            {
                ↓disposable = new Disposable();
            };
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        public Foo()
        {
            Disposable disposable = null;
            Console.CancelKeyPress += (_, __) =>
            {
                disposable?.Dispose();
                disposable = new Disposable();
            };
        }
    }
}";
            AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { DisposableCode, testCode }, fixedCode);
            AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { DisposableCode, testCode }, fixedCode);
        }
    }
}
