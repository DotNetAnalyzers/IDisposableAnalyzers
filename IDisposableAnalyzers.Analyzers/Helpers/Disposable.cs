namespace IDisposableAnalyzers
{
    using System.Threading;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static partial class Disposable
    {
        internal static bool IsPotentiallyAssignableTo(ExpressionSyntax candidate, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (candidate == null ||
                candidate.IsMissing ||
                candidate is LiteralExpressionSyntax)
            {
                return false;
            }

            if (candidate is ObjectCreationExpressionSyntax objectCreation)
            {
                return IsAssignableTo(semanticModel.GetTypeInfoSafe(objectCreation, cancellationToken).Type);
            }

            return IsPotentiallyAssignableTo(semanticModel.GetTypeInfoSafe(candidate, cancellationToken).Type);
        }

        internal static bool IsPotentiallyAssignableTo(ITypeSymbol type)
        {
            if (type == null ||
                type is IErrorTypeSymbol)
            {
                return false;
            }

            if (type.IsValueType &&
                !IsAssignableTo(type))
            {
                return false;
            }

            if (type.IsSealed &&
                !IsAssignableTo(type))
            {
                return false;
            }

            return true;
        }

        internal static bool IsAssignableTo(ITypeSymbol type)
        {
            if (type == null)
            {
                return false;
            }

            if (type is ITypeParameterSymbol typeParameter)
            {
                foreach (var constraintType in typeParameter.ConstraintTypes)
                {
                    if (IsAssignableTo(constraintType))
                    {
                        return true;
                    }
                }

                return false;
            }

            // https://blogs.msdn.microsoft.com/pfxteam/2012/03/25/do-i-need-to-dispose-of-tasks/
            if (type == KnownSymbol.Task)
            {
                return false;
            }

            return type == KnownSymbol.IDisposable ||
                   type.Is(KnownSymbol.IDisposable);
        }

        internal static bool TryGetDisposeMethod(ITypeSymbol type, Search search, out IMethodSymbol disposeMethod)
        {
            disposeMethod = null;
            if (type == null)
            {
                return false;
            }

            var disposers = type.GetMembers("Dispose");
            switch (disposers.Length)
            {
                case 0:
                    var baseType = type.BaseType;
                    if (search == Search.Recursive &&
                        IsAssignableTo(baseType))
                    {
                        return TryGetDisposeMethod(baseType, Search.Recursive, out disposeMethod);
                    }

                    return false;
                case 1:
                    disposeMethod = disposers[0] as IMethodSymbol;
                    if (disposeMethod == null)
                    {
                        return false;
                    }

                    return (disposeMethod.Parameters.Length == 0 &&
                            disposeMethod.DeclaredAccessibility == Accessibility.Public) ||
                           (disposeMethod.Parameters.Length == 1 &&
                            disposeMethod.Parameters[0].Type == KnownSymbol.Boolean);
                case 2:
                    if (disposers.TrySingle(x => (x as IMethodSymbol)?.Parameters.Length == 1, out ISymbol temp))
                    {
                        disposeMethod = temp as IMethodSymbol;
                        return disposeMethod != null &&
                               disposeMethod.Parameters[0].Type == KnownSymbol.Boolean;
                    }

                    break;
            }

            return false;
        }

        internal static bool TryGetBaseVirtualDisposeMethod(ITypeSymbol type, out IMethodSymbol result)
        {
            bool IsVirtualDispose(IMethodSymbol m)
            {
                return m.IsVirtual &&
                       m.ReturnsVoid &&
                       m.Parameters.Length == 1 &&
                       m.Parameters[0].Type == KnownSymbol.Boolean;
            }

            var baseType = type.BaseType;
            while (baseType != null)
            {
                if (baseType.TrySingleMethodRecursive("Dispose", IsVirtualDispose, out result))
                {
                    return true;
                }

                baseType = baseType.BaseType;
            }

            result = null;
            return false;
        }

        internal static bool IsDisposedAfter(ISymbol local, SyntaxNode location, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            using (var pooled = InvocationWalker.Borrow(location.FirstAncestorOrSelf<BlockSyntax>()))
            {
                foreach (var invocation in pooled.Invocations)
                {
                    if (IsDisposing(invocation, local, semanticModel, cancellationToken))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        internal static bool IsReturned(ISymbol symbol, BlockSyntax block, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            using (var walker = ReturnValueWalker.Borrow(block, Search.TopLevel, semanticModel, cancellationToken))
            {
                foreach (var value in walker)
                {
                    var returnedSymbol = semanticModel.GetSymbolSafe(value, cancellationToken);
                    if (SymbolComparer.Equals(symbol, returnedSymbol))
                    {
                        return true;
                    }

                    if (value is ObjectCreationExpressionSyntax objectCreation)
                    {
                        if (objectCreation.ArgumentList != null)
                        {
                            foreach (var argument in objectCreation.ArgumentList.Arguments)
                            {
                                var arg = semanticModel.GetSymbolSafe(argument.Expression, cancellationToken);
                                if (SymbolComparer.Equals(symbol, arg))
                                {
                                    return true;
                                }
                            }
                        }

                        if (objectCreation.Initializer != null)
                        {
                            foreach (var argument in objectCreation.Initializer.Expressions)
                            {
                                var arg = semanticModel.GetSymbolSafe(argument, cancellationToken);
                                if (SymbolComparer.Equals(symbol, arg))
                                {
                                    return true;
                                }
                            }
                        }
                    }

                    if (value is InvocationExpressionSyntax invocation)
                    {
                        if (returnedSymbol == KnownSymbol.RxDisposable.Create &&
                            invocation.ArgumentList != null &&
                            invocation.ArgumentList.Arguments.TrySingle(out ArgumentSyntax argument) &&
                            argument.Expression is ParenthesizedLambdaExpressionSyntax lambda)
                        {
                            var body = lambda.Body;
                            using (var pooledInvocations = InvocationWalker.Borrow(body))
                            {
                                foreach (var candidate in pooledInvocations.Invocations)
                                {
                                    if (IsDisposing(candidate, symbol, semanticModel, cancellationToken))
                                    {
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }

        internal static bool IsAssignedToFieldOrProperty(ISymbol symbol, BlockSyntax block, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            AssignmentExpressionSyntax assignment = null;
            if (block?.TryGetAssignment(symbol, semanticModel, cancellationToken, out assignment) == true)
            {
                var left = semanticModel.GetSymbolSafe(assignment.Left, cancellationToken) ??
                           semanticModel.GetSymbolSafe((assignment.Left as ElementAccessExpressionSyntax)?.Expression, cancellationToken);
                return left is IFieldSymbol || left is IPropertySymbol || left is ILocalSymbol || left is IParameterSymbol;
            }

            return false;
        }

        internal static bool IsAddedToFieldOrProperty(ISymbol symbol, BlockSyntax block, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            using (var pooledInvocations = InvocationWalker.Borrow(block))
            {
                foreach (var invocation in pooledInvocations.Invocations)
                {
                    var method = semanticModel.GetSymbolSafe(invocation, cancellationToken) as IMethodSymbol;
                    if (method?.Name == "Add")
                    {
                        using (var nameWalker = IdentifierNameWalker.Borrow(invocation.ArgumentList))
                        {
                            foreach (var identifierName in nameWalker.IdentifierNames)
                            {
                                var argSymbol = semanticModel.GetSymbolSafe(identifierName, cancellationToken);
                                if (symbol.Equals(argSymbol))
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

        internal static bool ShouldDispose(ILocalSymbol local, SyntaxNode location, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (local.TrySingleDeclaration(cancellationToken, out var declaration))
            {
                if (declaration.Parent is UsingStatementSyntax ||
                    declaration.Parent is AnonymousFunctionExpressionSyntax)
                {
                    return false;
                }

                if (declaration.FirstAncestorOrSelf<BlockSyntax>() is BlockSyntax block)
                {
                    return !IsReturned(local, block, semanticModel, cancellationToken) &&
                           !IsAssignedToFieldOrProperty(local, block, semanticModel, cancellationToken) &&
                           !IsAddedToFieldOrProperty(local, block, semanticModel, cancellationToken) &&
                           !IsDisposedAfter(local, location, semanticModel, cancellationToken);
                }
            }

            return false;
        }

        internal static bool IsIgnored(ExpressionSyntax node, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (node.Parent is AnonymousFunctionExpressionSyntax ||
                node.Parent is UsingStatementSyntax ||
                node.Parent is EqualsValueClauseSyntax ||
                node.Parent is ReturnStatementSyntax ||
                node.Parent is ArrowExpressionClauseSyntax)
            {
                return false;
            }

            if (node.Parent is StatementSyntax)
            {
                return true;
            }

            if (node.Parent is ArgumentSyntax argument)
            {
                return Disposable.IsArgumentDisposedByReturnValue(argument, semanticModel, cancellationToken)
                                 .IsEither(Result.No, Result.AssumeNo);
            }

            if (node.Parent is MemberAccessExpressionSyntax memberAccess)
            {
                return Disposable.IsArgumentDisposedByInvocationReturnValue(memberAccess, semanticModel, cancellationToken)
                                 .IsEither(Result.No, Result.AssumeNo);
            }

            return false;
        }
    }
}
