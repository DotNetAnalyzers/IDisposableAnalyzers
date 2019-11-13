namespace IDisposableAnalyzers.Test.IDISP002DisposeMemberTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    public static partial class ValidCode
    {
        public static class Injected
        {
            [Test]
            public static void IgnoreAssignedWithCtorArgument()
            {
                var testCode = @"
namespace N
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
            public static void IgnoreAssignedWithCtorArgumentIndexer()
            {
                var testCode = @"
namespace N
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
            public static void IgnoreInjectedAndCreatedPropertyWhenFactoryTouchesIndexer()
            {
                var testCode = @"
namespace N
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
            public static void IgnoreDictionaryPassedInViaCtor()
            {
                var testCode = @"
namespace N
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
            public static void IgnorePassedInViaCtorUnderscore()
            {
                var testCode = @"
namespace N
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
            public static void IgnorePassedInViaCtorUnderscoreWhenClassIsDisposable()
            {
                var testCode = @"
namespace N
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
            public static void AssignedWithCreatedAndInjected()
            {
                var testCode = @"
#pragma warning disable IDISP008
namespace N
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
