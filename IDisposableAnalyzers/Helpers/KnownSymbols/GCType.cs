namespace IDisposableAnalyzers
{
    using Gu.Roslyn.AnalyzerExtensions;

    internal class GCType : QualifiedType
    {
        internal readonly QualifiedMethod SuppressFinalize;

        internal GCType()
            : base("System.GC")
        {
            this.SuppressFinalize = new QualifiedMethod(this, nameof(this.SuppressFinalize));
        }
    }
}
