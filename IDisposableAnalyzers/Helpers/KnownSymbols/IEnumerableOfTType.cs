namespace IDisposableAnalyzers;

using Gu.Roslyn.AnalyzerExtensions;

// ReSharper disable once InconsistentNaming
internal class IEnumerableOfTType : QualifiedType
{
    internal readonly QualifiedMethod GetEnumerator;

    internal IEnumerableOfTType()
        : base("System.Collections.Generic.IEnumerable`1")
    {
        this.GetEnumerator = new QualifiedMethod(this, nameof(this.GetEnumerator));
    }
}
