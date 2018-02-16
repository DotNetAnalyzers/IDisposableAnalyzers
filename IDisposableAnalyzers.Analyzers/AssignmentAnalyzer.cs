namespace IDisposableAnalyzers
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class AssignmentAnalyzer : DiagnosticAnalyzer
    {
        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(IDISP008DontMixInjectedAndCreatedForMember.Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(HandleAssignment, SyntaxKind.SimpleAssignmentExpression);
        }

        private static void HandleAssignment(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            if (context.Node is AssignmentExpressionSyntax assignment)
            {
                if (context.SemanticModel.GetSymbolSafe(assignment.Left, context.CancellationToken) is IParameterSymbol parameter &&
                    parameter.ContainingSymbol.DeclaredAccessibility != Accessibility.Private &&
                    parameter.RefKind == RefKind.Ref &&
                    Disposable.IsAssignableTo(context.SemanticModel.GetTypeInfoSafe(assignment.Right, context.CancellationToken).Type))
                {
                    context.ReportDiagnostic(Diagnostic.Create(IDISP008DontMixInjectedAndCreatedForMember.Descriptor, context.Node.GetLocation()));
                }
            }
        }
    }
}
