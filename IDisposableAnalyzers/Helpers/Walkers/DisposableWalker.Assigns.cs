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
            if (localOrParameter.TryGetScope(cancellationToken, out var scope))
            {
                return Assigns(new Target<SyntaxNode, ISymbol, SyntaxNode>(null!, localOrParameter.Symbol, scope), semanticModel, cancellationToken, visited, out first);
            }

            return false;
        }

        private static bool Assigns<TSource, TSymbol, TNode>(Target<TSource, TSymbol, TNode> target, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<(string Caller, SyntaxNode Node)>? visited, out FieldOrProperty fieldOrProperty)
              where TSource : SyntaxNode
              where TSymbol : class, ISymbol
              where TNode : SyntaxNode
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
                    when semanticModel.TryGetSymbol(variableDeclarator, cancellationToken, out ILocalSymbol? local) &&
                         local.TryGetScope(cancellationToken, out var scope):
                    return Assigns(new Target<VariableDeclaratorSyntax, ILocalSymbol, SyntaxNode>(variableDeclarator, local, scope), semanticModel, cancellationToken, visited, out fieldOrProperty);

                default:
                    return false;
            }
        }
    }
}
