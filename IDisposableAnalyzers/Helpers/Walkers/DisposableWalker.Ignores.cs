namespace IDisposableAnalyzers
{
    using System.Linq;
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed partial class DisposableWalker
    {
        internal static bool Ignores(ExpressionSyntax candidate, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            using var recursion = Recursion.Borrow(candidate, semanticModel, cancellationToken);
            return Ignores(candidate, recursion);
        }

        private static bool Ignores(ExpressionSyntax candidate, Recursion recursion)
        {
            if (Disposes(candidate, recursion) ||
                Assigns(candidate, recursion, out _) ||
                Stores(candidate, recursion, out _) ||
                Returns(candidate, recursion))
            {
                return false;
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
                when Identity(candidate, recursion) is { } id
                => Ignores(id, recursion),
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
                _ => false
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
                if (target.TargetNode is null)
                {
                    if (!Ignores(parentExpression, recursion))
                    {
                        return !DisposedByReturnValue(target, recursion, out _) &&
                               !AccessibleInReturnValue(target, recursion, out _);
                    }

                    return true;
                }

                using var walker = CreateUsagesWalker(target, recursion);
                if (walker.usages.Count == 0)
                {
                    return true;
                }

                return walker.usages.All(x => IsIgnored(x));

                bool IsIgnored(IdentifierNameSyntax candidate)
                {
                    switch (candidate.Parent.Kind())
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
                            if (DisposeMethod.TryFindFirst(assignedMember.ContainingType, recursion.SemanticModel.Compilation, Search.TopLevel, out var disposeMethod) &&
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

                using (var walker = CreateUsagesWalker(new LocalOrParameter(local), recursion))
                {
                    foreach (var usage in walker.usages)
                    {
                        if (!Ignores(usage, recursion))
                        {
                            return false;
                        }
                    }
                }

                return true;
            }

            return false;
        }
    }
}
