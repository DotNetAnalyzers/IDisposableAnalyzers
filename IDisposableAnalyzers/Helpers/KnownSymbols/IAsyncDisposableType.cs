namespace IDisposableAnalyzers
{
    using Gu.Roslyn.AnalyzerExtensions;

    internal class IAsyncDisposableType : QualifiedType
    {
        internal readonly QualifiedMethod DisposeAsync;

        internal IAsyncDisposableType()
            : base("System.IAsyncDisposable")
        {
            this.DisposeAsync = new QualifiedMethod(this, nameof(this.DisposeAsync));
        }
    }
}
