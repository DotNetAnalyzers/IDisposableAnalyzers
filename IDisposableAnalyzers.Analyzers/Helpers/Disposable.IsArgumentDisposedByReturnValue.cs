namespace IDisposableAnalyzers
{
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static partial class Disposable
    {
        internal static Result IsArgumentDisposedByExtensionMethodReturnValue(InvocationExpressionSyntax invocation, SemanticModel semanticModel, CancellationToken cancellationToken, PooledHashSet<SyntaxNode> visited = null)
        {
            if (semanticModel.GetSymbolSafe(invocation, cancellationToken) is IMethodSymbol method &&
                method.IsExtensionMethod)
            {
                if (method.ContainingType.DeclaringSyntaxReferences.Length == 0)
                {
                    return method.ReturnsVoid ||
                           !IsAssignableTo(method.ReturnType)
                        ? Result.No
                        : Result.AssumeYes;
                }

                using (var returnWalker = ReturnValueWalker.Borrow(invocation, Search.Recursive, semanticModel, cancellationToken))
                {
                    using (visited = PooledHashSet<SyntaxNode>.BorrowOrIncrementUsage(visited))
                    {
                        if (!visited.Add(invocation))
                        {
                            return Result.Unknown;
                        }

                        var parameter = method.Parameters[0];
                        foreach (var returnValue in returnWalker)
                        {
                            if (returnValue is ObjectCreationExpressionSyntax nestedObjectCreation &&
                                nestedObjectCreation.TryGetMatchingArgument(parameter, out var nestedArgument))
                            {
                                return IsArgumentDisposedByReturnValue(nestedArgument, semanticModel, cancellationToken, visited);
                            }

                            if (returnValue is InvocationExpressionSyntax nestedInvocation &&
                                nestedInvocation.TryGetMatchingArgument(parameter, out nestedArgument))
                            {
                                return IsArgumentDisposedByReturnValue(nestedArgument, semanticModel, cancellationToken, visited);
                            }

                            if (returnValue is MemberAccessExpressionSyntax memberAccess &&
                                memberAccess.Expression is InvocationExpressionSyntax extensionInvocation)
                            {
                                return IsArgumentDisposedByExtensionMethodReturnValue(extensionInvocation, semanticModel, cancellationToken, visited);
                            }
                        }
                    }
                }

                return Result.No;
            }

            return Result.Unknown;
        }

        internal static Result IsArgumentDisposedByReturnValue(ArgumentSyntax argument, SemanticModel semanticModel, CancellationToken cancellationToken, PooledHashSet<SyntaxNode> visited = null)
        {
            if (argument?.Parent is ArgumentListSyntax argumentList)
            {
                if (argumentList.Parent is InvocationExpressionSyntax invocation &&
                    semanticModel.GetSymbolSafe(invocation, cancellationToken) is IMethodSymbol method)
                {
                    if (method.ContainingType.DeclaringSyntaxReferences.Length == 0)
                    {
                        return method.ReturnsVoid ||
                               !IsAssignableTo(method.ReturnType)
                            ? Result.No
                            : Result.AssumeYes;
                    }

                    using (var returnWalker = ReturnValueWalker.Borrow(invocation, Search.Recursive, semanticModel, cancellationToken))
                    {
                        using (visited = PooledHashSet<SyntaxNode>.BorrowOrIncrementUsage(visited))
                        {
                            if (!visited.Add(argument))
                            {
                                return Result.Unknown;
                            }

                            foreach (var returnValue in returnWalker)
                            {
                                if (returnValue is ObjectCreationExpressionSyntax nestedObjectCreation &&
                                    invocation.TryGetMatchingParameter(argument, semanticModel, cancellationToken, out var parameter) &&
                                    nestedObjectCreation.TryGetMatchingArgument(parameter, out var nestedArgument))
                                {
                                    return IsArgumentDisposedByReturnValue(nestedArgument, semanticModel, cancellationToken, visited);
                                }

                                if (returnValue is InvocationExpressionSyntax nestedInvocation &&
                                    invocation.TryGetMatchingParameter(argument, semanticModel, cancellationToken, out parameter) &&
                                    nestedInvocation.TryGetMatchingArgument(parameter, out nestedArgument))
                                {
                                    return IsArgumentDisposedByReturnValue(nestedArgument, semanticModel, cancellationToken, visited);
                                }

                                if (returnValue is MemberAccessExpressionSyntax memberAccess &&
                                    memberAccess.Expression is InvocationExpressionSyntax extensionInvocation)
                                {
                                    return IsArgumentDisposedByExtensionMethodReturnValue(extensionInvocation, semanticModel, cancellationToken, visited);
                                }
                            }
                        }
                    }

                    return Result.No;
                }

                if (argumentList.Parent is ObjectCreationExpressionSyntax ||
                    argumentList.Parent is ConstructorInitializerSyntax)
                {
                    if (TryGetAssignedFieldOrProperty(argument, semanticModel, cancellationToken, out var member, out var ctor) &&
                        member != null)
                    {
                        var initializer = argument.FirstAncestorOrSelf<ConstructorInitializerSyntax>();
                        if (initializer != null)
                        {
                            if (semanticModel.GetDeclaredSymbolSafe(initializer.Parent, cancellationToken) is IMethodSymbol chainedCtor &&
                                chainedCtor.ContainingType != member.ContainingType)
                            {
                                if (TryGetDisposeMethod(chainedCtor.ContainingType, Search.TopLevel, out var disposeMethod))
                                {
                                    return IsMemberDisposed(member, disposeMethod, semanticModel, cancellationToken)
                                        ? Result.Yes
                                        : Result.No;
                                }
                            }
                        }

                        return IsMemberDisposed(member, ctor.ContainingType, semanticModel, cancellationToken);
                    }

                    if (ctor == null)
                    {
                        return Result.AssumeYes;
                    }

                    if (ctor.ContainingType.DeclaringSyntaxReferences.Length == 0)
                    {
                        return IsAssignableTo(ctor.ContainingType) ? Result.AssumeYes : Result.No;
                    }

                    return Result.No;
                }
            }

            return Result.Unknown;
        }

        private static bool TryGetAssignedFieldOrProperty(ArgumentSyntax argument, SemanticModel semanticModel, CancellationToken cancellationToken, out ISymbol member, out IMethodSymbol ctor)
        {
            if (TryGetConstructor(argument, semanticModel, cancellationToken, out ctor))
            {
                return TryGetAssignedFieldOrProperty(argument, ctor, semanticModel, cancellationToken, out member);
            }

            member = null;
            return false;
        }

        private static bool TryGetConstructor(ArgumentSyntax argument, SemanticModel semanticModel, CancellationToken cancellationToken, out IMethodSymbol ctor)
        {
            var objectCreation = argument.FirstAncestor<ObjectCreationExpressionSyntax>();
            if (objectCreation != null)
            {
                ctor = semanticModel.GetSymbolSafe(objectCreation, cancellationToken) as IMethodSymbol;
                return ctor != null;
            }

            var initializer = argument.FirstAncestor<ConstructorInitializerSyntax>();
            if (initializer != null)
            {
                ctor = semanticModel.GetSymbolSafe(initializer, cancellationToken);
                return ctor != null;
            }

            ctor = null;
            return false;
        }

        private static bool TryGetAssignedFieldOrProperty(ArgumentSyntax argument, IMethodSymbol method, SemanticModel semanticModel, CancellationToken cancellationToken, out ISymbol member)
        {
            member = null;
            if (method == null)
            {
                return false;
            }

            foreach (var reference in method.DeclaringSyntaxReferences)
            {
                var methodDeclaration = reference.GetSyntax(cancellationToken) as BaseMethodDeclarationSyntax;
                if (methodDeclaration == null)
                {
                    continue;
                }

                if (!methodDeclaration.TryGetMatchingParameter(argument, out var paremeter))
                {
                    continue;
                }

                var parameterSymbol = semanticModel.GetDeclaredSymbolSafe(paremeter, cancellationToken);
                if (methodDeclaration.Body.TryGetAssignment(parameterSymbol, semanticModel, cancellationToken, out var assignment))
                {
                    member = semanticModel.GetSymbolSafe(assignment.Left, cancellationToken);
                    if (member is IFieldSymbol ||
                        member is IPropertySymbol)
                    {
                        return true;
                    }
                }

                var ctor = reference.GetSyntax(cancellationToken) as ConstructorDeclarationSyntax;
                if (ctor?.Initializer != null)
                {
                    foreach (var arg in ctor.Initializer.ArgumentList.Arguments)
                    {
                        var argSymbol = semanticModel.GetSymbolSafe(arg.Expression, cancellationToken);
                        if (parameterSymbol.Equals(argSymbol))
                        {
                            var chained = semanticModel.GetSymbolSafe(ctor.Initializer, cancellationToken);
                            return TryGetAssignedFieldOrProperty(arg, chained, semanticModel, cancellationToken, out member);
                        }
                    }
                }
            }

            return false;
        }
    }
}
