// ReSharper disable UnusedMember.Global Used in HappyPathWithAll.PropertyChangedAnalyzersSln
// ReSharper disable NotAccessedField.Local
// ReSharper disable UnusedVariable
namespace IDisposableAnalyzers.Test.HappyPathCode
{
    using System;
    using System.ComponentModel;
    using System.IO;
    using System.Reactive.Disposables;

    public class Foo1 : IDisposable
    {
        private static readonly PropertyChangedEventArgs IsDirtyPropertyChangedEventArgs = new PropertyChangedEventArgs(nameof(IsDirty));
        private readonly SingleAssignmentDisposable subscription = new SingleAssignmentDisposable();
        private readonly CompositeDisposable compositeDisposable = new CompositeDisposable();

        private IDisposable meh1;
        private IDisposable meh2;
        private bool isDirty;

        public Foo1()
        {
            this.meh1 = this.RecursiveProperty;
            this.meh2 = this.RecursiveMethod();
            this.subscription.Disposable = File.OpenRead(string.Empty);
        }

        public event PropertyChangedEventHandler PropertyChanged
        {
            add { this.PropertyChangedCore += value; }
            remove { this.PropertyChangedCore -= value; }
        }

        private event PropertyChangedEventHandler PropertyChangedCore;

        public Disposable RecursiveProperty => this.RecursiveProperty;

        public IDisposable Disposable => this.subscription.Disposable;

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

        public Disposable RecursiveMethod() => this.RecursiveMethod();

        public void Meh()
        {
            using (var item = new Disposable())
            {
            }

            using (var item = this.RecursiveProperty)
            {
            }

            using (this.RecursiveProperty)
            {
            }

            using (var item = this.RecursiveMethod())
            {
            }

            using (this.RecursiveMethod())
            {
            }
        }

        public void Dispose()
        {
            this.subscription.Dispose();
            this.compositeDisposable.Dispose();
        }

        internal string AddAndReturnToString()
        {
            return this.compositeDisposable.AddAndReturn(new Disposable()).ToString();
        }
    }
}
