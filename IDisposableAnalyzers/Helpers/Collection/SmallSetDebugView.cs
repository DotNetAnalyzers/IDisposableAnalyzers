namespace IDisposableAnalyzers
{
    using System;
    using System.Diagnostics;
    using System.Linq;

    internal class SmallSetDebugView<T>
    {
        private readonly SmallSet<T> set;

        public SmallSetDebugView(SmallSet<T> set)
        {
            this.set = set ?? throw new ArgumentNullException(nameof(set));
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items => this.set.ToArray();
    }
}
