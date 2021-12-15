namespace IDisposableAnalyzers;

using Gu.Roslyn.AnalyzerExtensions;

internal class WebApplication : QualifiedType
{
    internal readonly QualifiedMethod Run;
    internal readonly QualifiedMethod RunAsync;

    internal WebApplication()
        : base("Microsoft.AspNetCore.Builder.WebApplication")
    {
        this.Run = new QualifiedMethod(this,      nameof(this.Run));
        this.RunAsync = new QualifiedMethod(this, nameof(this.RunAsync));
    }
}
