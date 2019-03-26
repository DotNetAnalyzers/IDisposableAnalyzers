namespace IDisposableAnalyzers
{
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed partial class DisposableWalker
    {
        public static bool ShouldDispose(LocalOrParameter localOrParameter, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            using (var walker = CreateUsagesWalker(localOrParameter, semanticModel, cancellationToken))
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

                    if (Stores(usage, semanticModel, cancellationToken, null, out _))
                    {
                        return false;
                    }

                    if (Disposes(usage, semanticModel, cancellationToken, null))
                    {
                        return false;
                    }

                    if (DisposedByReturnValue(usage, semanticModel, cancellationToken, null, out _))
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

        public static bool DisposesAfter(ILocalSymbol local, ExpressionSyntax location, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<(string, SyntaxNode)> visited)
        {
            if (local.TrySingleDeclaration(cancellationToken, out var declaration) &&
                declaration.Parent is VariableDeclarationSyntax variableDeclaration &&
                variableDeclaration.Parent is UsingStatementSyntax)
            {
                return true;
            }

            using (var walker = CreateUsagesWalker(new LocalOrParameter(local), semanticModel, cancellationToken))
            {
                foreach (var usage in walker.usages)
                {
                    if (location.IsExecutedBefore(usage).IsEither(ExecutedBefore.Yes, ExecutedBefore.Maybe) &&
                        Disposes(usage, semanticModel, cancellationToken, visited))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool DisposesBefore(ILocalSymbol local, ExpressionSyntax location, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<(string, SyntaxNode)> visited)
        {
            using (var walker = CreateUsagesWalker(new LocalOrParameter(local), semanticModel, cancellationToken))
            {
                foreach (var usage in walker.usages)
                {
                    if (usage.IsExecutedBefore(location).IsEither(ExecutedBefore.Yes, ExecutedBefore.Maybe) &&
                        Disposes(usage, semanticModel, cancellationToken, visited))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool Disposes(ILocalSymbol local, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<(string, SyntaxNode)> visited)
        {
            if (local.TrySingleDeclaration(cancellationToken, out var declaration) &&
               declaration.Parent is VariableDeclarationSyntax variableDeclaration &&
               variableDeclaration.Parent is UsingStatementSyntax)
            {
                return true;
            }

            using (var walker = CreateUsagesWalker(new LocalOrParameter(local), semanticModel, cancellationToken))
            {
                foreach (var usage in walker.usages)
                {
                    if (Disposes(usage, semanticModel, cancellationToken, visited))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool Disposes(ExpressionSyntax candidate, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<(string, SyntaxNode)> visited)
        {
            switch (candidate.Parent.Kind())
            {
                case SyntaxKind.UsingStatement:
                    return true;
                case SyntaxKind.CastExpression:
                case SyntaxKind.AsExpression:
                case SyntaxKind.ParenthesizedExpression:
                    return Disposes((ExpressionSyntax)candidate.Parent, semanticModel, cancellationToken, visited);
            }

            switch (candidate.Parent)
            {
                case ConditionalAccessExpressionSyntax conditionalAccess when conditionalAccess.WhenNotNull is InvocationExpressionSyntax invocation:
                    return IsDispose(invocation);
                case MemberAccessExpressionSyntax memberAccess when memberAccess.Parent is InvocationExpressionSyntax invocation:
                    return IsDispose(invocation);
                case EqualsValueClauseSyntax equalsValueClause when equalsValueClause.Parent is VariableDeclaratorSyntax variableDeclarator &&
                semanticModel.TryGetSymbol(variableDeclarator, cancellationToken, out ILocalSymbol assignedSymbol):
                    if (visited.CanVisit(candidate, out visited))
                    {
                        using (visited)
                        {
                            return Disposes(assignedSymbol, semanticModel, cancellationToken, visited);
                        }
                    }

                    return false;
            }

            return false;

            bool IsDispose(InvocationExpressionSyntax invocation)
            {
                return invocation.ArgumentList is ArgumentListSyntax argumentList &&
                        argumentList.Arguments.Count == 0 &&
                        invocation.TryGetMethodName(out var name) &&
                        name == "Dispose";
            }
        }
    }
}
