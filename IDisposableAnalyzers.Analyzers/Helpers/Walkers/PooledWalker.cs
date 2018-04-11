namespace IDisposableAnalyzers
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;

    internal abstract class PooledWalker<T> : CSharpSyntaxWalker, IDisposable
        where T : PooledWalker<T>
    {
        private static readonly ConcurrentQueue<PooledWalker<T>> Cache = new ConcurrentQueue<PooledWalker<T>>();

        public void Dispose()
        {
            this.Dispose(true);
        }

        protected static T BorrowAndVisit(SyntaxNode node, Func<T> create)
        {
            var walker = Borrow(create);
            walker.Visit(node);
            return walker;
        }

        protected static T Borrow(Func<T> create)
        {
            if (!Cache.TryDequeue(out var walker))
            {
                walker = create();
            }

            return (T)walker;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Debug.Assert(!Cache.Contains(this), "!Cache.Contains(this)");
                this.Clear();
                Cache.Enqueue(this);
            }
        }

        protected abstract void Clear();

        [Conditional("DEBUG")]
        protected void ThrowIfDisposed()
        {
            if (Cache.Contains(this))
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }
    }
}
