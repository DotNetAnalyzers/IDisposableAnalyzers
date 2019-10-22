namespace IDisposableAnalyzers
{
    using System;
    using System.Diagnostics;
    using System.Linq;

    internal class SmallSetDebugView<T>
    {
        private readonly SmallSet<T> set;

        internal SmallSetDebugView(SmallSet<T> set)
        {
            this.set = set ?? throw new ArgumentNullException(nameof(set));
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        internal T[] Items => this.set.ToArray();
    }
}
