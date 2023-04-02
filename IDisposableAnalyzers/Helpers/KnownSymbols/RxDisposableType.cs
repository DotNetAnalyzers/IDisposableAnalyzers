namespace IDisposableAnalyzers;

using Gu.Roslyn.AnalyzerExtensions;

internal class RxDisposableType : QualifiedType
{
    internal readonly QualifiedMethod Create;
    internal readonly QualifiedProperty Empty;

    internal RxDisposableType()
        : base("System.Reactive.Disposables.Disposable")
    {
        this.Create = new QualifiedMethod(this, nameof(this.Create));
        this.Empty = new QualifiedProperty(this, nameof(this.Empty));
    }
}
