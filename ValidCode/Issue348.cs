namespace ValidCode
{
    using System;
    using System.Collections.ObjectModel;

    public sealed class Issue348 : IDisposable
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
        }
    }
}


