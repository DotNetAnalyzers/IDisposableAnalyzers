namespace IDisposableAnalyzers
{
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed partial class DisposableWalker
    {
        internal static bool Returns(LocalOrParameter localOrParameter, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            using var recursion = Recursion.Borrow(localOrParameter.Symbol.ContainingType, semanticModel, cancellationToken);
            using var walker = CreateUsagesWalker(localOrParameter, recursion);
            foreach (var usage in walker.usages)
            {
                if (Returns(usage, recursion))
                {
                    return true;
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
                using var walker = CreateUsagesWalker(target, recursion);
                foreach (var usage in walker.usages)
                {
                    if (Returns(usage, recursion))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool Returns(ExpressionSyntax candidate, Recursion recursion)
        {
            return candidate switch
            {
                { Parent: ReturnStatementSyntax _ }
                => true,
                { Parent: ArrowExpressionClauseSyntax _ }
                => true,
                { Parent: EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax variableDeclarator } }
                => recursion.Target(variableDeclarator) is { } target &&
                   Returns(target, recursion),
                { }
                when Identity(candidate, recursion) is { } id &&
                     Returns(id, recursion)
                => true,
                _ => false,
            };
        }
    }
}
