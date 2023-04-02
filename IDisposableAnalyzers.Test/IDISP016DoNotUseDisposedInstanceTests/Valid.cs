namespace IDisposableAnalyzers.Test.IDISP016DoNotUseDisposedInstanceTests;

using Gu.Roslyn.Asserts;
using Microsoft.CodeAnalysis;
using NUnit.Framework;

public static partial class Valid
{
    private static readonly DisposeCallAnalyzer Analyzer = new();
    private static readonly DiagnosticDescriptor Descriptor = Descriptors.IDISP016DoNotUseDisposedInstance;

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
    public static void Issue348()
    {
        var code = @"
namespace N
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public sealed class C : IDisposable
    {
        private bool disposed;

        public List<IDisposable> Disposables1 { get; } = new();

        public List<Disposable> Disposables2 { get; } = new();

        public void M()
        {
            foreach (var disposable in this.Disposables1)
            {
                disposable.Dispose();
            }

            this.Disposables1.Clear();

            if (this.Disposables2.Count > 0)
            {
                foreach (var disposable in this.Disposables2)
                {
                    this.Disposables1.AddRange(new []{disposable}.Select(x => new Wrapper(disposable)));
                }
            }
        }

        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            foreach (var disposable in this.Disposables1)
            {
                disposable.Dispose();
            }

            this.Disposables1.Clear();
        }

        private sealed class Wrapper : IDisposable
        {
            private readonly Disposable disposable;

            public Wrapper(Disposable disposable)
            {
                this.disposable = disposable;
            }

            public void Dispose()
            {
            }
        }
    }
}";
        RoslynAssert.Valid(Analyzer, Descriptor, DisposableCode, code);
    }
}
