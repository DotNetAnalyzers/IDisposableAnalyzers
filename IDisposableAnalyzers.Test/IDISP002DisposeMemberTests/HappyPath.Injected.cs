namespace IDisposableAnalyzers.Test.IDISP002DisposeMemberTests
{
    using System.Threading.Tasks;

    using NUnit.Framework;

    internal partial class HappyPath
    {
        internal class Injected : NestedHappyPathVerifier<HappyPath>
        {
            [Test]
            public async Task IgnoreAssignedWithCtorArgument()
            {
                var testCode = @"
    using System;

    public sealed class Foo
    {
        private readonly IDisposable bar;
        
        public Foo(IDisposable bar)
        {
            this.bar = bar;
        }
    }";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task IgnoreAssignedWithCtorArgumentIndexer()
            {
                var testCode = @"
    using System;

    public sealed class Foo
    {
        private readonly IDisposable bar;
        
        public Foo(IDisposable[] bars)
        {
            this.bar = bars[0];
        }
    }";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task IgnoreInjectedAndCreatedField()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo
    {
        private readonly IDisposable bar;

        public Foo(IDisposable bar)
        {
            this.bar = bar;
        }

        public static Foo Create() => new Foo(new Disposable());
    }
}";
                await this.VerifyHappyPathAsync(DisposableCode, testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task IgnoreInjectedAndCreatedProperty()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo
    {
        public Foo(IDisposable bar)
        {
            this.Bar = bar;
        }

        public IDisposable Bar { get; }

        public static Foo Create() => new Foo(new Disposable());
    }
}";
                await this.VerifyHappyPathAsync(DisposableCode, testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task IgnoreInjectedAndCreatedPropertyWhenFactoryTouchesIndexer()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo
    {
        private readonly IDisposable bar;

        public Foo(IDisposable bar)
        {
            this.bar = bar;
        }

        public static Foo Create()
        {
            var disposables = new[] { new Disposable() };
            return new Foo(disposables[0]);
        }
    }
}";
                await this.VerifyHappyPathAsync(DisposableCode, testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task IgnoreDictionaryPassedInViaCtor()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.Collections.Concurrent;
    using System.IO;

    public class Foo
    {
        private readonly Stream current;

        public Foo(ConcurrentDictionary<int, Stream> streams)
        {
            this.current = streams[1];
        }
    }
}";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task IgnorePassedInViaCtorUnderscore()
            {
                var testCode = @"
    using System;

    public sealed class Foo
    {
        private readonly IDisposable _bar;
        
        public Foo(IDisposable bar)
        {
            _bar = bar;
        }
    }";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task IgnorePassedInViaCtorUnderscoreWhenClassIsDisposable()
            {
                var testCode = @"
    using System;

    public sealed class Foo : IDisposable
    {
        private readonly IDisposable _bar;
        
        public Foo(IDisposable bar)
        {
            _bar = bar;
        }

        public void Dispose()
        {
        }
    }";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }
        }
    }
}