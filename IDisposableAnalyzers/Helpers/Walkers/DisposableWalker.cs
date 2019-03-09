namespace IDisposableAnalyzers
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed class DisposableWalker : PooledWalker<DisposableWalker>
    {
        private readonly List<IdentifierNameSyntax> usages = new List<IdentifierNameSyntax>();

        public static bool ShouldDispose(LocalOrParameter localOrParameter, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            using (var walker = CreateWalker(localOrParameter, semanticModel, cancellationToken))
            {
                foreach (var usage in walker.usages)
                {
                    if (IsReturned(usage, semanticModel, cancellationToken, null))
                    {
                        return false;
                    }

                    if (IsAssigned(usage, semanticModel, cancellationToken, null, out _))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public static bool IsReturned(LocalOrParameter localOrParameter, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<SyntaxNode> visited)
        {
            using (var walker = CreateWalker(localOrParameter, semanticModel, cancellationToken))
            {
                foreach (var usage in walker.usages)
                {
                    if (IsReturned(usage, semanticModel, cancellationToken, visited))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool IsAssigned(LocalOrParameter localOrParameter, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<SyntaxNode> visited, out FieldOrProperty first)
        {
            using (var walker = CreateWalker(localOrParameter, semanticModel, cancellationToken))
            {
                foreach (var usage in walker.usages)
                {
                    if (IsAssigned(usage, semanticModel, cancellationToken, null, out first))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public void RemoveAll(Predicate<IdentifierNameSyntax> match) => this.usages.RemoveAll(match);

        public override void VisitIdentifierName(IdentifierNameSyntax node)
        {
            this.usages.Add(node);
        }

        protected override void Clear()
        {
            this.usages.Clear();
        }

        private static DisposableWalker CreateWalker(LocalOrParameter localOrParameter, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (localOrParameter.TryGetScope(cancellationToken, out var scope))
            {
                var walker = BorrowAndVisit(scope, () => new DisposableWalker());
                walker.RemoveAll(x => !IsMatch(x));
                return walker;
            }

            return Borrow(() => new DisposableWalker());

            bool IsMatch(IdentifierNameSyntax identifierName)
            {
                return identifierName.Identifier.Text == localOrParameter.Name &&
                       semanticModel.TryGetSymbol(identifierName, cancellationToken, out ISymbol symbol) &&
                       symbol.Equals(localOrParameter.Symbol);
            }
        }

        private static bool IsReturned(ExpressionSyntax candidate, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<SyntaxNode> visited)
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
                    return IsReturned((ExpressionSyntax)candidate.Parent, semanticModel, cancellationToken, visited);
            }

            switch (candidate.Parent)
            {
                case ArgumentSyntax argument when argument.Parent is ArgumentListSyntax argumentList &&
                                                  argumentList.Parent is ObjectCreationExpressionSyntax objectCreation:
                    return IsReturned(objectCreation, semanticModel, cancellationToken, visited);
                case EqualsValueClauseSyntax equalsValueClause when equalsValueClause.Parent is VariableDeclaratorSyntax variableDeclarator &&
                                                                    semanticModel.TryGetSymbol(variableDeclarator, cancellationToken, out ISymbol symbol) &&
                                                                    LocalOrParameter.TryCreate(symbol, out var localOrParameter):
#pragma warning disable IDISP003 // Dispose previous before re-assigning.
                    using (visited = visited.IncrementUsage())
#pragma warning restore IDISP003
                    {
                        return visited.Add(variableDeclarator) &&
                               IsReturned(localOrParameter, semanticModel, cancellationToken, visited);
                    }

                default:
                    return false;
            }
        }

        private static bool IsAssigned(ExpressionSyntax candidate, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<SyntaxNode> visited, out FieldOrProperty fieldOrProperty)
        {
            switch (candidate.Parent.Kind())
            {
                case SyntaxKind.CastExpression:
                case SyntaxKind.AsExpression:
                case SyntaxKind.ConditionalExpression:
                case SyntaxKind.CoalesceExpression:
                    return IsAssigned((ExpressionSyntax)candidate.Parent, semanticModel, cancellationToken, visited, out fieldOrProperty);
            }

            switch (candidate.Parent)
            {
                case AssignmentExpressionSyntax assignment when assignment.Right.Contains(candidate) && semanticModel.TryGetSymbol(assignment.Left, cancellationToken, out ISymbol symbol):
                    return FieldOrProperty.TryCreate(symbol, out fieldOrProperty);
                case ArgumentSyntax argument when argument.Parent is ArgumentListSyntax argumentList &&
                                                  argumentList.Parent is InvocationExpressionSyntax invocation &&
                                                  semanticModel.TryGetSymbol(invocation, cancellationToken, out IMethodSymbol method) &&
                                                  method.TryFindParameter(argument, out var parameter) &&
                                                  LocalOrParameter.TryCreate(parameter, out var localOrParameter):
#pragma warning disable IDISP003 // Dispose previous before re-assigning.
                    using (visited = visited.IncrementUsage())
#pragma warning restore IDISP003
                    {
                        return visited.Add(argument) &&
                               IsAssigned(localOrParameter, semanticModel, cancellationToken, visited, out fieldOrProperty);
                    }

                case EqualsValueClauseSyntax equalsValueClause when equalsValueClause.Parent is VariableDeclaratorSyntax variableDeclarator &&
                                                                    semanticModel.TryGetSymbol(variableDeclarator, cancellationToken, out ISymbol symbol) &&
                                                                    LocalOrParameter.TryCreate(symbol, out var localOrParameter):
#pragma warning disable IDISP003 // Dispose previous before re-assigning.
                    using (visited = visited.IncrementUsage())
#pragma warning restore IDISP003
                    {
                        return visited.Add(variableDeclarator) &&
                               IsAssigned(localOrParameter, semanticModel, cancellationToken, visited, out fieldOrProperty);
                    }

                default:
                    return false;
            }
        }
    }
}
