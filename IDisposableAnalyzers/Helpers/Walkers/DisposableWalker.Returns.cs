namespace IDisposableAnalyzers
{
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed partial class DisposableWalker
    {
        internal static bool Returns(LocalOrParameter localOrParameter, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<(string Caller, SyntaxNode Node)>? visited)
        {
            using (var walker = CreateUsagesWalker(localOrParameter, semanticModel, cancellationToken))
            {
                foreach (var usage in walker.usages)
                {
                    if (Returns(usage, semanticModel, cancellationToken, visited))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool Returns<TSource, TSymbol, TNode>(Target<TSource, TSymbol, TNode> target, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<(string Caller, SyntaxNode Node)>? visited)
            where TSource : SyntaxNode
            where TSymbol : ISymbol
            where TNode : SyntaxNode
        {
            if (target.TargetNode is { })
            {
                using (var walker = CreateUsagesWalker(target, semanticModel, cancellationToken))
                {
                    foreach (var usage in walker.usages)
                    {
                        if (Returns(usage, semanticModel, cancellationToken, visited))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static bool Returns(ExpressionSyntax candidate, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<(string Caller, SyntaxNode Node)>? visited)
        {
            switch (candidate.Parent.Kind())
            {
                case SyntaxKind.ReturnStatement:
                case SyntaxKind.ArrowExpressionClause:
                    return true;
                case SyntaxKind.CastExpression:
                case SyntaxKind.AsExpression:
                case SyntaxKind.ConditionalExpression:
                case SyntaxKind.CoalesceExpression:
                    return Returns((ExpressionSyntax)candidate.Parent, semanticModel, cancellationToken, visited);
            }

            switch (candidate.Parent)
            {
                case EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax variableDeclarator }
                    when Target(variableDeclarator, semanticModel, cancellationToken, visited) is { } target:
                    return Returns(target, semanticModel, cancellationToken, visited);

                default:
                    return false;
            }
        }
    }
}
