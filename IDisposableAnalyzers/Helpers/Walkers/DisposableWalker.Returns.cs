namespace IDisposableAnalyzers
{
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed partial class DisposableWalker
    {
        public static bool Returns(LocalOrParameter localOrParameter, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<(string, SyntaxNode)> visited)
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

        private static bool Returns(ExpressionSyntax candidate, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<(string, SyntaxNode)> visited)
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
                case SyntaxKind.CollectionInitializerExpression:
                case SyntaxKind.ObjectCreationExpression:
                    return Returns((ExpressionSyntax)candidate.Parent, semanticModel, cancellationToken, visited);
            }

            switch (candidate.Parent)
            {
                case ArgumentSyntax argument when argument.Parent is ArgumentListSyntax argumentList &&
                                                  argumentList.Parent is ObjectCreationExpressionSyntax objectCreation:
                    return Returns(objectCreation, semanticModel, cancellationToken, visited);
                case EqualsValueClauseSyntax equalsValueClause when equalsValueClause.Parent is VariableDeclaratorSyntax variableDeclarator &&
                                                                    semanticModel.TryGetSymbol(variableDeclarator, cancellationToken, out ISymbol symbol) &&
                                                                    LocalOrParameter.TryCreate(symbol, out var localOrParameter):
                    if (visited.CanVisit(candidate, out visited))
                    {
                        using (visited)
                        {
                            return Returns(localOrParameter, semanticModel, cancellationToken, visited);
                        }
                    }

                    return false;
                default:
                    return false;
            }
        }
    }
}
