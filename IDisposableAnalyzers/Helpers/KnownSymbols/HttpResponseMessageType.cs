namespace IDisposableAnalyzers;

using Gu.Roslyn.AnalyzerExtensions;

internal class HttpResponseMessageType : QualifiedType
{
    internal readonly QualifiedMethod EnsureSuccessStatusCode;

    internal HttpResponseMessageType()
        : base("System.Net.Http.HttpResponseMessage")
    {
        this.EnsureSuccessStatusCode = new QualifiedMethod(this, nameof(this.EnsureSuccessStatusCode));
    }
}
