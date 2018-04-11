namespace IDisposableAnalyzers
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CacheAnalyzer : DiagnosticAnalyzer
    {
        internal static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: "CacheAnalyzer",
            title: "CacheAnalyzer",
            messageFormat: "CacheAnalyzer",
            category: "CacheAnalyzer",
            defaultSeverity: DiagnosticSeverity.Hidden,
            isEnabledByDefault: AnalyzerConstants.EnabledByDefault,
            description: "CacheAnalyzer for start & purge cache");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);

        public override void Initialize(AnalysisContext context)
        {
            RegisterCacheScope<SyntaxTree, SemanticModel>(context);
        }

        private static void RegisterCacheScope<TKey, TValue>(AnalysisContext context)
        {
#pragma warning disable RS1013 // Start action has no registered non-end actions.
            context.RegisterCompilationStartAction(x =>
#pragma warning restore RS1013 // Start action has no registered non-end actions.
            {
                Cache<TKey, TValue>.Begin();
                x.RegisterCompilationEndAction(_ => Cache<TKey, TValue>.End());
            });
        }
    }
}
