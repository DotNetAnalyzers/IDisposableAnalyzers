﻿namespace IDisposableAnalyzers.Test.IDISP002DisposeMemberTests;

using Gu.Roslyn.Asserts;
using NUnit.Framework;

public static partial class Valid
{
    public static class Injected
    {
        [Test]
        public static void IgnoreAssignedWithCtorArgument()
        {
            var code = @"
namespace N
{
    using System;

    public sealed class C
    {
        private readonly IDisposable disposable;
        
        public C(IDisposable disposable)
        {
            this.disposable = disposable;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void IgnoreAssignedWithCtorArgumentIndexer()
        {
            var code = @"
namespace N
{
    using System;

    public sealed class C
    {
        private readonly IDisposable disposable;
        
        public C(IDisposable[] disposables)
        {
            this.disposable = disposables[0];
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void IgnoreInjectedAndCreatedPropertyWhenFactoryTouchesIndexer()
        {
            var code = @"
namespace N
{
    using System;

    public sealed class C
    {
        private readonly IDisposable disposable;

        public C(IDisposable disposable)
        {
            this.disposable = disposable;
        }

        public static C Create()
        {
            var disposables = new[] { new Disposable() };
            return new C(disposables[0]);
        }
    }
}";
            RoslynAssert.Valid(Analyzer, DisposableCode, code);
        }

        [Test]
        public static void IgnoreDictionaryPassedInViaCtor()
        {
            var code = @"
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
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void IgnorePassedInViaCtorUnderscore()
        {
            var code = @"
namespace N
{
    using System;

    public sealed class C
    {
        private readonly IDisposable _disposable;
        
        public C(IDisposable disposable)
        {
            _disposable = disposable;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void IgnorePassedInViaCtorUnderscoreWhenClassIsDisposable()
        {
            var code = @"
namespace N
{
    using System;

    public sealed class C : IDisposable
    {
        private readonly IDisposable _disposable;
        
        public C(IDisposable disposable)
        {
            _disposable = disposable;
        }

        public void Dispose()
        {
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void AssignedWithCreatedAndInjected()
        {
            var code = @"
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
            RoslynAssert.Valid(Analyzer, code);
        }
    }
}
