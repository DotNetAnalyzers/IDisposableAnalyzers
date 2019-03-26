namespace IDisposableAnalyzers
{
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed partial class DisposableWalker
    {
        public static bool DisposedByReturnValue(ArgumentSyntax candidate, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<(string, SyntaxNode)> visited, out ExpressionSyntax invocationOrObjectCreation)
        {
            if (candidate.Parent is ArgumentListSyntax argumentList &&
                semanticModel.TryGetSymbol(argumentList.Parent, cancellationToken, out IMethodSymbol method))
            {
                invocationOrObjectCreation = argumentList.Parent as ExpressionSyntax;
                if (invocationOrObjectCreation == null)
                {
                    return false;
                }

                if (method.MethodKind == MethodKind.Constructor)
                {
                    if (method.ContainingType == KnownSymbol.SingleAssignmentDisposable ||
                        method.ContainingType == KnownSymbol.RxDisposable ||
                        method.ContainingType == KnownSymbol.CompositeDisposable)
                    {
                        return true;
                    }

                    if (Disposable.IsAssignableFrom(method.ContainingType, semanticModel.Compilation))
                    {
                        if (method.ContainingType == KnownSymbol.BinaryReader ||
                            method.ContainingType == KnownSymbol.BinaryWriter ||
                            method.ContainingType == KnownSymbol.StreamReader ||
                            method.ContainingType == KnownSymbol.StreamWriter ||
                            method.ContainingType == KnownSymbol.CryptoStream ||
                            method.ContainingType == KnownSymbol.DeflateStream ||
                            method.ContainingType == KnownSymbol.GZipStream ||
                            method.ContainingType == KnownSymbol.StreamMemoryBlockProvider)
                        {
                            if (method.TryFindParameter("leaveOpen", out var leaveOpenParameter) &&
                                argumentList.TryFind(leaveOpenParameter, out var leaveOpenArgument) &&
                                leaveOpenArgument.Expression is LiteralExpressionSyntax literal &&
                                literal.IsKind(SyntaxKind.TrueLiteralExpression))
                            {
                                return false;
                            }

                            return true;
                        }

                        if (method.TryFindParameter(candidate, out var parameter))
                        {
                            if (parameter.Type.IsAssignableTo(KnownSymbol.HttpMessageHandler, semanticModel.Compilation) &&
                                method.ContainingType.IsAssignableTo(KnownSymbol.HttpClient, semanticModel.Compilation))
                            {
                                if (method.TryFindParameter("disposeHandler", out var leaveOpenParameter) &&
                                    argumentList.TryFind(leaveOpenParameter, out var leaveOpenArgument) &&
                                    leaveOpenArgument.Expression is LiteralExpressionSyntax literal &&
                                    literal.IsKind(SyntaxKind.FalseLiteralExpression))
                                {
                                    return false;
                                }

                                return true;
                            }

                            return DisposedByReturnValue(parameter, semanticModel, cancellationToken, visited);
                        }
                    }
                }
                else if (method.MethodKind == MethodKind.Ordinary &&
                         Disposable.IsAssignableFrom(method.ReturnType, semanticModel.Compilation) &&
                         method.TryFindParameter(candidate, out var parameter))
                {
                    return DisposedByReturnValue(parameter, semanticModel, cancellationToken, visited);
                }
            }

            invocationOrObjectCreation = null;
            return false;
        }

        private static bool DisposedByReturnValue(ExpressionSyntax candidate, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<(string, SyntaxNode)> visited, out ExpressionSyntax invocationOrObjectCreation)
        {
            switch (candidate.Parent.Kind())
            {
                case SyntaxKind.CastExpression:
                case SyntaxKind.AsExpression:
                case SyntaxKind.ConditionalExpression:
                case SyntaxKind.CoalesceExpression:
                    return DisposedByReturnValue((ExpressionSyntax)candidate.Parent, semanticModel, cancellationToken, visited, out invocationOrObjectCreation);
            }

            switch (candidate.Parent)
            {
                case ArgumentSyntax argument:
                    return DisposedByReturnValue(argument, semanticModel, cancellationToken, visited, out invocationOrObjectCreation);
                case InitializerExpressionSyntax initializer when initializer.Parent is ObjectCreationExpressionSyntax objectCreation &&
                                                                  semanticModel.TryGetType(objectCreation, cancellationToken, out var type) &&
                                                                  type == KnownSymbol.CompositeDisposable:
                    invocationOrObjectCreation = objectCreation;
                    return true;
                default:
                    invocationOrObjectCreation = null;
                    return false;
            }
        }

        private static bool DisposedByReturnValue(IParameterSymbol candidate, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<(string, SyntaxNode)> visited)
        {
            if (candidate.TrySingleDeclaration(cancellationToken, out var parameterSyntax) &&
                candidate.ContainingSymbol is IMethodSymbol method)
            {
                if (visited.CanVisit(parameterSyntax, out visited))
                {
                    using (visited)
                    {
                        using (var walker = CreateUsagesWalker(new LocalOrParameter(candidate), semanticModel, cancellationToken))
                        {
                            foreach (var usage in walker.usages)
                            {
                                switch (usage.Parent.Kind())
                                {
                                    case SyntaxKind.ReturnStatement:
                                    case SyntaxKind.ArrowExpressionClause:
                                        return true;
                                }

                                if (Assigns(usage, semanticModel, cancellationToken, visited, out var fieldOrProperty) &&
                                    DisposableMember.IsDisposed(fieldOrProperty, method.ContainingType, semanticModel, cancellationToken).IsEither(Result.Yes, Result.AssumeYes))
                                {
                                    return true;
                                }

                                if (usage.Parent is ArgumentSyntax argument &&
                                    DisposedByReturnValue(argument, semanticModel, cancellationToken, visited, out var invocationOrObjectCreation) &&
                                    Returns(invocationOrObjectCreation, semanticModel, cancellationToken, visited))
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }
    }
}
