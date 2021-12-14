namespace ValidCode
{
    using System;
    using System.Collections.ObjectModel;

    public sealed class Issue348 : IDisposable
    {
        private bool disposed;

        public ObservableCollection<Disposable> Disposables { get; } = new();

        public void M()
        {
            foreach (var disposable in this.Disposables)
            {
                disposable.Dispose();
            }

            this.Disposables.Clear();
        }

        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            foreach (var disposable in this.Disposables)
            {
                disposable.Dispose();
            }

            this.Disposables.Clear();
        }
    }
}


