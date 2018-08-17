using Gu.Roslyn.Asserts;

[assembly: MetadataReference(typeof(object), new[] { "global", "mscorlib" })]
[assembly: MetadataReference(typeof(System.Diagnostics.Debug), new[] { "global", "System" })]
[assembly: TransitiveMetadataReferences(typeof(Microsoft.CodeAnalysis.CSharp.CSharpCompilation))]
[assembly: TransitiveMetadataReferences(typeof(Microsoft.AspNetCore.Builder.IApplicationBuilder))]
[assembly: TransitiveMetadataReferences(typeof(Microsoft.AspNetCore.Hosting.IApplicationLifetime))]
[assembly: TransitiveMetadataReferences(typeof(Microsoft.Extensions.Logging.ILoggerFactory))]
[assembly: TransitiveMetadataReferences(typeof(Microsoft.Extensions.Logging.ApplicationInsightsLoggerFactoryExtensions))]
[assembly: MetadataReferences(
    typeof(System.Linq.Enumerable),
    typeof(System.Net.WebClient),
    typeof(System.Data.Common.DbConnection),
    typeof(System.Threading.Tasks.ValueTask),
    typeof(System.Xml.Serialization.XmlSerializer),
    typeof(Gu.Roslyn.AnalyzerExtensions.SyntaxTokenExt),
    typeof(Gu.Roslyn.CodeFixExtensions.Parse),
    typeof(NUnit.Framework.Assert))]
