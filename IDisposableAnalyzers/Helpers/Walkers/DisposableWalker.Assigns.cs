namespace IDisposableAnalyzers
{
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed partial class DisposableWalker
    {
        public static bool Assigns(LocalOrParameter localOrParameter, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<(string, SyntaxNode)> visited, out FieldOrProperty first)
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

        private static bool Assigns(ExpressionSyntax candidate, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<(string, SyntaxNode)> visited, out FieldOrProperty fieldOrProperty)
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
                case AssignmentExpressionSyntax assignment:
                    return assignment.Right.Contains(candidate) &&
                           semanticModel.TryGetSymbol(assignment.Left, cancellationToken, out ISymbol assignedSymbol) &&
                           FieldOrProperty.TryCreate(assignedSymbol, out fieldOrProperty);
                case ArgumentSyntax argument when argument.Parent is ArgumentListSyntax argumentList &&
                                                  argumentList.Parent is InvocationExpressionSyntax invocation &&
                                                  invocation.IsPotentialThisOrBase() &&
                                                  semanticModel.TryGetSymbol(invocation, cancellationToken, out IMethodSymbol method) &&
                                                  method.TryFindParameter(argument, out var parameter) &&
                                                  LocalOrParameter.TryCreate(parameter, out var localOrParameter):
                    if (visited.CanVisit(candidate, out visited))
                    {
                        using (visited)
                        {
                            return Assigns(localOrParameter, semanticModel, cancellationToken, visited, out fieldOrProperty);
                        }
                    }

                    return false;

                case EqualsValueClauseSyntax equalsValueClause when equalsValueClause.Parent is VariableDeclaratorSyntax variableDeclarator &&
                                                                    semanticModel.TryGetSymbol(variableDeclarator, cancellationToken, out ISymbol symbol) &&
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
