namespace IDisposableAnalyzers;

using System.Collections.Immutable;

using Gu.Roslyn.AnalyzerExtensions;

internal class IHostedServiceType : QualifiedType
{
    internal readonly QualifiedMethod StartAsync;
    internal readonly QualifiedMethod StopAsync;

    internal IHostedServiceType()
        : base("Microsoft.Extensions.Hosting.IHostedService")
    {
        var parameters = ImmutableArray.Create(QualifiedParameter.Create(new QualifiedType("System.Threading.CancellationToken")));
        this.StartAsync = new QualifiedOverload(this, nameof(this.StartAsync), parameters);
        this.StopAsync = new QualifiedOverload(this, nameof(this.StopAsync), parameters);
    }
}
