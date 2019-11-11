namespace IDisposableAnalyzers
{
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed partial class DisposableWalker
    {
        internal static bool ShouldDispose(LocalOrParameter localOrParameter, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (localOrParameter.Symbol is IParameterSymbol parameter &&
                parameter.RefKind != RefKind.None)
            {
                return false;
            }

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

            return true;
        }

        internal static bool DisposesAfter(ILocalSymbol local, ExpressionSyntax location, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<(string, SyntaxNode)> visited)
        {
            if (local.TrySingleDeclaration(cancellationToken, out var declaration) &&
                declaration is { Parent: VariableDeclarationSyntax { Parent: UsingStatementSyntax _ } })
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

        internal static bool DisposesBefore(ILocalSymbol local, ExpressionSyntax location, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<(string, SyntaxNode)> visited)
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

        internal static bool Disposes(ILocalSymbol local, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<(string, SyntaxNode)> visited)
        {
            if (local.TrySingleDeclaration(cancellationToken, out var declaration) &&
               declaration is { Parent: VariableDeclarationSyntax { Parent: UsingStatementSyntax _ } })
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
            }

            switch (candidate.Parent)
            {
                case ConditionalAccessExpressionSyntax { WhenNotNull: InvocationExpressionSyntax invocation }:
                    return IsDispose(invocation);
                case MemberAccessExpressionSyntax { Parent: InvocationExpressionSyntax invocation }:
                    return IsDispose(invocation);
                case AssignmentExpressionSyntax { Left: { } left } assignment
                    when left == candidate:
                    return Disposes(assignment, semanticModel, cancellationToken, visited);
                case EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax variableDeclarator }
                    when semanticModel.TryGetSymbol(variableDeclarator, cancellationToken, out ILocalSymbol assignedSymbol):
                    return Disposes(assignedSymbol, semanticModel, cancellationToken, visited);
                case ExpressionSyntax parent
                    when parent.IsKind(SyntaxKind.CastExpression) ||
                         parent.IsKind(SyntaxKind.AsExpression) ||
                         parent.IsKind(SyntaxKind.AsExpression) ||
                         parent.IsKind(SyntaxKind.ParenthesizedExpression):
                    return Disposes(parent, semanticModel, cancellationToken, visited);
                case ArgumentSyntax argument
                    when DisposedByReturnValue(argument, semanticModel, cancellationToken, visited, out var invocationOrObjectCreation):
                    return Disposes(invocationOrObjectCreation, semanticModel, cancellationToken, visited);
            }

            return false;

            static bool IsDispose(InvocationExpressionSyntax invocation)
            {
                return invocation is { ArgumentList: { Arguments: { Count: 0 } } } &&
                        invocation.TryGetMethodName(out var name) &&
                        name == "Dispose";
            }
        }
    }
}
