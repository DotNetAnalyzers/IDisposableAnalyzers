namespace IDisposableAnalyzers.Test.IDISP004DoNotIgnoreCreatedTests
{
    using Gu.Roslyn.Asserts;

    using NUnit.Framework;

    public static partial class Valid
    {
        [Test]
        public static void Generic()
        {
            var factory = @"
namespace N
{
    public class Factory
    {
        public static T Create<T>() where T : new() => new T();
    }
}";

            var code = @"
namespace N
{
    public class C
    {
        public void M()
        {
            Factory.Create<int>();
        }
    }
}";
            RoslynAssert.Valid(Analyzer, factory, code);
        }

        [Test]
        public static void Operator()
        {
            var c1 = @"
namespace N
{
    public class C1
    {
        public static C1 operator +(C1 left, C1 right) => new C1();
    }
}";

            var code = @"
namespace N
{
    public class C
    {
        public object M()
        {
            var meh1 = new C1();
            var meh2 = new C1();
            return meh1 + meh2;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, c1, code);
        }

        [Test]
        public static void OperatorNestedCall()
        {
            var c1 = @"
namespace N
{
    public class C1
    {
        public static C1 operator +(C1 left, C1 right) => new C1();
    }
}";

            var code = @"
namespace N
{
    public class C
    {
        public object M()
        {
            var meh1 = new C1();
            var meh2 = new C1();
            return Add(new C1(), new C1());
        }

        public object Add(C1 meh1, C1 meh2)
        {
            return meh1 + meh2;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, c1, code);
        }

        [Test]
        public static void OperatorEquals()
        {
            var c1 = @"
namespace N
{
    public class C1
    {
    }
}";

            var code = @"
namespace N
{
    public class C
    {
        public bool M()
        {
            var meh1 = new C1();
            var meh2 = new C1();
            return meh1 == meh2;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, c1, code);
        }

        [Test]
        public static void MethodReturningObject()
        {
            var code = @"
namespace N
{
    public class C
    {
        public void M1()
        {
            M2();
        }

        private static object M2() => new object();
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void MethodWithArgReturningObject()
        {
            var code = @"
namespace N
{
    public class C
    {
        public void M1()
        {
            M2(""M2"");
        }

        private static object M2(string arg) => new object();
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void MethodWithObjArgReturningObject()
        {
            var code = @"
namespace N
{
    public class C
    {
        public void M()
        {
            Id(new C());
        }

        private static object Id(object arg) => arg;
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void ReturningStatementBody()
        {
            var code = @"
namespace N
{
    using System.IO;

    public class C
    {
        public Stream M()
        {
            return File.OpenRead(string.Empty);
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void ReturningLocalStatementBody()
        {
            var code = @"
namespace N
{
    using System.IO;

    public class C
    {
        public Stream M()
        {
            var stream = File.OpenRead(string.Empty);
            return stream;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void ReturningExpressionBody()
        {
            var code = @"
namespace N
{
    using System.IO;

    public class C
    {
        public Stream M() => File.OpenRead(string.Empty);
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void ReturningNewAssigningAndDisposing()
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
            this.disposable.Dispose();
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
            return new C1(new Disposable());
        }
    }
}";
            RoslynAssert.Valid(Analyzer, DisposableCode, c1, code);
        }

        [TestCase("new C1()")]
        [TestCase("new C1(new Disposable())")]
        [TestCase("new C1(new Disposable(), new Disposable())")]
        public static void ReturningNewAssigningAndDisposingParams(string objectCreation)
        {
            var c1 = @"
namespace N
{
    using System;

    public class C1 : IDisposable
    {
        private readonly IDisposable[] disposables;

        public C1(params IDisposable[] disposables)
        {
            this.disposables = disposables;
        }

        public void Dispose()
        {
            foreach (var disposable in this.disposables)
            {
                disposable.Dispose();
            }
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
            return new C1(new Disposable(), new Disposable());
        }
    }
}".AssertReplace("new C1(new Disposable(), new Disposable())", objectCreation);

            RoslynAssert.Valid(Analyzer, DisposableCode, c1, code);
        }

        [Test]
        public static void ReturningCreateNewAssigningAndDisposing()
        {
            var c1 = @"
namespace N
{
    using System;

    public sealed class C1 : IDisposable
    {
        private readonly IDisposable disposable;

        public C1(IDisposable disposable)
        {
            this.disposable = disposable;
        }

        public void Dispose()
        {
            this.disposable?.Dispose();
        }
    }
}";
            var code = @"
namespace N
{
    using System;

    public class C
    {
        public C1 M()
        {
            return Create(new Disposable());
        }

        private static C1 Create(IDisposable disposable) => new C1(disposable);
    }
}";
            RoslynAssert.Valid(Analyzer, DisposableCode, c1, code);
        }

        [Test]
        public static void ReturningCreateNewStreamReader()
        {
            var code = @"
namespace N
{
    using System.IO;

    public class C
    {
        public StreamReader M()
        {
            return Create(File.OpenRead(string.Empty));
        }

        private static StreamReader Create(Stream stream) => new StreamReader(stream);
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void ReturningAssigningPrivateChained()
        {
            var c1 = @"
namespace N
{
    using System;

    public class C1 : IDisposable
    {
        private readonly IDisposable disposable;

        public C1(int value, IDisposable disposable)
            : this(disposable)
        {
        }

        private C1(IDisposable disposable)
        {
            this.disposable = disposable;
        }

        public void Dispose()
        {
            this.disposable.Dispose();
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
            return new C1(1, new Disposable());
        }
    }
}";
            RoslynAssert.Valid(Analyzer, DisposableCode, c1, code);
        }

        [Test]
        public static void StreamInStreamReader()
        {
            var code = @"
namespace N
{
    using System.IO;

    public class C
    {
        public StreamReader M()
        {
            return new StreamReader(File.OpenRead(string.Empty));
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void StreamInStreamReaderLocal()
        {
            var code = @"
namespace N
{
    using System.IO;

    public class C
    {
        public StreamReader M()
        {
            var reader = new StreamReader(File.OpenRead(string.Empty));
            return reader;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [TestCase("new CompositeDisposable(File.OpenRead(fileName))")]
        [TestCase("new CompositeDisposable(File.OpenRead(fileName), File.OpenRead(fileName))")]
        [TestCase("new CompositeDisposable { File.OpenRead(fileName) }")]
        [TestCase("new CompositeDisposable { File.OpenRead(fileName), File.OpenRead(fileName) }")]
        [TestCase("new CompositeDisposable(File.OpenRead(fileName), File.OpenRead(fileName)) { File.OpenRead(fileName), File.OpenRead(fileName) }")]
        public static void ReturnedInCompositeDisposable(string expression)
        {
            var code = @"
namespace N
{
    using System;
    using System.IO;
    using System.Reactive.Disposables;

    public class C
    {
        public static IDisposable M(string fileName) => new CompositeDisposable(File.OpenRead(fileName));
    }
}".AssertReplace("new CompositeDisposable(File.OpenRead(fileName))", expression);
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void YieldReturnFileOpenRead()
        {
            var code = @"
namespace N
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    class C
    {
        IEnumerable<IDisposable> M()
        {
            yield return File.OpenRead(string.Empty);
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void ReturnChainedReturningThis()
        {
            var disposable = @"
namespace N
{
    using System;

    public class Disposable : IDisposable
    {
        public Disposable M() => this;

        public void Dispose()
        {
        }
    }
}";

            var code = @"
namespace N
{
    using System;

    public static class C
    {
        public static IDisposable M()
        {
            return new Disposable().M();
        }
    }
}";
            RoslynAssert.Valid(Analyzer, disposable, code);
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

            var code = @"
namespace N
{
    using System;
    using Gu.Inject;

    public static class C
    {
        public static Kernel M()
        {
            var kernel = Create()
                .BindDisposable();
            return kernel;
        }

        private static Kernel BindDisposable(this Kernel container)
        {
            container.Bind<IDisposable, Disposable>();
            return container;
        }

        private static Kernel Create()
        {
            var container = new Kernel();
            container.Creating += OnCreating;
            container.Created += OnCreated;
            return container;
        }

        private static void OnCreating(object sender, CreatingEventArgs e)
        {
        }

        private static void OnCreated(object sender, CreatedEventArgs e)
        {
        }
    }
}";
            RoslynAssert.Valid(Analyzer, disposable, code);
        }

        [Test]
        public static void FactoryChainedManyReturned()
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

            var code = @"
namespace N
{
    using System;
    using Gu.Inject;

    public static class C
    {
        public static Kernel M()
        {
            var kernel = Create()
                .BindDisposable()
                .BindDisposable()
                .BindDisposable()
                .BindDisposable()
                .BindDisposable();
            return kernel;
        }

        private static Kernel BindDisposable(this Kernel container)
        {
            container.Bind<IDisposable, Disposable>();
            return container;
        }

        private static Kernel Create()
        {
            var container = new Kernel();
            container.Creating += OnCreating;
            container.Created += OnCreated;
            return container;
        }

        private static void OnCreating(object sender, CreatingEventArgs e)
        {
        }

        private static void OnCreated(object sender, CreatedEventArgs e)
        {
        }
    }
}";
            RoslynAssert.Valid(Analyzer, disposable, code);
        }

        [Test]
        public static void FactoryChainedBinaryReturned()
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

            var code = @"
namespace N
{
    using System;
    using Gu.Inject;

    public static class C
    {
        public static Kernel M()
        {
            return Create().Rebind<IDisposable, Disposable>();
        }

        private static Kernel Create()
        {
            var container = new Kernel();
            return container;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, disposable, code);
        }

        [Test]
        public static void FactoryChainedManyBinaryReturned()
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

            var code = @"
namespace N
{
    using System;
    using Gu.Inject;

    public static class C
    {
        public static Kernel M()
        {
            return Create()
                .Rebind<IDisposable, Disposable>()
                .Rebind<IDisposable, Disposable>()
                .Rebind<IDisposable, Disposable>()
                .Rebind<IDisposable, Disposable>()
                .Rebind<IDisposable, Disposable>();
        }

        private static Kernel Create()
        {
            var container = new Kernel();
            return container;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, disposable, code);
        }

        [Test]
        public static void ExtensionMethodBindReturn()
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

            var code = @"
namespace N
{
    using System;
    using Gu.Inject;

    public static class C
    {
        public static Kernel M(this Kernel kernel)
        {
            kernel.Bind<IDisposable, Disposable>();
            return kernel;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, disposable, code);
        }

        [Test]
        public static void ExtensionMethodReturnBindMany()
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

            var code = @"
namespace N
{
    using System;
    using Gu.Inject;

    public static class C
    {
        public static Kernel M(this Kernel kernel)
        {
            return kernel.Bind<IDisposable, Disposable>()
                .Bind<IDisposable, Disposable>()
                .Bind<IDisposable, Disposable>()
                .Bind<IDisposable, Disposable>();
        }
    }
}";
            RoslynAssert.Valid(Analyzer, disposable, code);
        }
    }
}
