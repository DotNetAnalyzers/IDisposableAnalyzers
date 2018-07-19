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
// ReSharper disable NotAccessedVariable
// ReSharper disable InlineOutVariableDeclaration
#pragma warning disable 1717
#pragma warning disable SA1101 // Prefix local calls with this
#pragma warning disable GU0011 // Don't ignore the return value.
#pragma warning disable GU0010 // Assigning same value.
#pragma warning disable IDE0009 // Member access should be qualified.
#pragma warning disable IDE0044
#pragma warning disable GU0021 // Calculated property allocates reference type.
#pragma warning disable 169
namespace IDisposableAnalyzers.Test.HappyPathCode
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Reactive.Disposables;

    public class Foo1 : IDisposable
    {
        private static readonly PropertyChangedEventArgs IsDirtyPropertyChangedEventArgs = new PropertyChangedEventArgs(nameof(IsDirty));
        private readonly SingleAssignmentDisposable subscription = new SingleAssignmentDisposable();
        private readonly CompositeDisposable compositeDisposable = new CompositeDisposable();
        private readonly Lazy<IDisposable> lazyDisposable;

        private IDisposable meh1;
        private IDisposable meh2;
        private bool isDirty;
        private IDisposable disposable;

        public Foo1(IDisposable disposable)
        {
            this.subscription.Disposable = File.OpenRead(string.Empty);
            this.disposable = Bar(disposable);
            using (var temp = CreateDisposableProperty)
            {
            }

            using (var temp = CreateDisposable())
            {
            }

            this.lazyDisposable = new Lazy<IDisposable>(() =>
            {
                var temp = new Disposable();
                return temp;
            });
        }

        public event PropertyChangedEventHandler PropertyChanged
        {
            add { this.PropertyChangedCore += value; }
            remove { this.PropertyChangedCore -= value; }
        }

        private event PropertyChangedEventHandler PropertyChangedCore;

        public IDisposable Disposable => this.subscription.Disposable;

#pragma warning disable IDISP012 // Property should not return created disposable.
        public IDisposable CreateDisposableProperty => new Disposable();
#pragma warning restore IDISP012 // Property should not return created disposable.

        public string Text => this.AddAndReturnToString();

        public bool IsDirty
        {
            get
            {
                return this.isDirty;
            }

            private set
            {
                if (value == this.isDirty)
                {
                    return;
                }

                this.isDirty = value;
                this.PropertyChangedCore?.Invoke(this, IsDirtyPropertyChangedEventArgs);
            }
        }

        public void Dispose()
        {
            this.subscription.Dispose();
            this.compositeDisposable.Dispose();
            if (this.lazyDisposable.IsValueCreated)
            {
                this.lazyDisposable.Value.Dispose();
            }
        }

        public IDisposable CreateDisposable() => new Disposable();

        internal string AddAndReturnToString()
        {
            return this.compositeDisposable.AddAndReturn(new Disposable()).ToString();
        }

        private static IDisposable Bar(IDisposable disposable, IEnumerable<IDisposable> disposables = null)
        {
            if (disposables == null)
            {
                return Bar(disposable, new[] { disposable });
            }

            return disposable;
        }
    }
}
