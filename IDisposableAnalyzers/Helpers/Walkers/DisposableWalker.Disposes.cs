namespace IDisposableAnalyzers
{
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
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

            using var recursion = Recursion.Borrow(localOrParameter.Symbol.ContainingType, semanticModel, cancellationToken);
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

            using var recursion = Recursion.Borrow(local.ContainingType, semanticModel, cancellationToken);
            using var walker = CreateUsagesWalker(new LocalOrParameter(local), recursion);
            foreach (var usage in walker.usages)
            {
                if (location.IsExecutedBefore(usage).IsEither(ExecutedBefore.Yes, ExecutedBefore.Maybe) &&
                    Disposes(usage, recursion))
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool DisposesBefore(ILocalSymbol local, ExpressionSyntax location, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            using (var recursion = Recursion.Borrow(local.ContainingType, semanticModel, cancellationToken))
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
                    case { Parent: VariableDeclarationSyntax { Parent: LocalDeclarationStatementSyntax { UsingKeyword: { ValueText: "using" } } } }:
                        return true;
                }
            }

            using (var recursion = Recursion.Borrow(local.ContainingType, semanticModel, cancellationToken))
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
            using var walker = CreateUsagesWalker(target, recursion);
            foreach (var usage in walker.usages)
            {
                if (Disposes(usage, recursion))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool Disposes(ExpressionSyntax candidate, Recursion recursion)
        {
            return candidate switch
            {
                { Parent: UsingStatementSyntax _ }
                => true,
                { Parent: EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax { Parent: UsingStatementSyntax _ } } } }
                => true,
                { Parent: EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax { Parent: LocalDeclarationStatementSyntax { UsingKeyword: { ValueText: "using" } } } } } }
                => true,
                { }
                when Identity(candidate, recursion) is { } id &&
                     Disposes(id, recursion)
                => true,
                { Parent: ConditionalAccessExpressionSyntax { WhenNotNull: InvocationExpressionSyntax invocation } }
                => IsDisposeOrReturnValueDisposed(invocation),
                { Parent: MemberAccessExpressionSyntax { Parent: InvocationExpressionSyntax invocation } }
                => IsDisposeOrReturnValueDisposed(invocation),
                { Parent: ConditionalAccessExpressionSyntax { } parent }
                => DisposedByReturnValue(parent, recursion, out var creation) &&
                   Disposes(creation, recursion),
                { Parent: MemberAccessExpressionSyntax { } parent }
                => DisposedByReturnValue(parent, recursion, out var creation) &&
                   Disposes(creation, recursion),
                { Parent: AssignmentExpressionSyntax { Left: { } left } assignment }
                when left == candidate
                => Disposes(assignment, recursion),
                { Parent: EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax variableDeclarator } }
                when recursion.Target(variableDeclarator) is { } target
                => Disposes(target, recursion),
                { Parent: ArgumentSyntax { Parent: ArgumentListSyntax { Parent: InvocationExpressionSyntax invocation } } }
                when Winform.IsComponentsAdd(invocation, recursion.SemanticModel, recursion.CancellationToken)
                => true,
                { Parent: ArgumentSyntax argument }
                when recursion.Target(argument) is { } target
                => DisposedByReturnValue(target, recursion, out var wrapper) &&
                   Disposes(wrapper, recursion),
                _ => false
            };

            bool IsDisposeOrReturnValueDisposed(InvocationExpressionSyntax invocation)
            {
                if (IsDispose(invocation))
                {
                    return true;
                }

                return DisposedByReturnValue(invocation, recursion, out var creation) &&
                       Disposes(creation, recursion);
            }

            static bool IsDispose(InvocationExpressionSyntax invocation)
            {
                return invocation is { ArgumentList: { Arguments: { Count: 0 } } } &&
                        invocation.TryGetMethodName(out var name) &&
                        name == "Dispose";
            }
        }
    }
}
