namespace IDisposableAnalyzers
{
    using System.Collections.Immutable;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class FinalizerAnalyzer : DiagnosticAnalyzer
    {
        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
            IDISP022DisposeFalse.Descriptor,
            IDISP023ReferenceTypeInFinalizerContext.Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(c => Handle(c), SyntaxKind.DestructorDeclaration);
        }

        private static void Handle(SyntaxNodeAnalysisContext context)
        {
            if (!context.IsExcludedFromAnalysis() &&
                context.Node is DestructorDeclarationSyntax methodDeclaration)
            {
                if (DisposeMethod.TryFindDisposeBoolCall(methodDeclaration, context.SemanticModel, context.CancellationToken, out _, out var isDisposing) &&
                    isDisposing.Expression?.IsKind(SyntaxKind.FalseLiteralExpression) != true)
                {
                    context.ReportDiagnostic(Diagnostic.Create(IDISP022DisposeFalse.Descriptor, isDisposing.GetLocation()));
                }

                using (var walker = FinalizerContextWalker.Borrow(methodDeclaration, context.SemanticModel, context.CancellationToken))
                {
                    foreach (var node in walker.ReferenceTypes)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(IDISP023ReferenceTypeInFinalizerContext.Descriptor, node.GetLocation()));
                    }
                }
            }
        }
    }
}
