namespace IDisposableAnalyzers
{
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed partial class DisposableWalker
    {
        internal static bool Returns(LocalOrParameter localOrParameter, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            using (var recursion = Recursion.Borrow(semanticModel, cancellationToken))
            {
                using (var walker = CreateUsagesWalker(localOrParameter, recursion))
                {
                    foreach (var usage in walker.usages)
                    {
                        if (Returns(usage, recursion))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static bool Returns<TSource, TSymbol, TNode>(Target<TSource, TSymbol, TNode> target, Recursion recursion)
            where TSource : SyntaxNode
            where TSymbol : ISymbol
            where TNode : SyntaxNode
        {
            if (target.TargetNode is { })
            {
                using (var walker = CreateUsagesWalker(target, recursion))
                {
                    foreach (var usage in walker.usages)
                    {
                        if (Returns(usage, recursion))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static bool Returns(ExpressionSyntax candidate, Recursion recursion)
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
                    return Returns((ExpressionSyntax)candidate.Parent, recursion);
            }

            switch (candidate.Parent)
            {
                case EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax variableDeclarator }
                    when recursion.Target(variableDeclarator) is { } target:
                    return Returns(target, recursion);

                default:
                    return false;
            }
        }
    }
}
