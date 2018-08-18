// ReSharper disable UnusedMember.Global Used in HappyPathWithAll.PropertyChangedAnalyzersSln
// ReSharper disable NotAccessedField.Local
// ReSharper disable UnusedVariable
// ReSharper disable UnusedParameter.Local
// ReSharper disable RedundantAssignment
// ReSharper disable ArrangeAccessorOwnerBody
// ReSharper disable FunctionRecursiveOnAllPaths
// ReSharper disable UnusedParameter.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ArrangeThisQualifier
// ReSharper disable PossibleUnintendedReferenceComparison
// ReSharper disable RedundantCheckBeforeAssignment
// ReSharper disable UnusedMethodReturnValue.Global
#pragma warning disable 1717
#pragma warning disable IDE0009 // Member access should be qualified.
namespace ValidCode
{
    using System;

    public sealed class LazyFoo : IDisposable
    {
        private readonly IDisposable created;
        private bool disposed;
        private IDisposable lazyDisposable;

        public LazyFoo(IDisposable injected)
        {
            this.Disposable = injected ?? (this.created = new Disposable());
        }

        public IDisposable Disposable { get; }

        public IDisposable LazyDisposable => this.lazyDisposable ?? (this.lazyDisposable = new Disposable());

        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            this.created?.Dispose();
            this.lazyDisposable?.Dispose();
        }
    }
}
