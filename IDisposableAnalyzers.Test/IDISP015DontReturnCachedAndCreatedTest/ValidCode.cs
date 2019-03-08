namespace IDisposableAnalyzers.Test.IDISP015DontReturnCachedAndCreatedTest
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public class ValidCode
    {
        private static readonly DiagnosticAnalyzer Analyzer = new MethodReturnValuesAnalyzer();
        private static readonly DiagnosticDescriptor Descriptor = IDISP015DontReturnCachedAndCreated.Descriptor;

        private const string DisposableCode = @"
namespace RoslynSandbox
{
    using System;

    public class Disposable : IDisposable
    {
        public Disposable(string meh)
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
        public void WhenRetuningCreated()
        {
            var testCode = @"
namespace RoslynSandbox
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
            AnalyzerAssert.Valid(Analyzer, Descriptor, testCode);
        }

        [Test]
        public void WhenRetuningInjected()
        {
            var testCode = @"
namespace RoslynSandbox
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
            AnalyzerAssert.Valid(Analyzer, Descriptor, testCode);
        }

        [Test]
        public void ReturningCachedInDictionary()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
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
            AnalyzerAssert.Valid(Analyzer, Descriptor, testCode);
        }

        [Test]
        public void CreatedAndDisposableEmpty()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    class C
    {
        public IDisposable M(bool b) => b ? new Disposable() : System.Reactive.Disposables.Disposable.Empty;
    }
}";
            AnalyzerAssert.Valid(Analyzer, DisposableCode, testCode);
        }

        [Test]
        public void CreatedAndNopDisposable()
        {
            var testCode = @"
namespace RoslynSandbox
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
            AnalyzerAssert.Valid(Analyzer, DisposableCode, testCode);
        }
    }
}
