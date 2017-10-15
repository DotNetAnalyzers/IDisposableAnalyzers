namespace IDisposableAnalyzers
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;

    internal abstract class PooledWalker<T> : CSharpSyntaxWalker, IDisposable
        where T : PooledWalker<T>
    {
        private static readonly ConcurrentQueue<PooledWalker<T>> Cache = new ConcurrentQueue<PooledWalker<T>>();
        private int refCount;

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

            walker.refCount = 1;
            return (T)walker;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.refCount--;
                Debug.Assert(this.refCount >= 0, "refCount>= 0");
                if (this.refCount == 0)
                {
                    this.Clear();
                    Cache.Enqueue(this);
                }
            }
        }

        protected abstract void Clear();

        [Conditional("DEBUG")]
        protected void ThrowIfDisposed()
        {
            if (this.refCount <= 0)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }
    }
}