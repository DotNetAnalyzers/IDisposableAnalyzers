namespace IDisposableAnalyzers.Test.IDISP007DontDisposeInjectedTests
{
    using System.Threading.Tasks;

    using NUnit.Framework;

    internal partial class HappyPath : HappyPathVerifier<IDISP007DontDisposeInjected>
    {
        public class Using : NestedHappyPathVerifier<HappyPath>
        {
            [Test]
            public async Task FileOpenRead()
            {
                var testCode = @"
    using System.IO;

    public class Foo
    {
        public Foo()
        {
            using (File.OpenRead(string.Empty))
            {
            }
        }
    }";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task FileOpenReadVariable()
            {
                var testCode = @"
    using System.IO;

    public class Foo
    {
        public Foo()
        {
            using (var stream = File.OpenRead(string.Empty))
            {
            }
        }
    }";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task AwaitedStream()
            {
                var testCode = @"
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
                            .ConfigureAwait(false);
        }

        stream.Position = 0;
        return stream;
    }
}";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task AwaitedStreamVariable()
            {
                var testCode = @"
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
                            .ConfigureAwait(false);
        }

        stream.Position = 0;
        return stream;
    }
}";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task InjectedIEnumerableOfTGetEnumerator()
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
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task CreatedUsingInjectedConcreteFactory()
            {
                var factoryCode = @"
using System;

public class Factory
{
    public IDisposable Create()
    {
        return new Disposable();
    }
}";
                var disposableCode = @"
using System;

public class Disposable : IDisposable
{
    public void Dispose()
    {
    }
}";

                var testCode = @"
public class Foo
{
    public Foo(Factory factory)
    {
        using (factory.Create())
        {
        }
    }
}";
                await this.VerifyHappyPathAsync(factoryCode, disposableCode, testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task InjectedPasswordBoxSecurePassword()
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
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task CreatedUsingInjectedInterfaceFactory()
            {
                var iFactoryCode = @"
using System;

public interface IFactory
{
    IDisposable Create();
}";
                var factoryCode = @"
using System;

public class Factory : IFactory
{
    public IDisposable Create()
    {
        return new Disposable();
    }
}";
                var disposableCode = @"
using System;

public class Disposable : IDisposable
{
    public void Dispose()
    {
    }
}";

                var testCode = @"
public class Foo
{
    public Foo(IFactory factory)
    {
        using (factory.Create())
        {
        }
    }
}";
                await this.VerifyHappyPathAsync(iFactoryCode, factoryCode, disposableCode, testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task CreatedUsingInjectedAbstractFactoryWIthImplementation()
            {
                var abstractFactoryCode = @"
using System;

public abstract class FactoryBase
{
    public abstract IDisposable Create();
}";
                var factoryCode = @"
using System;

public class Factory : FactoryBase
{
    public override IDisposable Create()
    {
        return new Disposable();
    }
}";
                var disposableCode = @"
using System;

public class Disposable : IDisposable
{
    public void Dispose()
    {
    }
}";

                var testCode = @"
public class Foo
{
    public Foo(FactoryBase factory)
    {
        using (factory.Create())
        {
        }
    }
}";
                await this.VerifyHappyPathAsync(abstractFactoryCode, factoryCode, disposableCode, testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task CreatedUsingInjectedGenericAbstractFactoryWithImplementation()
            {
                var abstractFactoryCode = @"
using System;

public abstract class FactoryBase<T>
{
    public abstract IDisposable Create();
}";
                var factoryCode = @"
using System;

public class Factory : FactoryBase<int>
{
    public override IDisposable Create()
    {
        return new Disposable();
    }
}";

                var testCode = @"
public class Foo
{
    public static void Bar<T>(FactoryBase<T> factory)
    {
        using (factory.Create())
        {
        }
    }
}";
                await this.VerifyHappyPathAsync(abstractFactoryCode, factoryCode, DisposableCode, testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task CreatedUsingInjectedAbstractFactoryNoImplementation()
            {
                var abstractFactoryCode = @"
using System;

public abstract class FactoryBase
{
    public abstract IDisposable Create();
}";

                var disposableCode = @"
using System;

public class Disposable : IDisposable
{
    public void Dispose()
    {
    }
}";

                var testCode = @"
public class Foo
{
    public Foo(FactoryBase factory)
    {
        using (factory.Create())
        {
        }
    }
}";
                await this.VerifyHappyPathAsync(abstractFactoryCode, disposableCode, testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task CreatedUsingInjectedGenericAbstractFactoryNoImplementation()
            {
                var abstractFactoryCode = @"
using System;

public abstract class FactoryBase<T>
{
    public abstract IDisposable Create();
}";

                var disposableCode = @"
using System;

public class Disposable : IDisposable
{
    public void Dispose()
    {
    }
}";

                var testCode = @"
public class Foo
{
    public Foo(FactoryBase<int> factory)
    {
        using (factory.Create())
        {
        }
    }
}";
                await this.VerifyHappyPathAsync(abstractFactoryCode, disposableCode, testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task CreatedUsingInjectedVirtualFactory()
            {
                var abstractFactoryCode = @"
using System;

public class FactoryBase
{
    public virtual IDisposable Create() => new Disposable();
}";
                var factoryCode = @"
using System;

public class Factory : FactoryBase
{
    public override IDisposable Create()
    {
        return new Disposable();
    }
}";
                var disposableCode = @"
using System;

public class Disposable : IDisposable
{
    public void Dispose()
    {
    }
}";

                var testCode = @"
public class Foo
{
    public Foo(FactoryBase factory)
    {
        using (factory.Create())
        {
        }
    }
}";
                await this.VerifyHappyPathAsync(new[] { abstractFactoryCode, factoryCode, disposableCode, testCode })
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task IgnoresRecursiveProperty()
            {
                var testCode = @"
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
}";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task IgnoresRecursiveMethod()
            {
                var testCode = @"
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
}";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }
        }
    }
}