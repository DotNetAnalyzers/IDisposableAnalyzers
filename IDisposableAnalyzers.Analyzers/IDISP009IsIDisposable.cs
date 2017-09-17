namespace IDisposableAnalyzers
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class IDISP009IsIDisposable : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "IDISP009";

        private static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Add IDisposable interface.",
            messageFormat: "Add IDisposable interface.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: AnalyzerConstants.EnabledByDefault,
            description: "The type has a Dispose method but does not implement `IDisposable`.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(HandleMethod, SyntaxKind.MethodDeclaration);
        }

        private static void HandleMethod(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            var method = (IMethodSymbol)context.ContainingSymbol;
            if (method.IsStatic ||
                method.Name != "Dispose" ||
                !method.ReturnsVoid ||
                method.Parameters.Length != 0)
            {
                return;
            }

            var containingType = method.ContainingType;
            if (!Disposable.IsAssignableTo(containingType))
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptor, context.Node.GetLocation()));
            }
        }
    }
}