namespace IDisposableAnalyzers;

using Gu.Roslyn.AnalyzerExtensions;

internal class HttpResponseType : QualifiedType
{
    internal readonly QualifiedMethod RegisterForDispose;
    internal readonly QualifiedMethod RegisterForDisposeAsync;

    internal HttpResponseType()
        : base("Microsoft.AspNetCore.Http.HttpResponse")
    {
        this.RegisterForDispose = new QualifiedMethod(this, nameof(this.RegisterForDispose));
        this.RegisterForDisposeAsync = new QualifiedMethod(this,  nameof(this.RegisterForDisposeAsync));
    }
}
