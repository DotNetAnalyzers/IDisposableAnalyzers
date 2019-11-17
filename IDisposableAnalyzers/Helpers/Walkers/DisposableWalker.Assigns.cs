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
        internal static bool Assigns(LocalOrParameter localOrParameter, SemanticModel semanticModel, CancellationToken cancellationToken, out FieldOrProperty first)
        {
            if (localOrParameter.TryGetScope(cancellationToken, out var scope))
            {
                using var recursion = Recursion.Borrow(semanticModel, cancellationToken);
                return Assigns(new Target<SyntaxNode, ISymbol, SyntaxNode>(null!, localOrParameter.Symbol, scope), recursion, out first);
            }

            return false;
        }

        private static bool Assigns<TSource, TSymbol, TNode>(Target<TSource, TSymbol, TNode> target, Recursion recursion, out FieldOrProperty fieldOrProperty)
              where TSource : SyntaxNode
              where TSymbol : class, ISymbol
              where TNode : SyntaxNode
        {
            using (var walker = CreateUsagesWalker(target, recursion))
            {
                foreach (var usage in walker.usages)
                {
                    if (Assigns(usage, recursion, out fieldOrProperty))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool Assigns(ExpressionSyntax candidate, Recursion recursion, out FieldOrProperty fieldOrProperty)
        {
            switch (candidate.Parent.Kind())
            {
                case SyntaxKind.CastExpression:
                case SyntaxKind.AsExpression:
                case SyntaxKind.ConditionalExpression:
                case SyntaxKind.CoalesceExpression:
                    return Assigns((ExpressionSyntax)candidate.Parent, recursion, out fieldOrProperty);
            }

            switch (candidate.Parent)
            {
                case AssignmentExpressionSyntax { Left: { } left, Right: { } right }:
                    return right.Contains(candidate) &&
                           recursion.SemanticModel.TryGetSymbol(left, recursion.CancellationToken, out var assignedSymbol) &&
                           FieldOrProperty.TryCreate(assignedSymbol, out fieldOrProperty);
                case ArgumentSyntax { Parent: ArgumentListSyntax { Parent: InvocationExpressionSyntax invocation } } argument
                    when invocation.IsPotentialThisOrBase() &&
                         recursion.Target(argument) is { } target:
                    return Assigns(target, recursion, out fieldOrProperty);
                case EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax variableDeclarator }
                    when recursion.Target(variableDeclarator) is { } target:
                    return Assigns(target, recursion, out fieldOrProperty);

                default:
                    return false;
            }
        }
    }
}
