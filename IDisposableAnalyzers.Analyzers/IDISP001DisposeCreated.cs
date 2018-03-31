namespace IDisposableAnalyzers
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class IDISP001DisposeCreated : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "IDISP001";

        internal static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Dispose created.",
            messageFormat: "Dispose created.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: AnalyzerConstants.EnabledByDefault,
            description: "When you create a instance of a type that implements `IDisposable` you are responsible for disposing it.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(HandleDeclaration, SyntaxKind.LocalDeclarationStatement);
        }

        private static void HandleDeclaration(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            if (context.Node is LocalDeclarationStatementSyntax localDeclaration)
            {
                foreach (var declarator in localDeclaration.Declaration.Variables)
                {
                    if (declarator.Initializer is EqualsValueClauseSyntax initializer &&
                        initializer.Value is ExpressionSyntax value &&
                        Disposable.IsCreation(value, context.SemanticModel, context.CancellationToken).IsEither(Result.Yes, Result.AssumeYes) &&
                        context.SemanticModel.GetDeclaredSymbolSafe(declarator, context.CancellationToken) is ILocalSymbol local &&
                        Disposable.ShouldDispose(local, declarator, context.SemanticModel, context.CancellationToken))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(Descriptor, localDeclaration.GetLocation()));
                    }
                }
            }
        }
    }
}
