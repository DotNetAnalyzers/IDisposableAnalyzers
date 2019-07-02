namespace IDisposableAnalyzers.Test.IDISP007DontDisposeInjectedTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public partial class ValidCode
    {
        public static class Using
        {
            // ReSharper disable once MemberHidesStaticFromOuterClass
            private static readonly DiagnosticAnalyzer Analyzer = new UsingStatementAnalyzer();

            [Test]
            public static void FileOpenRead()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class C
    {
        public C()
        {
            using (File.OpenRead(string.Empty))
            {
            }
        }
    }
}";
                RoslynAssert.Valid(Analyzer, testCode);
            }

            [Test]
            public static void FileOpenReadVariable()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class C
    {
        public C()
        {
            using (var stream = File.OpenRead(string.Empty))
            {
            }
        }
    }
}";
                RoslynAssert.Valid(Analyzer, testCode);
            }

            [Test]
            public static void AwaitedStream()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;
    using System.Threading.Tasks;

    public static class C
    {
        public static async Task M()
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
                RoslynAssert.Valid(Analyzer, testCode);
            }

            [Test]
            public static void AwaitedStreamVariable()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;
    using System.Threading.Tasks;

    public static class C
    {
        public static async Task<long> M()
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
                RoslynAssert.Valid(Analyzer, testCode);
            }

            [Test]
            public static void InjectedIEnumerableOfTGetEnumerator()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.Collections.Generic;

    public static class C
    {
        public static IEnumerable<T> M<T>(this IEnumerable<T> source)
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
                RoslynAssert.Valid(Analyzer, testCode);
            }

            [Test]
            public static void CreatedUsingInjectedConcreteFactory()
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
    public class C
    {
        public C(Factory factory)
        {
            using (factory.Create())
            {
            }
        }
    }
}";
                RoslynAssert.Valid(Analyzer, factoryCode, disposableCode, testCode);
            }

            [Test]
            public static void InjectedPasswordBoxSecurePassword()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.Windows.Controls;

    internal class C
    {
        private readonly PasswordBox passwordBox;

        internal C(PasswordBox passwordBox)
        {
            this.passwordBox = passwordBox;
            using (this.passwordBox.SecurePassword)
            {
            }
        }
    }
}";
                RoslynAssert.Valid(Analyzer, testCode);
            }

            [Test]
            public static void CreatedUsingInjectedInterfaceFactory()
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
    public class C
    {
        public C(IFactory factory)
        {
            using (factory.Create())
            {
            }
        }
    }
}";
                RoslynAssert.Valid(Analyzer, iFactoryCode, factoryCode, disposableCode, testCode);
            }

            [Test]
            public static void CreatedUsingInjectedAbstractFactoryWIthImplementation()
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
    public class C
    {
        public C(FactoryBase factory)
        {
            using (factory.Create())
            {
            }
        }
    }
}";
                RoslynAssert.Valid(Analyzer, abstractFactoryCode, factoryCode, disposableCode, testCode);
            }

            [Test]
            public static void CreatedUsingInjectedGenericAbstractFactoryWithImplementation()
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
    public class C
    {
        public static void M<T>(FactoryBase<T> factory)
        {
            using (factory.Create())
            {
            }
        }
    }
}";
                RoslynAssert.Valid(Analyzer, abstractFactoryCode, factoryCode, DisposableCode, testCode);
            }

            [Test]
            public static void CreatedUsingInjectedAbstractFactoryNoImplementation()
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
    public class C
    {
        public C(FactoryBase factory)
        {
            using (factory.Create())
            {
            }
        }
    }
}";
                RoslynAssert.Valid(Analyzer, abstractFactoryCode, disposableCode, testCode);
            }

            [Test]
            public static void CreatedUsingInjectedGenericAbstractFactoryNoImplementation()
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
    public class C
    {
        public C(FactoryBase<int> factory)
        {
            using (factory.Create())
            {
            }
        }
    }
}";
                RoslynAssert.Valid(Analyzer, abstractFactoryCode, disposableCode, testCode);
            }

            [Test]
            public static void CreatedUsingInjectedVirtualFactory()
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
    public class C
    {
        public C(FactoryBase factory)
        {
            using (factory.Create())
            {
            }
        }
    }
}";
                RoslynAssert.Valid(Analyzer, new[] { abstractFactoryCode, factoryCode, disposableCode, testCode });
            }

            [Test]
            public static void IgnoresRecursiveProperty()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class C
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
                RoslynAssert.Valid(Analyzer, testCode);
            }

            [Test]
            public static void IgnoresRecursiveMethod()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class C
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
                RoslynAssert.Valid(Analyzer, testCode);
            }

            [Test]
            public static void ReassignedParameter()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    internal class C
    {
        public static void ReassignParameter(IDisposable disposable)
        {
            using (disposable = File.OpenRead(string.Empty))
            {
            }
        }
    }
}";
                RoslynAssert.Valid(Analyzer, testCode);
            }

            [Test]
            public static void ReassignedParameterViaOut()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    internal class C
    {
        public static void ReassignParameter(IDisposable disposable)
        {
            if (TryReassign(disposable, out disposable))
            {
                using (disposable)
                {
                }
            }
        }

        private static bool TryReassign(IDisposable old, out IDisposable result)
        {
            result = File.OpenRead(string.Empty);
            return true;
        }
    }
}";
                RoslynAssert.Valid(Analyzer, testCode);
            }

            [Test]
            public static void ReassignedParameterViaOutAnd()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    internal class C
    {
        public static void ReassignParameter(IDisposable disposable)
        {
            if (disposable == null &&
                TryReassign(disposable, out disposable))
            {
                using (disposable)
                {
                }
            }
        }

        private static bool TryReassign(IDisposable old, out IDisposable result)
        {
            result = File.OpenRead(string.Empty);
            return true;
        }
    }
}";
                RoslynAssert.Valid(Analyzer, testCode);
            }
        }
    }
}
