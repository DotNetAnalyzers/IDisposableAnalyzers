namespace IDisposableAnalyzers
{
    using System.Linq;
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static partial class Disposable
    {
        internal static bool Ignores(ExpressionSyntax candidate, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (candidate.TryFirstAncestor(out TypeDeclarationSyntax? containingTypeDeclaration) &&
                semanticModel.TryGetNamedType(containingTypeDeclaration, cancellationToken, out var containingType))
            {
                using var recursion = Recursion.Borrow(containingType, semanticModel, cancellationToken);
                return Ignores(candidate, recursion);
            }

            return false;
        }

        private static bool Ignores(ExpressionSyntax candidate, Recursion recursion)
        {
            using (var temp = Recursion.Borrow(recursion.ContainingType, recursion.SemanticModel, recursion.CancellationToken))
            {
                if (Disposes(candidate, temp))
                {
                    return false;
                }
            }

            using (var temp = Recursion.Borrow(recursion.ContainingType, recursion.SemanticModel, recursion.CancellationToken))
            {
                if (Assigns(candidate, temp, out _))
                {
                    return false;
                }
            }

            using (var temp = Recursion.Borrow(recursion.ContainingType, recursion.SemanticModel, recursion.CancellationToken))
            {
                if (Stores(candidate, temp, out _))
                {
                    return false;
                }
            }

            using (var temp = Recursion.Borrow(recursion.ContainingType, recursion.SemanticModel, recursion.CancellationToken))
            {
                if (Returns(candidate, temp))
                {
                    return false;
                }
            }

            return candidate switch
            {
                { Parent: AssignmentExpressionSyntax { Left: IdentifierNameSyntax { Identifier: { ValueText: "_" } } } }
                => true,
                { Parent: EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax { Identifier: { ValueText: "_" } } } }
                => true,
                { Parent: AnonymousFunctionExpressionSyntax _ }
                => false,
                { Parent: StatementSyntax _ }
                => true,
                { }
                when Identity(candidate, recursion) is { } id &&
                     !Ignores(id, recursion)
                => false,
                { Parent: ArgumentSyntax { Parent: TupleExpressionSyntax tuple } }
                => Ignores(tuple, recursion),
                { Parent: ArgumentSyntax argument }
                when recursion.Target(argument) is { } target
                => Ignores(target, recursion),
                { Parent: MemberAccessExpressionSyntax _ }
                => WrappedAndIgnored(),
                { Parent: ConditionalAccessExpressionSyntax _ }
                => WrappedAndIgnored(),
                { Parent: InitializerExpressionSyntax { Parent: ExpressionSyntax creation } }
                => Ignores(creation, recursion),
                _ => false,
            };

            bool WrappedAndIgnored()
            {
                if (DisposedByReturnValue(candidate, recursion, out var returnValue) &&
                    !Ignores(returnValue, recursion))
                {
                    return false;
                }

                return true;
            }
        }

        private static bool Ignores(Target<ArgumentSyntax, IParameterSymbol, BaseMethodDeclarationSyntax> target, Recursion recursion)
        {
            if (target.Source is { Parent: ArgumentListSyntax { Parent: ExpressionSyntax parentExpression } })
            {
                if (target.Declaration is null)
                {
                    if (!Ignores(parentExpression, recursion))
                    {
                        return !DisposedByReturnValue(target, recursion, out _) &&
                               !AccessibleInReturnValue(target, recursion, out _);
                    }

                    return true;
                }

                using var walker = UsagesWalker.Borrow(target.Symbol, target.Declaration, recursion.SemanticModel, recursion.CancellationToken);
                if (walker.Usages.Count == 0)
                {
                    return true;
                }

                return walker.Usages.All(x => IsIgnored(x));

                bool IsIgnored(IdentifierNameSyntax candidate)
                {
                    switch (candidate.Parent?.Kind())
                    {
                        case SyntaxKind.NotEqualsExpression:
                            return true;
                        case SyntaxKind.Argument:
                            // Stopping analysis here assuming it is handled
                            return false;
                    }

                    switch (candidate.Parent)
                    {
                        case AssignmentExpressionSyntax { Right: { } right, Left: { } left }
                            when right == candidate &&
                                 recursion.SemanticModel.TryGetSymbol(left, recursion.CancellationToken, out var assignedSymbol) &&
                                 FieldOrProperty.TryCreate(assignedSymbol, out var assignedMember):
                            if (DisposeMethod.FindFirst(assignedMember.ContainingType, recursion.SemanticModel.Compilation, Search.TopLevel) is { } disposeMethod &&
                                DisposableMember.IsDisposed(assignedMember, disposeMethod, recursion.SemanticModel, recursion.CancellationToken))
                            {
                                return Ignores(parentExpression, recursion);
                            }

                            if (parentExpression.Parent.IsEither(SyntaxKind.ArrowExpressionClause, SyntaxKind.ReturnStatement))
                            {
                                return true;
                            }

                            return !recursion.SemanticModel.IsAccessible(target.Source.SpanStart, assignedMember.Symbol);
                        case EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax variableDeclarator }:
                            return Ignores(variableDeclarator, recursion);
                    }

                    if (Ignores(candidate, recursion))
                    {
                        return true;
                    }

                    return false;
                }
            }

            return false;
        }

        private static bool Ignores(VariableDeclaratorSyntax declarator, Recursion recursion)
        {
            if (recursion.SemanticModel.TryGetSymbol(declarator, recursion.CancellationToken, out ILocalSymbol? local))
            {
                if (declarator.TryFirstAncestor<UsingStatementSyntax>(out _))
                {
                    return false;
                }

                using var walker = UsagesWalker.Borrow(new LocalOrParameter(local), recursion.SemanticModel, recursion.CancellationToken);
                foreach (var usage in walker.Usages)
                {
                    if (!Ignores(usage, recursion))
                    {
                        return false;
                    }
                }

                return true;
            }

            return false;
        }
    }
}
