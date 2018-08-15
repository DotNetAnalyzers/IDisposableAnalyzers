namespace IDisposableAnalyzers
{
    using System.Collections.Immutable;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class MethodReturnValuesAnalyzer : DiagnosticAnalyzer
    {
        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
            IDISP015DontReturnCachedAndCreated.Descriptor);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(Handle, SyntaxKind.MethodDeclaration, SyntaxKind.GetAccessorDeclaration);
        }

        private static void Handle(SyntaxNodeAnalysisContext context)
        {
            if (context.ContainingSymbol is IMethodSymbol method &&
                context.Node is MethodDeclarationSyntax methodDeclaration &&
                Disposable.IsAssignableFrom(method.ReturnType, context.Compilation))
            {
                using (var walker = ReturnValueWalker.Borrow(context.Node, ReturnValueSearch.RecursiveInside, context.SemanticModel, context.CancellationToken))
                {
                    if (walker.TryFirst(IsCreated, out _) &&
                        walker.TryFirst(IsCachedOrInjected, out _))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(IDISP015DontReturnCachedAndCreated.Descriptor, methodDeclaration.Identifier.GetLocation()));
                    }
                }
            }

            bool IsCreated(ExpressionSyntax expression)
            {
                return Disposable.IsCreation(expression, context.SemanticModel, context.CancellationToken) == Result.Yes ||
                       Disposable.IsAlreadyAssignedWithCreated(expression, context.SemanticModel, context.CancellationToken, out _) == Result.Yes;
            }

            bool IsCachedOrInjected(ExpressionSyntax expression)
            {
                return Disposable.IsCachedOrInjected(expression, context.Node, context.SemanticModel, context.CancellationToken);
            }
        }
    }
}
