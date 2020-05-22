namespace IDisposableAnalyzers
{
    using Gu.Roslyn.AnalyzerExtensions;

    internal class DisposableMixins : QualifiedType
    {
        internal readonly QualifiedMethod DisposeWith;

        internal DisposableMixins()
            : base("System.Reactive.Disposables.DisposableMixins")
        {
            this.DisposeWith = new QualifiedMethod(this, nameof(this.DisposeWith));
        }
    }
}
