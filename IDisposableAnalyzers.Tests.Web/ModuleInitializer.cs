namespace IDisposableAnalyzers.Tests.Web
{
    using System.Runtime.CompilerServices;
    using Gu.Roslyn.Asserts;

    internal static class ModuleInitializer
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            Settings.Default = Settings.Default
                                       .WithMetadataReferences(
                                           MetadataReferences.Transitive(
                                               typeof(ModuleInitializer),
                                               typeof(Microsoft.Extensions.Hosting.GenericHostBuilderExtensions),
                                               typeof(Microsoft.Extensions.Logging.ApplicationInsightsLoggerFactoryExtensions),
                                               typeof(Microsoft.Extensions.DependencyInjection.MvcServiceCollectionExtensions),
                                               typeof(Microsoft.AspNetCore.Builder.HttpsPolicyBuilderExtensions),
                                               typeof(Microsoft.AspNetCore.Builder.AuthorizationAppBuilderExtensions),
                                               typeof(Microsoft.AspNetCore.Builder.DeveloperExceptionPageExtensions)));
        }
    }
}
