namespace IDisposableAnalyzers.Test.IDISP004DoNotIgnoreCreatedTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    public static partial class Valid
    {
        [Test]
        public static void Generic()
        {
            var factoryCode = @"
namespace N
{
    public class Factory
    {
        public static T Create<T>() where T : new() => new T();
    }
}";

            var testCode = @"
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
            RoslynAssert.Valid(Analyzer, factoryCode, testCode);
        }

        [Test]
        public static void Operator()
        {
            var mehCode = @"
namespace N
{
    public class Meh
    {
        public static Meh operator +(Meh left, Meh right) => new Meh();
    }
}";

            var testCode = @"
namespace N
{
    public class C
    {
        public object M()
        {
            var meh1 = new Meh();
            var meh2 = new Meh();
            return meh1 + meh2;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, mehCode, testCode);
        }

        [Test]
        public static void OperatorNestedCall()
        {
            var mehCode = @"
namespace N
{
    public class Meh
    {
        public static Meh operator +(Meh left, Meh right) => new Meh();
    }
}";

            var testCode = @"
namespace N
{
    public class C
    {
        public object M()
        {
            var meh1 = new Meh();
            var meh2 = new Meh();
            return Add(new Meh(), new Meh());
        }

        public object Add(Meh meh1, Meh meh2)
        {
            return meh1 + meh2;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, mehCode, testCode);
        }

        [Test]
        public static void OperatorEquals()
        {
            var mehCode = @"
namespace N
{
    public class Meh
    {
    }
}";

            var testCode = @"
namespace N
{
    public class C
    {
        public bool M()
        {
            var meh1 = new Meh();
            var meh2 = new Meh();
            return meh1 == meh2;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, mehCode, testCode);
        }

        [Test]
        public static void MethodReturningObject()
        {
            var testCode = @"
namespace N
{
    public class C
    {
        public void M()
        {
            Meh();
        }

        private static object Meh() => new object();
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void MethodWithArgReturningObject()
        {
            var testCode = @"
namespace N
{
    public class C
    {
        public void M()
        {
            Meh(""Meh"");
        }

        private static object Meh(string arg) => new object();
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void MethodWithObjArgReturningObject()
        {
            var testCode = @"
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
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void ReturningStatementBody()
        {
            var testCode = @"
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
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void ReturningLocalStatementBody()
        {
            var testCode = @"
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
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void ReturningExpressionBody()
        {
            var testCode = @"
namespace N
{
    using System.IO;

    public class C
    {
        public Stream M() => File.OpenRead(string.Empty);
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void ReturningNewAssigningAndDisposing()
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
            this.disposable.Dispose();
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
            return new C(new Disposable());
        }
    }
}";
            RoslynAssert.Valid(Analyzer, DisposableCode, fooCode, testCode);
        }

        [TestCase("new C()")]
        [TestCase("new C(new Disposable())")]
        [TestCase("new C(new Disposable(), new Disposable())")]
        public static void ReturningNewAssigningAndDisposingParams(string objectCreation)
        {
            var fooCode = @"
namespace N
{
    using System;

    public class C : IDisposable
    {
        private readonly IDisposable[] disposables;

        public C(params IDisposable[] disposables)
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
            var testCode = @"
namespace N
{
    public class Meh
    {
        public C M()
        {
            return new C(new Disposable(), new Disposable());
        }
    }
}".AssertReplace("new C(new Disposable(), new Disposable())", objectCreation);

            RoslynAssert.Valid(Analyzer, DisposableCode, fooCode, testCode);
        }

        [Test]
        public static void ReturningCreateNewAssigningAndDisposing()
        {
            var fooCode = @"
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
            this.disposable?.Dispose();
        }
    }
}";
            var testCode = @"
namespace N
{
    using System;

    public class Meh
    {
        public C M()
        {
            return Create(new Disposable());
        }

        private static C Create(IDisposable disposable) => new C(disposable);
    }
}";
            RoslynAssert.Valid(Analyzer, DisposableCode, fooCode, testCode);
        }

        [Test]
        public static void ReturningCreateNewStreamReader()
        {
            var testCode = @"
namespace N
{
    using System.IO;

    public class Meh
    {
        public StreamReader M()
        {
            return Create(File.OpenRead(string.Empty));
        }

        private static StreamReader Create(Stream stream) => new StreamReader(stream);
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void ReturningAssigningPrivateChained()
        {
            var fooCode = @"
namespace N
{
    using System;

    public class C : IDisposable
    {
        private readonly IDisposable disposable;

        public C(int value, IDisposable disposable)
            : this(disposable)
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
            var testCode = @"
namespace N
{
    public class Meh
    {
        public C M()
        {
            return new C(1, new Disposable());
        }
    }
}";
            RoslynAssert.Valid(Analyzer, DisposableCode, fooCode, testCode);
        }

        [Test]
        public static void StreamInStreamReader()
        {
            var testCode = @"
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
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void StreamInStreamReaderLocal()
        {
            var testCode = @"
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
            RoslynAssert.Valid(Analyzer, testCode);
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
    }
}
