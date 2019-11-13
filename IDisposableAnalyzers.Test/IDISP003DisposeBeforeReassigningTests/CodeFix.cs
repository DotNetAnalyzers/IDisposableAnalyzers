namespace IDisposableAnalyzers.Test.IDISP003DisposeBeforeReassigningTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public static partial class CodeFix
    {
        private static readonly DiagnosticAnalyzer Analyzer = new AssignmentAnalyzer();
        private static readonly DisposeBeforeAssignFix Fix = new DisposeBeforeAssignFix();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.IDISP003DisposeBeforeReassigning);

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

        private const string ExplicitDisposable = @"
namespace N
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
            var before = @"
namespace N
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
            var after = @"
namespace N
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
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
            RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void LocalOfTypeObjectAssignedTwice()
        {
            var before = @"
namespace N
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

            var after = @"
namespace N
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
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
            RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void LocalAssignedAndThenAssignedWithNull()
        {
            var before = @"
namespace N
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

            var after = @"
namespace N
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
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
            RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void LocalAssignedTwiceInsideIf()
        {
            var before = @"
namespace N
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

            var after = @"
namespace N
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
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
            RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void LocalAssignedInElse()
        {
            var before = @"
namespace N
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

            var after = @"
namespace N
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
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
            RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void LocalAssignedWithOutThenSimple()
        {
            var before = @"
namespace N
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

            var after = @"
namespace N
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
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
            RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void LocalInLambdaClosure()
        {
            var before = @"
namespace N
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

            var after = @"
namespace N
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
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { Disposable, before }, after);
            RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { Disposable, before }, after);
        }

        [Test]
        public static void LocalInitializedBeforeWhileLoop()
        {
            var before = @"
namespace N
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

            var after = @"
namespace N
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
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void LocalInitializedWithNullBeforeWhileLoop()
        {
            var before = @"
namespace N
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

            var after = @"
namespace N
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
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void ParameterAssignedTwice()
        {
            var before = @"
namespace N
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

            var after = @"
namespace N
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
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
            RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void FieldInitializedThenAssignedInCtor()
        {
            var before = @"
namespace N
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

            var after = @"
namespace N
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
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
            RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void FieldOfTypeObjectInitializedThenAssignedInConstructor()
        {
            var before = @"
namespace N
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

            var after = @"
namespace N
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
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
            RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void PropertyInitializedAndAssignedInConstructor()
        {
            var before = @"
namespace N
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

            var after = @"
namespace N
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
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
            RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void PropertyWithBackingFieldInitializedThenAssignedInConstructor()
        {
            var before = @"
namespace N
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

            var after = @"
namespace N
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
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
            RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void PropertyWithBackingFieldAssignedTwiceInConstructor()
        {
            var before = @"
namespace N
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

            var after = @"
namespace N
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
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
            RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void FieldAssignedInPublicMethod()
        {
            var before = @"
namespace N
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

            var after = @"
namespace N
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
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
            RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void FieldAssignedInPublicMethodReturnExpression()
        {
            var before = @"
namespace N
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

            var after = @"
namespace N
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
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
            RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void FieldAssignedInPublicMethodExpressionBody()
        {
            var before = @"
namespace N
{
    using System;
    using System.IO;

    public class C
    {
        private Stream stream;

        public IDisposable Meh() => ↓this.stream = File.OpenRead(string.Empty);
    }
}";

            var after = @"
namespace N
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
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
            RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void FieldAssignedInLambda()
        {
            var before = @"
namespace N
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

            var after = @"
namespace N
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
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
            RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void FieldAssignedInLambdaBlock()
        {
            var before = @"
namespace N
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

            var after = @"
namespace N
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
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { Disposable, before }, after);
            RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { Disposable, before }, after);
        }

        [Test]
        public static void FieldAssignedInGetterReturnExpression()
        {
            var before = @"
namespace N
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

            var after = @"
namespace N
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
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
            RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void FieldAssignedInPropertyExpressionBody()
        {
            var before = @"
namespace N
{
    using System;
    using System.IO;

    public class C
    {
        private Stream stream;

        public IDisposable Meh => ↓this.stream = File.OpenRead(string.Empty);
    }
}";

            var after = @"
namespace N
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

            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void FieldAssignedInLambdaArgument()
        {
            var before = @"
namespace N
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

            var after = @"
namespace N
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
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { Disposable, before }, after);
            RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { Disposable, before }, after);
        }

        [Test]
        public static void FieldAssignedInLambdaArgumentBlock()
        {
            var before = @"
namespace N
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
            var after = @"
namespace N
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
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { Disposable, before }, after);
            RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { Disposable, before }, after);
        }
    }
}
