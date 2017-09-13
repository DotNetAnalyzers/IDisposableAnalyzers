namespace IDisposableAnalyzers
{
    // ReSharper disable once InconsistentNaming
    internal class IEnumerableType : QualifiedType
    {
        internal readonly QualifiedMethod GetEnumerator;

        public IEnumerableType()
            : base("System.Collections.IEnumerable")
        {
            this.GetEnumerator = new QualifiedMethod(this, nameof(this.GetEnumerator));
        }
    }
}