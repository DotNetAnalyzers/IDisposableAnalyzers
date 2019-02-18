// ReSharper disable All
namespace ValidCode
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Reactive.Disposables;

    public class Misc : IDisposable
    {
        private static readonly PropertyChangedEventArgs IsDirtyPropertyChangedEventArgs = new PropertyChangedEventArgs(nameof(IsDirty));
        private readonly SingleAssignmentDisposable subscription = new SingleAssignmentDisposable();
        private readonly CompositeDisposable compositeDisposable = new CompositeDisposable();
        private readonly Lazy<IDisposable> lazyDisposable;

        private IDisposable meh1;
        private IDisposable meh2;
        private bool isDirty;
        private IDisposable disposable;

        public Misc(IDisposable disposable)
        {
            this.subscription.Disposable = File.OpenRead(string.Empty);
            this.disposable = Bar(disposable);
            using (var temp = this.CreateDisposableProperty)
            {
            }

            using (var temp = this.CreateDisposable())
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

        public static IDisposable AssignLocalInSwitch(int i)
        {
            IDisposable result;
            if (i == 0)
            {
                result = null;
            }
            else
            {
                switch (i)
                {
                    case 1:
                        result = File.OpenRead(string.Empty);
                        break;
                    case 2:
                        result = File.OpenRead(string.Empty);
                        break;
                    default:
                        result = null;
                        break;
                }
            }

            return result;
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

        public static void Touch(string fileName)
        {
            File.Create(fileName).Dispose();
            File.Create(fileName)?.Dispose();
        }

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
