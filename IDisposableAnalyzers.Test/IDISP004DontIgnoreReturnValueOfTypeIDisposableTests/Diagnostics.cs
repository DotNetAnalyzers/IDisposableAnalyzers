namespace IDisposableAnalyzers.Test.IDISP004DontIgnoreReturnValueOfTypeIDisposableTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal class Diagnostics
    {
        private static readonly string DisposableCode = @"
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
        public void IgnoringFileOpenRead()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo
    {
        public void Meh()
        {
            ↓File.OpenRead(string.Empty);
        }
    }
}";
            AnalyzerAssert.Diagnostics<IDISP004DontIgnoreReturnValueOfTypeIDisposable>(testCode);
        }

        [Test]
        public void IgnoringNewDisposable()
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
            AnalyzerAssert.Diagnostics<IDISP004DontIgnoreReturnValueOfTypeIDisposable>(DisposableCode, testCode);
        }

        [Test]
        public void FactoryMethodNewDisposable()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public sealed class Foo
    {
        public void Meh()
        {
            ↓Create();
        }

        private static Disposable Create()
        {
            return new Disposable();
        }
    }
}";
            AnalyzerAssert.Diagnostics<IDISP004DontIgnoreReturnValueOfTypeIDisposable>(DisposableCode, testCode);
        }

        [Test]
        public void IgnoringFileOpenReadPassedIntoCtor()
        {
            var barCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Bar
    {
        private readonly Stream stream;

        public Bar(Stream stream)
        {
           this.stream = stream;
        }
    }
}";
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public sealed class Foo
    {
        public Bar Meh()
        {
            return new Bar(↓File.OpenRead(string.Empty));
        }
    }
}";
            AnalyzerAssert.Diagnostics<IDISP004DontIgnoreReturnValueOfTypeIDisposable>(barCode, testCode);
        }

        [Test]
        public void IgnoringNewDisposabledPassedIntoCtor()
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
            AnalyzerAssert.Diagnostics<IDISP004DontIgnoreReturnValueOfTypeIDisposable>(DisposableCode, barCode, testCode);
        }

        [Test]
        public void Generic()
        {
            var interfaceCode = @"
namespace RoslynSandbox
{
    using System;
 
    public interface IDisposable<T> : IDisposable
    {
    }
}";

            var disposableCode = @"
namespace RoslynSandbox
{
    public sealed class Disposable<T> : IDisposable<T>
    {
        public void Dispose()
        {
        }
    }
}";

            var factoryCode = @"
namespace RoslynSandbox
{
    public class Factory
    {
        public static IDisposable<T> Create<T>() => new Disposable<T>();
    }
}";

            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        public void Bar()
        {
            ↓Factory.Create<int>();
        }
    }
}";
            AnalyzerAssert.Diagnostics<IDISP004DontIgnoreReturnValueOfTypeIDisposable>(interfaceCode, disposableCode, factoryCode, testCode);
        }

        [Test]
        public void ConstrainedGeneric()
        {
            var factoryCode = @"
namespace RoslynSandbox
{
    using System;

    public class Factory
    {
        public static T Create<T>() where T : IDisposable, new() => new T();
    }
}";

            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public void Bar()
        {
            ↓Factory.Create<Disposable>();
        }
    }
}";
            AnalyzerAssert.Diagnostics<IDISP004DontIgnoreReturnValueOfTypeIDisposable>(factoryCode, DisposableCode, testCode);
        }

        [Test]
        public void WithOptionalParameter()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections.Generic;

    public class Foo
    {
        public Foo(IDisposable disposable)
        {
            ↓Bar(disposable);
        }

        private static IDisposable Bar(IDisposable disposable, List<IDisposable> list = null)
        {
            if (list == null)
            {
                list = new List<IDisposable>();
            }

            if (list.Contains(disposable))
            {
                return new Disposable();
            }

            list.Add(disposable);
            return Bar(disposable, list);
        }
    }
}";
            AnalyzerAssert.Diagnostics<IDISP004DontIgnoreReturnValueOfTypeIDisposable>(DisposableCode, testCode);
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
            AnalyzerAssert.Diagnostics<IDISP004DontIgnoreReturnValueOfTypeIDisposable>(DisposableCode, fooCode, testCode);
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
            AnalyzerAssert.Diagnostics<IDISP004DontIgnoreReturnValueOfTypeIDisposable>(DisposableCode, fooCode, testCode);
        }
    }
}