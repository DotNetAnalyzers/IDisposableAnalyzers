namespace IDisposableAnalyzers.Test.IDISP004DontIgnoreReturnValueOfTypeIDisposableTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal class Diagnostics
    {
        private static readonly IDISP004DontIgnoreReturnValueOfTypeIDisposable Analyzer = new IDISP004DontIgnoreReturnValueOfTypeIDisposable();

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
        public void FileOpenRead()
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
            AnalyzerAssert.Diagnostics(Analyzer, testCode);
        }

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
            AnalyzerAssert.Diagnostics(Analyzer, DisposableCode, testCode);
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
            AnalyzerAssert.Diagnostics(Analyzer, DisposableCode, testCode);
        }

        [Test]
        public void FileOpenReadPassedIntoCtor()
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
            AnalyzerAssert.Diagnostics(Analyzer, barCode, testCode);
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
            AnalyzerAssert.Diagnostics(Analyzer, DisposableCode, barCode, testCode);
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
            AnalyzerAssert.Diagnostics(Analyzer, interfaceCode, disposableCode, factoryCode, testCode);
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
            AnalyzerAssert.Diagnostics(Analyzer, factoryCode, DisposableCode, testCode);
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
            AnalyzerAssert.Diagnostics(Analyzer, DisposableCode, testCode);
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
            AnalyzerAssert.Diagnostics(Analyzer, DisposableCode, fooCode, testCode);
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
            AnalyzerAssert.Diagnostics(Analyzer, DisposableCode, fooCode, testCode);
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
            AnalyzerAssert.Diagnostics(Analyzer, DisposableCode, testCode);
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
            AnalyzerAssert.Diagnostics(Analyzer, DisposableCode, testCode);
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
            AnalyzerAssert.Diagnostics(Analyzer, DisposableCode, testCode);
        }

        [Test]
        public void MethodCreatingDisposableExpressionBodyToString()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public static class Foo
    {
        public static Stream Stream() => File.OpenRead(string.Empty);

        public static long Bar()
        {
            return ↓Stream().Length;
        }
    }
}";
            AnalyzerAssert.Diagnostics(Analyzer, testCode);
        }

        [Explicit("Fix later")]
        [Test]
        public void PropertyCreatingDisposableExpressionBodyToString()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public static class Foo
    {
        public static Stream Stream => File.OpenRead(string.Empty);

        public static long Bar()
        {
            return ↓Stream.Length;
        }
    }
}";
            AnalyzerAssert.Diagnostics(Analyzer, testCode);
        }

        [Test]
        public void NoFixForArgument()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        internal static string Bar()
        {
            return Meh(↓File.OpenRead(string.Empty));
        }

        private static string Meh(Stream stream) => stream.ToString();
    }
}";

            AnalyzerAssert.Diagnostics(Analyzer, testCode);
        }
    }
}
