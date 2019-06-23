namespace IDisposableAnalyzers.Test.IDISP002DisposeMemberTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    public partial class ValidCode
    {
        public class Injected
        {
            [Test]
            public void IgnoreAssignedWithCtorArgument()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class C
    {
        private readonly IDisposable bar;
        
        public C(IDisposable bar)
        {
            this.bar = bar;
        }
    }
}";
                RoslynAssert.Valid(Analyzer, testCode);
            }

            [Test]
            public void IgnoreAssignedWithCtorArgumentIndexer()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class C
    {
        private readonly IDisposable bar;
        
        public C(IDisposable[] bars)
        {
            this.bar = bars[0];
        }
    }
}";
                RoslynAssert.Valid(Analyzer, testCode);
            }

            [Test]
            public void IgnoreInjectedAndCreatedPropertyWhenFactoryTouchesIndexer()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class C
    {
        private readonly IDisposable bar;

        public C(IDisposable bar)
        {
            this.bar = bar;
        }

        public static C Create()
        {
            var disposables = new[] { new Disposable() };
            return new C(disposables[0]);
        }
    }
}";
                RoslynAssert.Valid(Analyzer, DisposableCode, testCode);
            }

            [Test]
            public void IgnoreDictionaryPassedInViaCtor()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.Collections.Concurrent;
    using System.IO;

    public class C
    {
        private readonly Stream current;

        public C(ConcurrentDictionary<int, Stream> streams)
        {
            this.current = streams[1];
        }
    }
}";
                RoslynAssert.Valid(Analyzer, testCode);
            }

            [Test]
            public void IgnorePassedInViaCtorUnderscore()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class C
    {
        private readonly IDisposable _bar;
        
        public C(IDisposable bar)
        {
            _bar = bar;
        }
    }
}";
                RoslynAssert.Valid(Analyzer, testCode);
            }

            [Test]
            public void IgnorePassedInViaCtorUnderscoreWhenClassIsDisposable()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class C : IDisposable
    {
        private readonly IDisposable _bar;
        
        public C(IDisposable bar)
        {
            _bar = bar;
        }

        public void Dispose()
        {
        }
    }
}";
                RoslynAssert.Valid(Analyzer, testCode);
            }

            [Test]
            public void AssignedWithCreatedAndInjected()
            {
                var testCode = @"
#pragma warning disable IDISP008
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class C
    {
        private readonly IDisposable disposable;

        public C()
        {
            this.disposable = File.OpenRead(string.Empty);
        }

        public C(IDisposable disposable)
        {
            this.disposable = disposable;
        }
    }
}";
                RoslynAssert.Valid(Analyzer, testCode);
            }
        }
    }
}
