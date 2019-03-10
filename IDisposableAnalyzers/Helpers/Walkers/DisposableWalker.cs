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
                    if (Returns(usage, semanticModel, cancellationToken, null))
                    {
                        return false;
                    }

                    if (Assigns(usage, semanticModel, cancellationToken, null, out _))
                    {
                        return false;
                    }

                    if (Stores(usage, semanticModel, cancellationToken, null))
                    {
                        return false;
                    }
                }
            }

            if (localOrParameter.Symbol is ILocalSymbol local &&
                local.TrySingleDeclaration(cancellationToken, out SingleVariableDesignationSyntax designation) &&
                designation.Parent is DeclarationExpressionSyntax declaration &&
                declaration.Parent is ArgumentSyntax argument &&
                argument.Parent is ArgumentListSyntax argumentList &&
                semanticModel.TryGetSymbol(argumentList.Parent, cancellationToken, out IMethodSymbol method) &&
                method.TryFindParameter(argument, out var parameter) &&
                LocalOrParameter.TryCreate(parameter, out localOrParameter))
            {
                return ShouldDispose(localOrParameter, semanticModel, cancellationToken);
            }

            return true;
        }

        public static bool Returns(LocalOrParameter localOrParameter, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<SyntaxNode> visited)
        {
            using (var walker = CreateWalker(localOrParameter, semanticModel, cancellationToken))
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

        public static bool Assigns(LocalOrParameter localOrParameter, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<SyntaxNode> visited, out FieldOrProperty first)
        {
            using (var walker = CreateWalker(localOrParameter, semanticModel, cancellationToken))
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

        public static bool Stores(LocalOrParameter localOrParameter, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<SyntaxNode> visited)
        {
            using (var walker = CreateWalker(localOrParameter, semanticModel, cancellationToken))
            {
                foreach (var usage in walker.usages)
                {
                    if (Stores(usage, semanticModel, cancellationToken, visited))
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

        private static bool Returns(ExpressionSyntax candidate, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<SyntaxNode> visited)
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
#pragma warning disable IDISP003 // Dispose previous before re-assigning.
                    using (visited = visited.IncrementUsage())
#pragma warning restore IDISP003
                    {
                        return visited.Add(variableDeclarator) &&
                               Returns(localOrParameter, semanticModel, cancellationToken, visited);
                    }

                default:
                    return false;
            }
        }

        private static bool Assigns(ExpressionSyntax candidate, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<SyntaxNode> visited, out FieldOrProperty fieldOrProperty)
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
                               Assigns(localOrParameter, semanticModel, cancellationToken, visited, out fieldOrProperty);
                    }

                case EqualsValueClauseSyntax equalsValueClause when equalsValueClause.Parent is VariableDeclaratorSyntax variableDeclarator &&
                                                                    semanticModel.TryGetSymbol(variableDeclarator, cancellationToken, out ISymbol symbol) &&
                                                                    LocalOrParameter.TryCreate(symbol, out var localOrParameter):
#pragma warning disable IDISP003 // Dispose previous before re-assigning.
                    using (visited = visited.IncrementUsage())
#pragma warning restore IDISP003
                    {
                        return visited.Add(variableDeclarator) &&
                               Assigns(localOrParameter, semanticModel, cancellationToken, visited, out fieldOrProperty);
                    }

                default:
                    return false;
            }
        }

        private static bool Stores(ExpressionSyntax candidate, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<SyntaxNode> visited)
        {
            switch (candidate.Parent.Kind())
            {
                case SyntaxKind.CastExpression:
                case SyntaxKind.AsExpression:
                case SyntaxKind.ConditionalExpression:
                case SyntaxKind.CoalesceExpression:
                    return Stores((ExpressionSyntax)candidate.Parent, semanticModel, cancellationToken, visited);
            }

            switch (candidate.Parent)
            {
                case AssignmentExpressionSyntax assignment when assignment.Right.Contains(candidate) &&
                                                                assignment.Left.IsKind(SyntaxKind.ElementAccessExpression):
                    return true;
                case ArgumentSyntax argument when argument.Parent is ArgumentListSyntax argumentList &&
                                                  argumentList.Parent is InvocationExpressionSyntax invocation &&
                                                  semanticModel.TryGetSymbol(invocation, cancellationToken, out var method):

                    if (!method.TrySingleMethodDeclaration(cancellationToken, out _) &&
                        method.ContainingType.IsAssignableTo(KnownSymbol.IEnumerable, semanticModel.Compilation))
                    {
                        return true;
                    }

                    if (method.TryFindParameter(argument, out var parameter) &&
                        LocalOrParameter.TryCreate(parameter, out var localOrParameter))
                    {
#pragma warning disable IDISP003 // Dispose previous before re-assigning.
                        using (visited = visited.IncrementUsage())
#pragma warning restore IDISP003
                        {
                            return visited.Add(argument) &&
                                   Stores(localOrParameter, semanticModel, cancellationToken, visited);
                        }
                    }

                    break;

                case EqualsValueClauseSyntax equalsValueClause when equalsValueClause.Parent is VariableDeclaratorSyntax variableDeclarator &&
                                                                    semanticModel.TryGetSymbol(variableDeclarator, cancellationToken, out ISymbol symbol) &&
                                                                    LocalOrParameter.TryCreate(symbol, out var local):
#pragma warning disable IDISP003 // Dispose previous before re-assigning.
                    using (visited = visited.IncrementUsage())
#pragma warning restore IDISP003
                    {
                        return visited.Add(variableDeclarator) &&
                               Stores(local, semanticModel, cancellationToken, visited);
                    }

                default:
                    return false;
            }

            return false;
        }
    }
}
