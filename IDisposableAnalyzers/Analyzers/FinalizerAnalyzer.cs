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
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
            Descriptors.IDISP022DisposeFalse,
            Descriptors.IDISP023ReferenceTypeInFinalizerContext);

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
                if (DisposeMethod.TryFindDisposeBoolCall(methodDeclaration, out _, out var isDisposing) &&
                    isDisposing.Expression?.IsKind(SyntaxKind.FalseLiteralExpression) != true)
                {
                    context.ReportDiagnostic(Diagnostic.Create(Descriptors.IDISP022DisposeFalse, isDisposing.GetLocation()));
                }

                using var walker = FinalizerContextWalker.Borrow(methodDeclaration, context.SemanticModel, context.CancellationToken);
                foreach (var node in walker.UsedReferenceTypes)
                {
                    context.ReportDiagnostic(Diagnostic.Create(Descriptors.IDISP023ReferenceTypeInFinalizerContext, node.GetLocation()));
                }
            }
        }
    }
}
