namespace IDisposableAnalyzers.Test.IDISP007DontDisposeInjectedTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    internal partial class ValidCode
    {
        public class Using
        {
            // ReSharper disable once MemberHidesStaticFromOuterClass
            private static readonly DiagnosticAnalyzer Analyzer = new UsingStatementAnalyzer();

            [Test]
            public void FileOpenRead()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        public Foo()
        {
            using (File.OpenRead(string.Empty))
            {
            }
        }
    }
}";
                AnalyzerAssert.Valid(Analyzer, testCode);
            }

            [Test]
            public void FileOpenReadVariable()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        public Foo()
        {
            using (var stream = File.OpenRead(string.Empty))
            {
            }
        }
    }
}";
                AnalyzerAssert.Valid(Analyzer, testCode);
            }

            [Test]
            public void AwaitedStream()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;
    using System.Threading.Tasks;

    public static class Foo
    {
        public static async Task Bar()
        {
            using (await ReadAsync(string.Empty).ConfigureAwait(false))
            {
            }
        }

        private static async Task<MemoryStream> ReadAsync(string fileName)
        {
            var stream = new MemoryStream();
            using (var fileStream = File.OpenRead(fileName))
            {
                await fileStream.CopyToAsync(stream)
                                ;
            }

            stream.Position = 0;
            return stream;
        }
    }
}";
                AnalyzerAssert.Valid(Analyzer, testCode);
            }

            [Test]
            public void AwaitedStreamVariable()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;
    using System.Threading.Tasks;

    public static class Foo
    {
        public static async Task<long> Bar()
        {
            using (var stream = await ReadAsync(string.Empty).ConfigureAwait(false))
            {
                return stream.Length;
            }
        }

        private static async Task<MemoryStream> ReadAsync(string fileName)
        {
            var stream = new MemoryStream();
            using (var fileStream = File.OpenRead(fileName))
            {
                await fileStream.CopyToAsync(stream)
                                ;
            }

            stream.Position = 0;
            return stream;
        }
    }
}";
                AnalyzerAssert.Valid(Analyzer, testCode);
            }

            [Test]
            public void InjectedIEnumerableOfTGetEnumerator()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.Collections.Generic;

    public static class Foo
    {
        public static IEnumerable<T> Bar<T>(this IEnumerable<T> source)
        {
            using (var e = source.GetEnumerator())
            {
                while (e.MoveNext())
                {
                    yield return default(T);
                }
            }
        }
    }
}";
                AnalyzerAssert.Valid(Analyzer, testCode);
            }

            [Test]
            public void CreatedUsingInjectedConcreteFactory()
            {
                var factoryCode = @"
namespace RoslynSandbox
{
    using System;

    public class Factory
    {
        public IDisposable Create()
        {
            return new Disposable();
        }
    }
}";
                var disposableCode = @"
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

                var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public Foo(Factory factory)
        {
            using (factory.Create())
            {
            }
        }
    }
}";
                AnalyzerAssert.Valid(Analyzer, factoryCode, disposableCode, testCode);
            }

            [Test]
            public void InjectedPasswordBoxSecurePassword()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.Windows.Controls;

    internal class Foo
    {
        private readonly PasswordBox passwordBox;

        internal Foo(PasswordBox passwordBox)
        {
            this.passwordBox = passwordBox;
            using (this.passwordBox.SecurePassword)
            {
            }
        }
    }
}";
                AnalyzerAssert.Valid(Analyzer, testCode);
            }

            [Test]
            public void CreatedUsingInjectedInterfaceFactory()
            {
                var iFactoryCode = @"
namespace RoslynSandbox
{
    using System;

    public interface IFactory
    {
        IDisposable Create();
    }
}";
                var factoryCode = @"
namespace RoslynSandbox
{
    using System;

    public class Factory : IFactory
    {
        public IDisposable Create()
        {
            return new Disposable();
        }
    }
}";
                var disposableCode = @"
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

                var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public Foo(IFactory factory)
        {
            using (factory.Create())
            {
            }
        }
    }
}";
                AnalyzerAssert.Valid(Analyzer, iFactoryCode, factoryCode, disposableCode, testCode);
            }

            [Test]
            public void CreatedUsingInjectedAbstractFactoryWIthImplementation()
            {
                var abstractFactoryCode = @"
namespace RoslynSandbox
{
    using System;

    public abstract class FactoryBase
    {
        public abstract IDisposable Create();
    }
}";
                var factoryCode = @"
namespace RoslynSandbox
{
    using System;

    public class Factory : FactoryBase
    {
        public override IDisposable Create()
        {
            return new Disposable();
        }
    }
}";
                var disposableCode = @"
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

                var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public Foo(FactoryBase factory)
        {
            using (factory.Create())
            {
            }
        }
    }
}";
                AnalyzerAssert.Valid(Analyzer, abstractFactoryCode, factoryCode, disposableCode, testCode);
            }

            [Test]
            public void CreatedUsingInjectedGenericAbstractFactoryWithImplementation()
            {
                var abstractFactoryCode = @"
namespace RoslynSandbox
{
    using System;

    public abstract class FactoryBase<T>
    {
        public abstract IDisposable Create();
    }
}";
                var factoryCode = @"
namespace RoslynSandbox
{
    using System;

    public class Factory : FactoryBase<int>
    {
        public override IDisposable Create()
        {
            return new Disposable();
        }
    }
}";

                var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public static void Bar<T>(FactoryBase<T> factory)
        {
            using (factory.Create())
            {
            }
        }
    }
}";
                AnalyzerAssert.Valid(Analyzer, abstractFactoryCode, factoryCode, DisposableCode, testCode);
            }

            [Test]
            public void CreatedUsingInjectedAbstractFactoryNoImplementation()
            {
                var abstractFactoryCode = @"
namespace RoslynSandbox
{
    using System;

    public abstract class FactoryBase
    {
        public abstract IDisposable Create();
    }
}";

                var disposableCode = @"
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

                var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public Foo(FactoryBase factory)
        {
            using (factory.Create())
            {
            }
        }
    }
}";
                AnalyzerAssert.Valid(Analyzer, abstractFactoryCode, disposableCode, testCode);
            }

            [Test]
            public void CreatedUsingInjectedGenericAbstractFactoryNoImplementation()
            {
                var abstractFactoryCode = @"
namespace RoslynSandbox
{
    using System;

    public abstract class FactoryBase<T>
    {
        public abstract IDisposable Create();
    }
}";

                var disposableCode = @"
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

                var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public Foo(FactoryBase<int> factory)
        {
            using (factory.Create())
            {
            }
        }
    }
}";
                AnalyzerAssert.Valid(Analyzer, abstractFactoryCode, disposableCode, testCode);
            }

            [Test]
            public void CreatedUsingInjectedVirtualFactory()
            {
                var abstractFactoryCode = @"
namespace RoslynSandbox
{
    using System;

    public class FactoryBase
    {
        public virtual IDisposable Create() => new Disposable();
    }
}";
                var factoryCode = @"
namespace RoslynSandbox
{
    using System;

    public class Factory : FactoryBase
    {
        public override IDisposable Create()
        {
            return new Disposable();
        }
    }
}";
                var disposableCode = @"
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

                var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public Foo(FactoryBase factory)
        {
            using (factory.Create())
            {
            }
        }
    }
}";
                AnalyzerAssert.Valid(Analyzer, new[] { abstractFactoryCode, factoryCode, disposableCode, testCode });
            }

            [Test]
            public void IgnoresRecursiveProperty()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        public IDisposable RecursiveProperty => RecursiveProperty;

        public void Meh()
        {
            var item = RecursiveProperty;

            using(var meh = RecursiveProperty)
            {
            }

            using(RecursiveProperty)
            {
            }
        }
    }
}";
                AnalyzerAssert.Valid(Analyzer, testCode);
            }

            [Test]
            public void IgnoresRecursiveMethod()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        public IDisposable RecursiveMethod() => RecursiveMethod();

        public void Meh()
        {
            var meh = RecursiveMethod();

            using(var item = RecursiveMethod())
            {
            }

            using(RecursiveMethod())
            {
            }
        }
    }
}";
                AnalyzerAssert.Valid(Analyzer, testCode);
            }
        }
    }
}
