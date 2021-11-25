namespace IDisposableAnalyzers.Test.IDISP007DoNotDisposeInjectedTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    [TestFixture(typeof(DisposeCallAnalyzer))]
    [TestFixture(typeof(LocalDeclarationAnalyzer))]
    [TestFixture(typeof(UsingStatementAnalyzer))]
    public static class Valid<T>
        where T : DiagnosticAnalyzer, new()
    {
        private static readonly T Analyzer = new T();

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
        public static void DisposingArrayItem()
        {
            var code = @"
namespace N
{
    using System;

    public sealed class C : IDisposable
    {
        private readonly IDisposable[] disposables;

        public void M()
        {
            var disposable = disposables[0];
            disposable.Dispose();
        }

        public void Dispose()
        {
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void DisposingDictionaryItem()
        {
            var code = @"
namespace N
{
    using System;
    using System.Collections.Generic;

    public sealed class C : IDisposable
    {
        private readonly Dictionary<int, IDisposable> map = new Dictionary<int, IDisposable>();

        public void M()
        {
            var disposable = map[0];
            disposable.Dispose();
        }

        public void Dispose()
        {
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void DisposingWithBaseClass()
        {
            var baseClass = @"
namespace N
{
    using System;
    using System.IO;

    public abstract class BaseClass : IDisposable
    {
        private readonly Stream stream = File.OpenRead(string.Empty);
        private bool disposed = false;

        public void Dispose()
        {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;

            if (disposing)
            {
                this.stream.Dispose();
            }
        }
    }
}";

            var code = @"
namespace N
{
    using System;
    using System.IO;

    public class C : BaseClass
    {
        private readonly Stream stream = File.OpenRead(string.Empty);
        private bool disposed;

        protected override void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            if (disposing)
            {
                this.stream.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}";
            RoslynAssert.Valid(Analyzer, baseClass, code);
        }

        [Test]
        public static void DisposingInjectedPropertyInBaseClass()
        {
            var baseClass = @"
namespace N
{
    using System;

    public class BaseClass : IDisposable
    {
        private bool disposed = false;

        public BaseClass()
            : this(null)
        {
        }

        public BaseClass(object p)
        {
            this.P = p;
        }

        public object P { get; }

        public void Dispose()
        {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
        }
    }
}";

            var fooImplCode = @"
namespace N
{
    using System;
    using System.IO;

    public class C : BaseClass
    {
        public C(int no)
            : this(no.ToString())
        {
        }

        public C(string fileName)
            : base(File.OpenRead(fileName))
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                (this.P as IDisposable)?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}";
            RoslynAssert.Valid(Analyzer, baseClass, fooImplCode);
        }

        [Test]
        public static void DisposingInjectedPropertyInBaseClassFieldExpressionBody()
        {
            var baseClass = @"
namespace N
{
    using System;

    public class BaseClass : IDisposable
    {
        private readonly object p;
        private bool disposed = false;

        public BaseClass()
            : this(null)
        {
        }

        public BaseClass(object p)
        {
            this.p = p;
        }

        public object P => this.p;

        public void Dispose()
        {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
        }
    }
}";

            var fooImplCode = @"
namespace N
{
    using System;
    using System.IO;

    public class C : BaseClass
    {
        public C(int no)
            : this(no.ToString())
        {
        }

        public C(string fileName)
            : base(File.OpenRead(fileName))
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                (this.P as IDisposable)?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}";
            RoslynAssert.Valid(Analyzer, baseClass, fooImplCode);
        }

        [Test]
        public static void DisposingInjectedPropertyInBaseClassFieldExpressionBodyNotAssignedByChained()
        {
            var baseClass = @"
namespace N
{
    using System;

    public class BaseClass : IDisposable
    {
        private static IDisposable Empty = new Disposable();

        private readonly object p;
        private bool disposed = false;

        public BaseClass(string text)
        {
            this.p = Empty;
        }

        public BaseClass(object p)
        {
            this.p = p;
        }

        public object P => this.p;

        public void Dispose()
        {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
        }
    }
}";

            var fooImplCode = @"
namespace N
{
    using System;
    using System.IO;

    public class C : BaseClass
    {
        public C(string fileName)
            : base(File.OpenRead(fileName))
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                (this.P as IDisposable)?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}";
            RoslynAssert.Valid(Analyzer, DisposableCode, baseClass, fooImplCode);
        }

        [Test]
        public static void InjectedInClassThatIsNotIDisposable()
        {
            var code = @"
namespace N
{
    using System;

    public sealed class C
    {
        private readonly IDisposable disposable;

        public C(IDisposable disposable)
        {
            this.disposable = disposable;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void InjectedInClassThatIsIDisposable()
        {
            var code = @"
namespace N
{
    using System;

    public sealed class C : IDisposable
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
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void InjectedInClassThatIsIDisposableManyCtors()
        {
            var code = @"
namespace N
{
    using System;

    public sealed class C : IDisposable
    {
        private readonly IDisposable disposable;

        public C(IDisposable disposable)
            : this(disposable, string.Empty)
        {
        }

        public C(IDisposable disposable1, IDisposable disposable2, int n)
            : this(disposable1, n)
        {
        }

        private C(IDisposable disposable, int n)
            : this(disposable, n.ToString())
        {
        }

        private C(IDisposable disposable, string n)
        {
            this.disposable = disposable;
        }

        public void Dispose()
        {
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void InjectedObjectInClassThatIsIDisposableWhenTouchingInjectedInDisposeMethod()
        {
            var code = @"
namespace N
{
    using System;

    public sealed class C : IDisposable
    {
        private object f;

        public C(object f)
        {
            this.f = f;
        }

        public void Dispose()
        {
            f = null;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void NotDisposingFieldInVirtualDispose()
        {
            var code = @"
namespace N
{
    using System;
    using System.IO;

    public class C : IDisposable
    {
        private readonly IDisposable disposable;
        private bool disposed;

        public C(IDisposable disposable)
        {
            this.disposable = disposable;
        }

        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
        }

        protected void ThrowIfDisposed()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void InjectingIntoPrivateCtor()
        {
            var disposableCode = @"
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

            var code = @"
namespace N
{
    using System;

    public sealed class C : IDisposable
    {
        private readonly IDisposable disposable;

        public C()
            : this(new Disposable())
        {
        }

        private C(IDisposable disposable)
        {
            this.disposable = disposable;
        }

        public void Dispose()
        {
            this.disposable.Dispose();
        }
    }
}";
            RoslynAssert.Valid(Analyzer, disposableCode, code);
        }

        [Test]
        public static void BoolProperty()
        {
            var code = @"
namespace N
{
    using System;
    using System.ComponentModel;

    public sealed class C : IDisposable
    {
        private static readonly PropertyChangedEventArgs IsDirtyPropertyChangedEventArgs = new PropertyChangedEventArgs(nameof(IsDirty));
        private bool isDirty;

        public C()
        {
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public bool IsDirty
        {
            get
            {
                return this.isDirty;
            }

            private set
            {
                if (value == this.isDirty)
                {
                    return;
                }

                this.isDirty = value;
                this.PropertyChanged?.Invoke(this, IsDirtyPropertyChangedEventArgs);
            }
        }

        public void Dispose()
        {
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void InjectedInMethod()
        {
            var code = @"
namespace N
{
    using System;

    public sealed class C : IDisposable
    {
        private bool isDirty;

        public C()
        {
        }

        public void M(IDisposable disposable)
        {
        }

        public void Dispose()
        {
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void IgnoreLambdaCreation()
        {
            var code = @"
namespace N
{
    using System;

    public class C
    {
        public C()
        {
            Action<IDisposable> action = x => x.Dispose();
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Ignore("Dunno about this.")]
        [TestCase("action(stream)")]
        [TestCase("action.Invoke(stream)")]
        public static void IgnoreLambdaUsageOnLocal(string invokeCode)
        {
            var code = @"
namespace N
{
    using System;
    using System.IO;

    public class C
    {
        public C()
        {
            Action<IDisposable> action = x => x.Dispose();
            var stream = File.OpenRead(string.Empty);
            action(stream);
        }
    }
}".AssertReplace("action(stream)", invokeCode);
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void IgnoreInLambdaMethod()
        {
            var code = @"
namespace N
{
    using System;

    public class C
    {
        public C()
        {
            M(x => x.Dispose());
        }

        public static void M(Action<IDisposable> action)
        {
            action(null);
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void ReassignedParameter()
        {
            var code = @"
namespace N
{
    using System;
    using System.IO;

    internal class C
    {
        public static void ReassignParameter(IDisposable disposable)
        {
            disposable = File.OpenRead(string.Empty);
            disposable.Dispose();
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void ReassignedParameterViaOut()
        {
            var code = @"
namespace N
{
    using System;
    using System.IO;

    internal class C
    {
        public static void ReassignParameter(IDisposable disposable)
        {
            if (TryReassign(disposable, out disposable))
            {
                disposable.Dispose();
            }
        }

        private static bool TryReassign(IDisposable old, out IDisposable result)
        {
            result = File.OpenRead(string.Empty);
            return true;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void WhenAssumingCreated()
        {
            var binaryReferencedCode = @"
namespace BinaryReferencedAssembly
{
    using System;

    public class Disposable : IDisposable
    {
        public void Dispose()
        {
        }
    }

    public class Factory
    {
        public IDisposable Create() => new Disposable();
    }
}";
            var binaryReference = BinaryReference.Compile(binaryReferencedCode);

            var code = @"
namespace N
{
    using BinaryReferencedAssembly;

    public class C
    {
        private Factory factory;

        public C(Factory factory)
        {
            this.factory = factory;
        }

        public void M()
        {
            this.factory.Create().Dispose();
            this.factory.Create()?.Dispose();
        }
    }
}";

            var solution = CodeFactory.CreateSolution(
                code,
                CodeFactory.DefaultCompilationOptions(new[] { Analyzer })
                           .WithMetadataImportOptions(MetadataImportOptions.Public),
                MetadataReferences.FromAttributes().Add(binaryReference));

            RoslynAssert.Valid(Analyzer, solution);
        }

        [Test]
        public static void FactoryChainedReturned()
        {
            var disposable = @"
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

            var factory = @"
namespace N
{
    using System;
    using Gu.Inject;

    public static class Factory
    {
        public static Kernel CreateKernel()
        {
            return Create().BindDisposable();
        }

        private static Kernel BindDisposable(this Kernel kernel1)
        {
            return kernel1.Bind<IDisposable, Disposable>();
        }

        private static Kernel Create()
        {
            var kernel2 = new Kernel();
            kernel2.Creating += OnCreating;
            kernel2.Created += OnCreated;
            return kernel2;
        }

        private static void OnCreating(object sender, CreatingEventArgs e)
        {
        }

        private static void OnCreated(object sender, CreatedEventArgs e)
        {
        }
    }
}";

            var code = @"
namespace N
{
    using Gu.Inject;
    using NUnit.Framework;

    [TestFixture]
    public class Tests
    {
        private Kernel _container;

        [SetUp]
        public void SetUp()
        {
            _container = Factory.CreateKernel();
        }

        [TearDown]
        public void TearDown()
        {
            _container.Dispose();
        }
    }
}";
            RoslynAssert.Valid(Analyzer, disposable, factory, code);
        }

        [Test]
        public static void NewChainedReturned()
        {
            var factory = @"
namespace N
{
    using System;
    using Gu.Inject;

    public static class Factory
    {
        public static Kernel CreateKernel()
        {
            return new Kernel().Id();
        }

        private static Kernel Id(this Kernel kernel) => kernel;
    }
}";

            var code = @"
namespace N
{
    using Gu.Inject;
    using NUnit.Framework;

    [TestFixture]
    public class Tests
    {
        private Kernel _container;

        [SetUp]
        public void SetUp()
        {
            _container = Factory.CreateKernel();
        }

        [TearDown]
        public void TearDown()
        {
            _container.Dispose();
        }
    }
}";
            RoslynAssert.Valid(Analyzer, factory, code);
        }

        [Test]
        public static void NewChainedLocalReturned()
        {
            var factory = @"
namespace N
{
    using System;
    using Gu.Inject;

    public static class Factory
    {
        public static Kernel CreateKernel()
        {
            var kernel = new Kernel().Id();
            return kernel;
        }

        private static Kernel Id(this Kernel k) => k;
    }
}";

            var code = @"
namespace N
{
    using Gu.Inject;
    using NUnit.Framework;

    [TestFixture]
    public class Tests
    {
        private Kernel _container;

        [SetUp]
        public void SetUp()
        {
            _container = Factory.CreateKernel();
        }

        [TearDown]
        public void TearDown()
        {
            _container.Dispose();
        }
    }
}";
            RoslynAssert.Valid(Analyzer, factory, code);
        }

        [Test]
        public static void FactoryChainedLocalReturned()
        {
            var disposable = @"
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

            var factory = @"
namespace N
{
    using System;
    using Gu.Inject;

    public static class Factory
    {
        public static Kernel CreateKernel()
        {
            var kernel = Create().BindDisposable();
            return kernel;
        }

        private static Kernel BindDisposable(this Kernel kernel1)
        {
            kernel1.Bind<IDisposable, Disposable>();
            return kernel1;
        }

        private static Kernel Create()
        {
            var kernel2 = new Kernel();
            kernel2.Creating += OnCreating;
            kernel2.Created += OnCreated;
            return kernel2;
        }

        private static void OnCreating(object sender, CreatingEventArgs e)
        {
        }

        private static void OnCreated(object sender, CreatedEventArgs e)
        {
        }
    }
}";

            var code = @"
namespace N
{
    using Gu.Inject;
    using NUnit.Framework;

    [TestFixture]
    public class FixtureStackTests
    {
        private Kernel _container;

        [SetUp]
        public void SetUp()
        {
            _container = Factory.CreateKernel();
        }

        [TearDown]
        public void TearDown()
        {
            _container.Dispose();
        }
    }
}";
            RoslynAssert.Valid(Analyzer, disposable, factory, code);
        }
    }
}
