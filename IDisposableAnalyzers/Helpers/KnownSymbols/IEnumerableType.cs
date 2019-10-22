namespace IDisposableAnalyzers
{
    using Gu.Roslyn.AnalyzerExtensions;

    // ReSharper disable once InconsistentNaming
    internal class IEnumerableType : QualifiedType
    {
        internal readonly QualifiedMethod GetEnumerator;

        internal IEnumerableType()
            : base("System.Collections.IEnumerable")
        {
            this.GetEnumerator = new QualifiedMethod(this, nameof(this.GetEnumerator));
        }
    }
}
