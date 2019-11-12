namespace IDisposableAnalyzers.Test.IDISP003DisposeBeforeReassigningTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public static partial class CodeFix
    {
        private static readonly DiagnosticAnalyzer Analyzer = new AssignmentAnalyzer();
        private static readonly DisposeBeforeAssignFix Fix = new DisposeBeforeAssignFix();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(IDISP003DisposeBeforeReassigning.Descriptor);

        private const string Disposable = @"
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

        private const string ExplicitDisposable = @"
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

        [Test]
        public static void LocalAssignedTwice()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class C
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

    public class C
    {
        public void Meh()
        {
            var stream = File.OpenRead(string.Empty);
            stream?.Dispose();
            stream = File.OpenRead(string.Empty);
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }

        [Test]
        public static void LocalOfTypeObjectAssignedTwice()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class C
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

    public class C
    {
        public void Meh()
        {
            object stream = File.OpenRead(string.Empty);
            (stream as System.IDisposable)?.Dispose();
            stream = File.OpenRead(string.Empty);
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }

        [Test]
        public static void LocalAssignedAndThenAssignedWithNull()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class C
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

    public class C
    {
        public void Meh()
        {
            var stream = File.OpenRead(string.Empty);
            stream?.Dispose();
            stream = null;
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }

        [Test]
        public static void LocalAssignedTwiceInsideIf()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class C
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

    public class C
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
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }

        [Test]
        public static void LocalAssignedInElse()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class C
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

    public class C
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
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }

        [Test]
        public static void LocalAssignedWithOutThenSimple()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class C
    {
        public void M()
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

    public class C
    {
        public void M()
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
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }

        [Test]
        public static void LocalInLambdaClosure()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class C
    {
        public C()
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

    public class C
    {
        public C()
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
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { Disposable, testCode }, fixedCode);
            RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { Disposable, testCode }, fixedCode);
        }

        [Test]
        public static void LocalInitializedBeforeWhileLoop()
        {
            var code = @"
namespace RoslynSandbox
{
    using System.IO;

    public class C
    {
        public C(int i)
        {
            Stream stream = File.OpenRead(string.Empty);
            while (i > 0)
            {
                ↓stream = File.OpenRead(string.Empty);
                i--;
            }

            stream.Dispose();
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class C
    {
        public C(int i)
        {
            Stream stream = File.OpenRead(string.Empty);
            while (i > 0)
            {
                stream?.Dispose();
                stream = File.OpenRead(string.Empty);
                i--;
            }

            stream.Dispose();
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, code, fixedCode);
        }

        [Test]
        public static void LocalInitializedWithNullBeforeWhileLoop()
        {
            var code = @"
namespace RoslynSandbox
{
    using System.IO;

    public class C
    {
        public C(int i)
        {
            Stream stream = null;
            while (i > 0)
            {
                ↓stream = File.OpenRead(string.Empty);
                i--;
            }

            stream.Dispose();
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class C
    {
        public C(int i)
        {
            Stream stream = null;
            while (i > 0)
            {
                stream?.Dispose();
                stream = File.OpenRead(string.Empty);
                i--;
            }

            stream.Dispose();
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, code, fixedCode);
        }

        [Test]
        public static void ParameterAssignedTwice()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class C
    {
        public void M(Stream stream)
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

    public class C
    {
        public void M(Stream stream)
        {
            stream = File.OpenRead(string.Empty);
            stream?.Dispose();
            stream = File.OpenRead(string.Empty);
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }

        [Test]
        public static void FieldInitializedThenAssignedInCtor()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class C
    {
        private readonly Stream stream = File.OpenRead(string.Empty);

        public C()
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

    public class C
    {
        private readonly Stream stream = File.OpenRead(string.Empty);

        public C()
        {
            this.stream?.Dispose();
            this.stream = File.OpenRead(string.Empty);
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }

        [Test]
        public static void FieldOfTypeObjectInitializedThenAssignedInConstructor()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class C
    {
        private readonly object stream = File.OpenRead(string.Empty);

        public C()
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

    public class C
    {
        private readonly object stream = File.OpenRead(string.Empty);

        public C()
        {
            (this.stream as IDisposable)?.Dispose();
            this.stream = File.OpenRead(string.Empty);
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }

        [Test]
        public static void PropertyInitializedAndAssignedInConstructor()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class C
    {
        public C()
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

    public class C
    {
        public C()
        {
            this.Stream?.Dispose();
            this.Stream = File.OpenRead(string.Empty);
        }

        public Stream Stream { get; } = File.OpenRead(string.Empty);
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }

        [Test]
        public static void PropertyWithBackingFieldInitializedThenAssignedInConstructor()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class C
    {
        private Stream stream = File.OpenRead(string.Empty);

        public C()
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

    public class C
    {
        private Stream stream = File.OpenRead(string.Empty);

        public C()
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
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }

        [Test]
        public static void PropertyWithBackingFieldAssignedTwiceInConstructor()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class C
    {
        private Stream stream;

        public C()
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

    public class C
    {
        private Stream stream;

        public C()
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
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }

        [Test]
        public static void FieldAssignedInPublicMethod()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class C
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

    public class C
    {
        private Stream stream;

        public void Meh()
        {
            this.stream?.Dispose();
            this.stream = File.OpenRead(string.Empty);
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }

        [Test]
        public static void FieldAssignedInPublicMethodReturnExpression()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class C
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

    public class C
    {
        private Stream stream;

        public IDisposable Meh()
        {
            this.stream?.Dispose();
            return this.stream = File.OpenRead(string.Empty);
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }

        [Test]
        public static void FieldAssignedInPublicMethodExpressionBody()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class C
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

    public class C
    {
        private Stream stream;

        public IDisposable Meh()
        {
            this.stream?.Dispose();
            return this.stream = File.OpenRead(string.Empty);
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }

        [Test]
        public static void FieldAssignedInLambda()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class C
    {
        private Stream stream;

        public C()
        {
            this.M += (o, e) => ↓this.stream = File.OpenRead(string.Empty);
        }

        public event EventHandler M;
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class C
    {
        private Stream stream;

        public C()
        {
            this.M += (o, e) =>
            {
                this.stream?.Dispose();
                this.stream = File.OpenRead(string.Empty);
            };
        }

        public event EventHandler M;
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }

        [Test]
        public static void FieldAssignedInLambdaBlock()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class C : IDisposable
    {
        private readonly IDisposable subscription;
        private Disposable disposable;

        public C(IObservable<object> observable)
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

    public sealed class C : IDisposable
    {
        private readonly IDisposable subscription;
        private Disposable disposable;

        public C(IObservable<object> observable)
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
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { Disposable, testCode }, fixedCode);
            RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { Disposable, testCode }, fixedCode);
        }

        [Test]
        public static void FieldAssignedInGetterReturnExpression()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class C
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

    public class C
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
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }

        [Test]
        public static void FieldAssignedInPropertyExpressionBody()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class C
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

    public class C
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

            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }

        [Test]
        public static void FieldAssignedInLambdaArgument()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class C : IDisposable
    {
        private readonly IDisposable subscription;
        private Disposable disposable;

        public C(IObservable<object> observable)
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

    public sealed class C : IDisposable
    {
        private readonly IDisposable subscription;
        private Disposable disposable;

        public C(IObservable<object> observable)
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
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { Disposable, testCode }, fixedCode);
            RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { Disposable, testCode }, fixedCode);
        }

        [Test]
        public static void FieldAssignedInLambdaArgumentBlock()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class C : IDisposable
    {
        private readonly IDisposable subscription;
        private IDisposable disposable;

        public C(IObservable<object> observable)
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

    public sealed class C : IDisposable
    {
        private readonly IDisposable subscription;
        private IDisposable disposable;

        public C(IObservable<object> observable)
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
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { Disposable, testCode }, fixedCode);
            RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { Disposable, testCode }, fixedCode);
        }
    }
}
