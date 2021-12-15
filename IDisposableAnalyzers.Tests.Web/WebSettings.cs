namespace IDisposableAnalyzers.Tests.Web
{
    using System.Runtime.CompilerServices;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis;

    internal static class WebSettings
    {
        internal static readonly Settings Exe = Settings.Default.WithCompilationOptions(x => x.WithOutputKind(OutputKind.ConsoleApplication));

        [ModuleInitializer]
        internal static void Initialize()
        {
            Settings.Default = Settings.Default
                                       .WithMetadataReferences(
                                           MetadataReferences.Transitive(
                                               typeof(WebSettings),
                                               typeof(Microsoft.Extensions.Hosting.GenericHostBuilderExtensions),
                                               typeof(Microsoft.Extensions.Logging.ApplicationInsightsLoggerFactoryExtensions),
                                               typeof(Microsoft.Extensions.DependencyInjection.MvcServiceCollectionExtensions),
                                               typeof(Microsoft.AspNetCore.Builder.StaticFileExtensions),
                                               typeof(Microsoft.AspNetCore.Builder.HttpsPolicyBuilderExtensions),
                                               typeof(Microsoft.AspNetCore.Builder.AuthorizationAppBuilderExtensions),
                                               typeof(Microsoft.AspNetCore.Builder.DeveloperExceptionPageExtensions)));
        }
    }
}
