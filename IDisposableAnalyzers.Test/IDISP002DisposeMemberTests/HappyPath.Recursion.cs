namespace IDisposableAnalyzers.Test.IDISP002DisposeMemberTests
{
    using System.Threading.Tasks;

    using NUnit.Framework;

    internal partial class HappyPath
    {
        public class Recursion : NestedHappyPathVerifier<HappyPath>
        {
            [Test]
            public async Task IgnoresWhenDisposingRecursiveProperty()
            {
                var testCode = @"
    using System;

    public class Foo : IDisposable
    {
        public IDisposable RecursiveProperty => RecursiveProperty;

        public void Dispose()
        {
            this.RecursiveProperty.Dispose();
        }
    }";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task IgnoresWhenNotDisposingRecursiveProperty()
            {
                var testCode = @"
    using System;

    public class Foo : IDisposable
    {
        public IDisposable RecursiveProperty => RecursiveProperty;

        public void Dispose()
        {
        }
    }";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task IgnoresWhenDisposingFieldAssignedWithRecursiveProperty()
            {
                var testCode = @"
    using System;

    public class Foo : IDisposable
    {
        private IDisposable disposable;

        public Foo()
        {
            this.disposable = this.RecursiveProperty;
        }

        public IDisposable RecursiveProperty => RecursiveProperty;

        public void Dispose()
        {
            this.disposable.Dispose();
        }
    }";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task IgnoresWhenNotDisposingFieldAssignedWithRecursiveProperty()
            {
                var testCode = @"
    using System;

    public class Foo : IDisposable
    {
        private IDisposable disposable;

        public Foo()
        {
            this.disposable = this.RecursiveProperty;
        }

        public IDisposable RecursiveProperty => RecursiveProperty;

        public void Dispose()
        {
        }
    }";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task IgnoresWhenDisposingRecursiveMethod()
            {
                var testCode = @"
using System;

public class Foo
{
    public IDisposable RecursiveMethod() => RecursiveMethod();

    public void Dispose()
    {
        this.RecursiveMethod().Dispose();
    }
}";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task IgnoresWhenDisposingRecursiveMethodChain()
            {
                var testCode = @"
using System;

public class Foo
{
    public IDisposable Recursive1() => Recursive2();

    public IDisposable Recursive2() => Recursive1();

    public void Dispose()
    {
        this.Recursive1().Dispose();
    }
}";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task IgnoresRecursiveOutParameter()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo
    {
        private readonly Stream stream;

        public Foo()
        {
            if (this.TryGetStream(out this.stream))
            {
            }
        }

        public bool TryGetStream(out Stream outValue)
        {
            outValue = null;
            return this.TryGetStream(out outValue);
        }
    }
}";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task IgnoresRecursiveOutParameterChain()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public sealed class Foo 
    {
        private readonly Stream stream;

        public Foo()
        {
            if (this.TryGetStream1(out this.stream))
            {
            }
        }

        public bool TryGetStream1(out Stream outValue1)
        {
            return this.TryGetStream2(out outValue1);
        }

        public bool TryGetStream2(out Stream outValue2)
        {
            return this.TryGetStream1(out outValue2);
        }
    }
}";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }
        }
    }
}