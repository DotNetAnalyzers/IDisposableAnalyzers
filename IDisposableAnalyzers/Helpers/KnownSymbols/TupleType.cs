namespace IDisposableAnalyzers;

using Gu.Roslyn.AnalyzerExtensions;

internal class TupleType : QualifiedType
{
    internal readonly QualifiedMethod Create;

    internal TupleType()
        : base("System.Tuple")
    {
        this.Create = new QualifiedMethod(this, nameof(this.Create));
    }
}
