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

            using var recursion = Recursion.Borrow(semanticModel, cancellationToken);
            using var walker = CreateUsagesWalker(localOrParameter, recursion);
            foreach (var usage in walker.usages)
            {
                if (Returns(usage, recursion))
                {
                    return false;
                }

                if (Assigns(usage, recursion, out _))
                {
                    return false;
                }

                if (Stores(usage, recursion, out _))
                {
                    return false;
                }

                if (Disposes(usage, recursion))
                {
                    return false;
                }

                if (DisposedByReturnValue(usage, recursion, out _))
                {
                    return false;
                }
            }

            return true;
        }

        internal static bool DisposesAfter(ILocalSymbol local, ExpressionSyntax location, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (local.TrySingleDeclaration(cancellationToken, out var declaration) &&
                declaration is { Parent: VariableDeclarationSyntax { Parent: UsingStatementSyntax _ } })
            {
                return true;
            }

            using (var recursion = Recursion.Borrow(semanticModel, cancellationToken))
            {
                using (var walker = CreateUsagesWalker(new LocalOrParameter(local), recursion))
                {
                    foreach (var usage in walker.usages)
                    {
                        if (location.IsExecutedBefore(usage).IsEither(ExecutedBefore.Yes, ExecutedBefore.Maybe) &&
                            Disposes(usage, recursion))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        internal static bool DisposesBefore(ILocalSymbol local, ExpressionSyntax location, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            using (var recursion = Recursion.Borrow(semanticModel, cancellationToken))
            {
                using var walker = CreateUsagesWalker(new LocalOrParameter(local), recursion);
                foreach (var usage in walker.usages)
                {
                    if (usage.IsExecutedBefore(location).IsEither(ExecutedBefore.Yes, ExecutedBefore.Maybe) &&
                        Disposes(usage, recursion))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        internal static bool Disposes(ILocalSymbol local, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (local.TrySingleDeclaration(cancellationToken, out var declaration))
            {
                switch (declaration)
                {
                    case { Parent: VariableDeclarationSyntax { Parent: UsingStatementSyntax _ } }:
                    case { Parent: VariableDeclarationSyntax { Parent: LocalDeclarationStatementSyntax { UsingKeyword: var usingKeywod } } }
                        when usingKeywod.IsKind(SyntaxKind.UsingKeyword):
                        return true;
                }
            }

            using (var recursion = Recursion.Borrow(semanticModel, cancellationToken))
            {
                using var walker = CreateUsagesWalker(new LocalOrParameter(local), recursion);
                foreach (var usage in walker.usages)
                {
                    if (Disposes(usage, recursion))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool Disposes<TSource, TSymbol, TNode>(Target<TSource, TSymbol, TNode> target, Recursion recursion)
            where TSource : SyntaxNode
            where TSymbol : class, ISymbol
            where TNode : SyntaxNode
        {
            using (var walker = CreateUsagesWalker(target, recursion))
            {
                foreach (var usage in walker.usages)
                {
                    if (Disposes(usage, recursion))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool Disposes(ExpressionSyntax candidate, Recursion recursion)
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
                    return Disposes(assignment, recursion);
                case EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax { Parent: UsingStatementSyntax _ } }:
                    return true;
                case EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax variableDeclarator }
                    when recursion.Target(variableDeclarator) is { } target:
                    return Disposes(target, recursion);
                case ExpressionSyntax parent
                    when parent.IsKind(SyntaxKind.CastExpression) ||
                         parent.IsKind(SyntaxKind.AsExpression) ||
                         parent.IsKind(SyntaxKind.AsExpression) ||
                         parent.IsKind(SyntaxKind.ParenthesizedExpression):
                    return Disposes(parent, recursion);
                case ArgumentSyntax argument
                    when recursion.Target(argument) is { } target:
                    return DisposedByReturnValue(target, recursion, out var creation) &&
                           Disposes(creation, recursion);
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
