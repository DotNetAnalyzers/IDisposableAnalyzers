namespace IDisposableAnalyzers
{
    using System.Collections.Immutable;
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class ArgumentAnalyzer : DiagnosticAnalyzer
    {
        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
            Descriptors.IDISP001DisposeCreated,
            Descriptors.IDISP003DisposeBeforeReassigning);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(c => Handle(c), SyntaxKind.Argument);
        }

        private static void Handle(SyntaxNodeAnalysisContext context)
        {
            if (!context.IsExcludedFromAnalysis() &&
                context.Node is ArgumentSyntax argument &&
                argument.Parent is ArgumentListSyntax argumentList &&
                argumentList.Parent is InvocationExpressionSyntax invocation &&
                argument.RefOrOutKeyword.IsEither(SyntaxKind.RefKeyword, SyntaxKind.OutKeyword) &&
                IsCreation(argument, context.SemanticModel, context.CancellationToken) &&
                context.SemanticModel.TryGetSymbol(argument.Expression, context.CancellationToken, out ISymbol symbol))
            {
                if (symbol.Kind == SymbolKind.Discard ||
                    (LocalOrParameter.TryCreate(symbol, out var localOrParameter) &&
                     DisposableWalker.ShouldDispose(localOrParameter, context.SemanticModel, context.CancellationToken)))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Descriptors.IDISP001DisposeCreated, argument.GetLocation()));
                }

                if (Disposable.IsAssignedWithCreated(symbol, invocation, context.SemanticModel, context.CancellationToken).IsEither(Result.Yes, Result.AssumeYes) &&
                    !Disposable.IsDisposedBefore(symbol, invocation, context.SemanticModel, context.CancellationToken))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Descriptors.IDISP003DisposeBeforeReassigning, argument.GetLocation()));
                }
            }
        }

        private static bool IsCreation(ArgumentSyntax candidate, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (candidate.Parent is ArgumentListSyntax argumentList &&
                argumentList.Parent is InvocationExpressionSyntax invocation &&
                semanticModel.TryGetSymbol(invocation, cancellationToken, out var method) &&
                method.TryFindParameter(candidate, out var parameter) &&
                Disposable.IsPotentiallyAssignableFrom(parameter.Type, semanticModel.Compilation))
            {
                using (var assignedValues = AssignedValueWalker.Borrow(candidate.Expression, semanticModel, cancellationToken))
                {
                    using (var recursive = RecursiveValues.Borrow(assignedValues, semanticModel, cancellationToken))
                    {
                        return Disposable.IsAnyCreation(recursive, semanticModel, cancellationToken).IsEither(Result.Yes, Result.AssumeYes) &&
                               !Disposable.IsAnyCachedOrInjected(recursive, semanticModel, cancellationToken).IsEither(Result.Yes, Result.AssumeYes);
                    }
                }
            }

            return false;
        }
    }
}
