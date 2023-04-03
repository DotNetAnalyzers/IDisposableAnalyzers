namespace IDisposableAnalyzers;

using Gu.Roslyn.AnalyzerExtensions;

internal class ValueTaskType : QualifiedType
{
    internal readonly QualifiedMethod FromResult;
    internal readonly QualifiedMethod Run;
    internal readonly QualifiedMethod RunOfT;
    internal readonly QualifiedMethod ConfigureAwait;
    internal readonly QualifiedProperty CompletedTask;

    internal ValueTaskType()
        : base("System.Threading.Tasks.ValueTask")
    {
        this.FromResult = new QualifiedMethod(this, nameof(this.FromResult));
        this.ConfigureAwait = new QualifiedMethod(this, nameof(this.ConfigureAwait));
        this.CompletedTask = new QualifiedProperty(this, nameof(this.CompletedTask));
    }
}
