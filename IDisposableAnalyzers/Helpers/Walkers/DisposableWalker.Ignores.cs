namespace IDisposableAnalyzers
{
    using System;
    using System.Linq;
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed partial class DisposableWalker
    {
        internal static bool Ignores(ExpressionSyntax node, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<(string Caller, SyntaxNode Node)>? visited)
        {
            if (Disposes(node, semanticModel, cancellationToken, visited) ||
                Assigns(node, semanticModel, cancellationToken, visited, out _) ||
                Stores(node, semanticModel, cancellationToken, visited, out _) ||
                Returns(node, semanticModel, cancellationToken, visited))
            {
                return false;
            }

            switch (node.Parent)
            {
                case AssignmentExpressionSyntax { Left: IdentifierNameSyntax { Identifier: { ValueText: "_" } } }:
                case EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax { Identifier: { ValueText: "_" } } }:
                    return true;
                case AnonymousFunctionExpressionSyntax _:
                case UsingStatementSyntax _:
                case ReturnStatementSyntax _:
                case ArrowExpressionClauseSyntax _:
                    return false;
                case StatementSyntax _:
                    return true;
                case ArgumentSyntax { Parent: TupleExpressionSyntax tuple }:
                    return Ignores(tuple, semanticModel, cancellationToken, visited);
                case ArgumentSyntax argument:
                    return Ignores(Target(argument, semanticModel, cancellationToken, visited).Value, semanticModel, cancellationToken, visited);
                case MemberAccessExpressionSyntax memberAccess
                    when semanticModel.TryGetSymbol(memberAccess, cancellationToken, out var symbol):
                    return IsChainedDisposingInReturnValue(symbol, semanticModel, cancellationToken, visited).IsEither(Result.No, Result.AssumeNo);
                case ConditionalAccessExpressionSyntax { WhenNotNull: { } whenNotNull } conditionalAccess
                    when semanticModel.TryGetSymbol(whenNotNull, cancellationToken, out var symbol):
                    return IsChainedDisposingInReturnValue(symbol, semanticModel, cancellationToken, visited).IsEither(Result.No, Result.AssumeNo);
                case InitializerExpressionSyntax { Parent: ExpressionSyntax creation } initializer:
                    return Ignores(creation, semanticModel, cancellationToken, visited);
            }

            return false;
        }

        private static bool Ignores(Target<ArgumentSyntax, IParameterSymbol, BaseMethodDeclarationSyntax> target, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<(string Caller, SyntaxNode Node)>? visited)
        {
            if (target.Source is { Parent: ArgumentListSyntax { Parent: ExpressionSyntax parentExpression } } &&
                semanticModel.TryGetSymbol(parentExpression, cancellationToken, out IMethodSymbol? method))
            {
                if (method.DeclaringSyntaxReferences.IsEmpty)
                {
                    if (!Ignores(parentExpression, semanticModel, cancellationToken, visited))
                    {
                        return !DisposedByReturnValue(target.Source, semanticModel, cancellationToken, visited, out _) &&
                               !AccessibleInReturnValue(target.Source, semanticModel, cancellationToken, visited, out _);
                    }

                    return true;
                }

                using (var walker = CreateUsagesWalker(target, semanticModel, cancellationToken))
                {
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
                                     semanticModel.TryGetSymbol(left, cancellationToken, out var assignedSymbol) &&
                                     FieldOrProperty.TryCreate(assignedSymbol, out var assignedMember):
                                if (DisposeMethod.TryFindFirst(assignedMember.ContainingType, semanticModel.Compilation, Search.TopLevel, out var disposeMethod) &&
                                    DisposableMember.IsDisposed(assignedMember, disposeMethod, semanticModel, cancellationToken))
                                {
                                    return Ignores(parentExpression, semanticModel, cancellationToken, visited);
                                }

                                if (parentExpression.Parent.IsEither(SyntaxKind.ArrowExpressionClause, SyntaxKind.ReturnStatement))
                                {
                                    return true;
                                }

                                return !semanticModel.IsAccessible(target.Source.SpanStart, assignedMember.Symbol);
                            case EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax variableDeclarator }:
                                return Ignores(variableDeclarator, semanticModel, cancellationToken, visited);
                        }

                        if (Ignores(candidate, semanticModel, cancellationToken, visited))
                        {
                            return true;
                        }

                        return false;
                    }
                }
            }

            return false;
        }

        private static bool Ignores(VariableDeclaratorSyntax declarator, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<(string Caller, SyntaxNode Node)>? visited)
        {
            if (declarator.TryFirstAncestor(out BlockSyntax? block) &&
                semanticModel.TryGetSymbol(declarator, cancellationToken, out ILocalSymbol? local))
            {
                if (declarator.TryFirstAncestor<UsingStatementSyntax>(out _))
                {
                    return false;
                }

                using (var walker = CreateUsagesWalker(new LocalOrParameter(local), semanticModel, cancellationToken))
                {
                    foreach (var usage in walker.usages)
                    {
                        if (!Ignores(usage, semanticModel, cancellationToken, visited))
                        {
                            return false;
                        }
                    }
                }

                return true;
            }

            return false;
        }

        [Obsolete("Use DisposableWalker")]
        private static Result IsChainedDisposingInReturnValue(ISymbol symbol, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<(string Caller, SyntaxNode Node)>? visited)
        {
            if (symbol is IMethodSymbol method)
            {
                if (method.ReturnsVoid)
                {
                    return Result.No;
                }

                if (method.ReturnType.Name == "ConfiguredTaskAwaitable")
                {
                    return Result.Yes;
                }

                if (method.ContainingType.DeclaringSyntaxReferences.Length == 0)
                {
                    if (method.ReturnType == KnownSymbol.Task)
                    {
                        return Result.No;
                    }

                    if (method.ReturnType == KnownSymbol.TaskOfT &&
                        method.ReturnType is INamedTypeSymbol namedType &&
                        namedType.TypeArguments.TrySingle(out var type))
                    {
                        return !Disposable.IsAssignableFrom(type, semanticModel.Compilation)
                            ? Result.No
                            : Result.AssumeYes;
                    }

                    return !Disposable.IsAssignableFrom(method.ReturnType, semanticModel.Compilation)
                        ? Result.No
                        : Result.AssumeYes;
                }

                if (method is { IsExtensionMethod: true, ReducedFrom: { } reducedFrom } &&
                    reducedFrom.Parameters.TryFirst(out var parameter))
                {
                    _ = reducedFrom.TrySingleMethodDeclaration(cancellationToken, out var declaration);
                    return DisposedByReturnValue(new Target<SyntaxNode, IParameterSymbol, BaseMethodDeclarationSyntax>(null!, parameter, declaration), semanticModel, cancellationToken, visited) ? Result.Yes : Result.No;
                }
            }

            return Result.AssumeNo;
        }
    }
}
