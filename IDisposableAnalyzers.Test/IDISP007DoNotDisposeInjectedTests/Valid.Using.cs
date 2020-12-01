namespace IDisposableAnalyzers.Test.IDISP007DoNotDisposeInjectedTests
{
    using Gu.Roslyn.Asserts;

    using Microsoft.CodeAnalysis.Diagnostics;

    using NUnit.Framework;

    public static class ValidUsing
    {
        private static readonly DiagnosticAnalyzer Analyzer = new UsingStatementAnalyzer();

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
        public static void FileOpenRead()
        {
            var code = @"
namespace N
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
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void FileOpenReadVariable()
        {
            var code = @"
namespace N
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
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void AwaitedStream()
        {
            var code = @"
namespace N
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
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void AwaitedStreamVariable()
        {
            var code = @"
namespace N
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
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void InjectedIEnumerableOfTGetEnumerator()
        {
            var code = @"
namespace N
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
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void CreatedUsingInjectedConcreteFactory()
        {
            var factoryCode = @"
namespace N
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
            RoslynAssert.Valid(Analyzer, factoryCode, disposableCode, code);
        }

        [Test]
        public static void InjectedPasswordBoxSecurePassword()
        {
            var code = @"
namespace N
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
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void CreatedUsingInjectedInterfaceFactory()
        {
            var iFactoryCode = @"
namespace N
{
    using System;

    public interface IFactory
    {
        IDisposable Create();
    }
}";
            var factoryCode = @"
namespace N
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
            RoslynAssert.Valid(Analyzer, iFactoryCode, factoryCode, disposableCode, code);
        }

        [Test]
        public static void CreatedUsingInjectedAbstractFactoryWIthImplementation()
        {
            var factoryBase = @"
namespace N
{
    using System;

    public abstract class FactoryBase
    {
        public abstract IDisposable Create();
    }
}";
            var factoryCode = @"
namespace N
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
            RoslynAssert.Valid(Analyzer, factoryBase, factoryCode, disposableCode, code);
        }

        [Test]
        public static void CreatedUsingInjectedGenericAbstractFactoryWithImplementation()
        {
            var factoryBaseOfT = @"
namespace N
{
    using System;

    public abstract class FactoryBase<T>
    {
        public abstract IDisposable Create();
    }
}";
            var factoryCode = @"
namespace N
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

            var code = @"
namespace N
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
            RoslynAssert.Valid(Analyzer, factoryBaseOfT, factoryCode, DisposableCode, code);
        }

        [Test]
        public static void CreatedUsingInjectedAbstractFactoryNoImplementation()
        {
            var factoryBase = @"
namespace N
{
    using System;

    public abstract class FactoryBase
    {
        public abstract IDisposable Create();
    }
}";

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
            RoslynAssert.Valid(Analyzer, factoryBase, disposableCode, code);
        }

        [Test]
        public static void CreatedUsingInjectedGenericAbstractFactoryNoImplementation()
        {
            var factoryBaseOfT = @"
namespace N
{
    using System;

    public abstract class FactoryBase<T>
    {
        public abstract IDisposable Create();
    }
}";

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
            RoslynAssert.Valid(Analyzer, factoryBaseOfT, disposableCode, code);
        }

        [Test]
        public static void CreatedUsingInjectedVirtualFactory()
        {
            var factoryBase = @"
namespace N
{
    using System;

    public class FactoryBase
    {
        public virtual IDisposable Create() => new Disposable();
    }
}";
            var factoryCode = @"
namespace N
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
            RoslynAssert.Valid(Analyzer, new[] { factoryBase, factoryCode, disposableCode, code });
        }

        [Test]
        public static void IgnoresRecursiveProperty()
        {
            var code = @"
namespace N
{
    using System;

    public class C
    {
        public IDisposable RecursiveProperty => RecursiveProperty;

        public void M()
        {
            var item = RecursiveProperty;

            using(var m = RecursiveProperty)
            {
            }

            using(RecursiveProperty)
            {
            }
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void IgnoresRecursiveMethod()
        {
            var code = @"
namespace N
{
    using System;

    public class C
    {
        public IDisposable RecursiveMethod() => RecursiveMethod();

        public void M()
        {
            var m = RecursiveMethod();

            using(var item = RecursiveMethod())
            {
            }

            using(RecursiveMethod())
            {
            }
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
            using (disposable = File.OpenRead(string.Empty))
            {
            }
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
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void ReassignedParameterViaOutAnd()
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
            RoslynAssert.Valid(Analyzer, code);
        }
    }
}
