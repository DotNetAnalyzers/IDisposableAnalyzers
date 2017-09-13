namespace IDisposableAnalyzers
{
    // ReSharper disable once InconsistentNaming
    internal class IEnumerableOfTType : QualifiedType
    {
        internal readonly QualifiedMethod GetEnumerator;

        public IEnumerableOfTType()
            : base("System.Collections.Generic.IEnumerable`1")
        {
            this.GetEnumerator = new QualifiedMethod(this, nameof(this.GetEnumerator));
        }
    }
}