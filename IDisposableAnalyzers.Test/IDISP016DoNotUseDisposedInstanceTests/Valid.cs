namespace IDisposableAnalyzers.Test.IDISP016DoNotUseDisposedInstanceTests
{
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
    using System.Collections.ObjectModel;

    public sealed class C : IDisposable
    {
        private bool disposed;

        public ObservableCollection<Disposable> Disposables1 { get; } = new();

        public ObservableCollection<Disposable> Disposables2 { get; } = new();

        public void M()
        {
            foreach (var disposable in this.Disposables1)
            {
                disposable.Dispose();
            }

            this.Disposables1.Clear();

            foreach (var disposable in this.Disposables2)
            {
                disposable.Dispose();
            }

            this.Disposables2.Clear();
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

            if (this.Disposables2.Count > 0)
            {
                foreach (var disposable in this.Disposables2)
                {
                    _ = disposable.ToString();
                }
            }

            this.Disposables2.Clear();
        }
    }
}";
            RoslynAssert.Valid(Analyzer, Descriptor, DisposableCode, code);
        }
    }
}
