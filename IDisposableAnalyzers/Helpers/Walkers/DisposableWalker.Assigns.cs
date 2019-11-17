namespace IDisposableAnalyzers
{
    using System;
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed partial class DisposableWalker
    {
        internal static bool Assigns(LocalOrParameter localOrParameter, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<(string Caller, SyntaxNode Node)>? visited, out FieldOrProperty first)
        {
            using (var walker = CreateUsagesWalker(localOrParameter, semanticModel, cancellationToken))
            {
                foreach (var usage in walker.usages)
                {
                    if (Assigns(usage, semanticModel, cancellationToken, visited, out first))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool Assigns(SymbolAndDeclaration<IParameterSymbol, BaseMethodDeclarationSyntax> target, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<(string Caller, SyntaxNode Node)>? visited, out FieldOrProperty fieldOrProperty)
        {
            using (var walker = CreateUsagesWalker(target, semanticModel, cancellationToken))
            {
                foreach (var usage in walker.usages)
                {
                    if (Assigns(usage, semanticModel, cancellationToken, visited, out fieldOrProperty))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool Assigns(ExpressionSyntax candidate, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<(string Caller, SyntaxNode Node)>? visited, out FieldOrProperty fieldOrProperty)
        {
            switch (candidate.Parent.Kind())
            {
                case SyntaxKind.CastExpression:
                case SyntaxKind.AsExpression:
                case SyntaxKind.ConditionalExpression:
                case SyntaxKind.CoalesceExpression:
                    return Assigns((ExpressionSyntax)candidate.Parent, semanticModel, cancellationToken, visited, out fieldOrProperty);
            }

            switch (candidate.Parent)
            {
                case AssignmentExpressionSyntax { Left: { } left, Right: { } right }:
                    return right.Contains(candidate) &&
                           semanticModel.TryGetSymbol(left, cancellationToken, out var assignedSymbol) &&
                           FieldOrProperty.TryCreate(assignedSymbol, out fieldOrProperty);
                case ArgumentSyntax { Parent: ArgumentListSyntax { Parent: InvocationExpressionSyntax invocation } } argument
                    when invocation.IsPotentialThisOrBase() &&
                         Target(argument, semanticModel, cancellationToken, visited) is { } target:
                    return Assigns(target, semanticModel, cancellationToken, visited, out fieldOrProperty);
                case EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax variableDeclarator }
                when semanticModel.TryGetSymbol(variableDeclarator, cancellationToken, out var symbol) &&
                     LocalOrParameter.TryCreate(symbol, out var localOrParameter):
                    if (visited.CanVisit(candidate, out visited))
                    {
                        using (visited)
                        {
                            return Assigns(localOrParameter, semanticModel, cancellationToken, visited, out fieldOrProperty);
                        }
                    }

                    return false;

                default:
                    return false;
            }
        }
    }
}
