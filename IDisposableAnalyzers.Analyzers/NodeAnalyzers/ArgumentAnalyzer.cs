namespace IDisposableAnalyzers
{
    using System.Collections.Immutable;
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
            IDISP001DisposeCreated.Descriptor,
            IDISP003DisposeBeforeReassigning.Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(HandleArgument, SyntaxKind.Argument);
        }

        private static void HandleArgument(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            if (context.Node is ArgumentSyntax argument &&
                argument.Parent is ArgumentListSyntax argumentList &&
                argumentList.Parent is InvocationExpressionSyntax invocation &&
                argument.RefOrOutKeyword.IsEitherKind(SyntaxKind.RefKeyword, SyntaxKind.OutKeyword) &&
                context.SemanticModel.GetSymbolSafe(invocation, context.CancellationToken) is IMethodSymbol method &&
                method.TrySingleDeclaration(context.CancellationToken, out BaseMethodDeclarationSyntax declaration) &&
                method.TryFindParameter(argument, out var parameter) &&
                Disposable.IsPotentiallyAssignableTo(parameter.Type))
            {
                if (Disposable.IsCreation(argument, context.SemanticModel, context.CancellationToken).IsEither(Result.Yes, Result.AssumeYes) &&
                    !Disposable.IsAddedToFieldOrProperty(parameter, declaration, context.SemanticModel, context.CancellationToken) &&
                    context.SemanticModel.GetSymbolSafe(argument.Expression, context.CancellationToken) is ISymbol symbol)
                {
                    if (Disposable.IsAssignedWithCreated(symbol, invocation, context.SemanticModel, context.CancellationToken).IsEither(Result.Yes, Result.AssumeYes) &&
                        !Disposable.IsDisposedBefore(symbol, argument.Expression, context.SemanticModel, context.CancellationToken))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(IDISP003DisposeBeforeReassigning.Descriptor, argument.GetLocation()));
                    }

                    if (TryGetSymbol(argument, context, out var assignedSymbol))
                    {
                        if (assignedSymbol is ILocalSymbol assignedLocal &&
                            Disposable.ShouldDispose(assignedLocal, argument, context.SemanticModel, context.CancellationToken))
                        {
                            context.ReportDiagnostic(Diagnostic.Create(IDISP001DisposeCreated.Descriptor, argument.GetLocation()));
                        }

                        if (assignedSymbol is IParameterSymbol assignedParameter &&
                            assignedParameter.RefKind == RefKind.None &&
                            Disposable.ShouldDispose(assignedParameter, argument, context.SemanticModel, context.CancellationToken))
                        {
                            context.ReportDiagnostic(Diagnostic.Create(IDISP001DisposeCreated.Descriptor, argument.GetLocation()));
                        }
                    }
                }
            }
        }

        private static bool TryGetSymbol(ArgumentSyntax argument, SyntaxNodeAnalysisContext context, out ISymbol symbol)
        {
            if (argument.Expression is IdentifierNameSyntax candidate)
            {
                return context.SemanticModel.TryGetSymbol(candidate, context.CancellationToken, out symbol);
            }

            if (argument.Expression is DeclarationExpressionSyntax declarationExpression &&
                declarationExpression.Designation is SingleVariableDesignationSyntax singleVariable)
            {
                return context.SemanticModel.TryGetSymbol(singleVariable, context.CancellationToken, out symbol);
            }

            symbol = null;
            return false;
        }
    }
}
