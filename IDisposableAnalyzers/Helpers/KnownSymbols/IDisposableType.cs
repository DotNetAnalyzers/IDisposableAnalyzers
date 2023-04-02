namespace IDisposableAnalyzers;

using Gu.Roslyn.AnalyzerExtensions;

internal class IDisposableType : QualifiedType
{
    internal readonly QualifiedMethod Dispose;

    internal IDisposableType()
        : base("System.IDisposable")
    {
        this.Dispose = new QualifiedMethod(this, nameof(this.Dispose));
    }
}
