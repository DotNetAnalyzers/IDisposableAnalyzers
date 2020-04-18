namespace IDisposableAnalyzers
{
    using Gu.Roslyn.AnalyzerExtensions;

    internal class HostingAbstractionsHostExtensionsType : QualifiedType
    {
        internal readonly QualifiedMethod Run;
        internal readonly QualifiedMethod RunAsync;

        internal HostingAbstractionsHostExtensionsType()
            : base("Microsoft.Extensions.Hosting.HostingAbstractionsHostExtensions")
        {
            this.Run = new QualifiedMethod(this, nameof(this.Run));
            this.RunAsync = new QualifiedMethod(this, nameof(this.RunAsync));
        }
    }
}
