namespace IDisposableAnalyzers.Test.IDISP015DoNotReturnCachedAndCreatedTest
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis;
    using NUnit.Framework;

    public static class Valid
    {
        private static readonly MethodReturnValuesAnalyzer Analyzer = new();
        private static readonly DiagnosticDescriptor Descriptor = Descriptors.IDISP015DoNotReturnCachedAndCreated;

        private const string DisposableCode = @"
namespace N
{
    using System;

    public class Disposable : IDisposable
    {
        public Disposable(string _)
            : this()
        {
        }

        public Disposable()
        {
        }

        public void Dispose()
        {
        }
    }
}";

        [Test]
        public static void WhenRetuningCreated()
        {
            var code = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C
    {
        public IDisposable M()
        {
            return File.OpenRead(string.Empty);
        }
    }
}";
            RoslynAssert.Valid(Analyzer, Descriptor, code);
        }

        [Test]
        public static void WhenRetuningInjected()
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

        public IDisposable M()
        {
            return this.disposable;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, Descriptor, code);
        }

        [Test]
        public static void ReturningCachedInDictionary()
        {
            var code = @"
namespace N
{
    using System.Collections.Generic;
    using System.IO;

    public class C
    {
        private readonly Dictionary<int, Stream> streams = new Dictionary<int, Stream>();

        public C()
        {
            this.streams[0] = File.OpenRead(string.Empty);
        }

        public Stream Get(int i)
        {
            return this.streams[i];
        }
    }
}";
            RoslynAssert.Valid(Analyzer, Descriptor, code);
        }

        [Test]
        public static void CreatedAndDisposableEmpty()
        {
            var code = @"
namespace N
{
    using System;

    class C
    {
        public IDisposable M(bool b) => b ? new Disposable() : System.Reactive.Disposables.Disposable.Empty;
    }
}";
            RoslynAssert.Valid(Analyzer, DisposableCode, code);
        }

        [Test]
        public static void CreatedAndNopDisposable()
        {
            var code = @"
namespace N
{
    using System;

    class C
    {
        public IDisposable M(bool b) => b ? new Disposable() : Empty.Default;

        private sealed class Empty : IDisposable
        {
            public static readonly IDisposable Default = new Empty();

            public void Dispose()
            {
            }
        }
    }
}";
            RoslynAssert.Valid(Analyzer, DisposableCode, code);
        }
    }
}
