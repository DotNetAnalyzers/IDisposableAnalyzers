namespace IDisposableAnalyzers
{
    using System.Threading;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
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
            if (location.FirstAncestorOrSelf<BlockSyntax>() is BlockSyntax block)
            {
                using (var walker = InvocationWalker.Borrow(block))
                {
                    foreach (var invocation in walker.Invocations)
                    {
                        if (location.IsExecutedBefore(invocation) == Result.Yes &&
                            IsDisposing(invocation, local, semanticModel, cancellationToken))
                        {
                            return true;
                        }
                    }
                }

                using (var walker = UsingStatementWalker.Borrow(block))
                {
                    foreach (var usingStatement in walker.UsingStatements)
                    {
                        if (location.IsExecutedBefore(usingStatement) == Result.Yes &&
                            usingStatement.Expression is IdentifierNameSyntax identifierName &&
                            identifierName.Identifier.ValueText == local.Name)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        internal static bool IsReturned(ISymbol symbol, SyntaxNode scope, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            using (var walker = ReturnValueWalker.Borrow(scope, Search.TopLevel, semanticModel, cancellationToken))
            {
                foreach (var value in walker)
                {
                    var candidate = value;
                    if (candidate is CastExpressionSyntax castExpression)
                    {
                        candidate = castExpression.Expression;
                    }

                    if (candidate is BinaryExpressionSyntax binary &&
                        binary.IsKind(SyntaxKind.AsExpression))
                    {
                        candidate = binary.Left;
                    }

                    var returnedSymbol = semanticModel.GetSymbolSafe(candidate, cancellationToken);
                    if (SymbolComparer.Equals(symbol, returnedSymbol))
                    {
                        return true;
                    }

                    if (candidate is ObjectCreationExpressionSyntax objectCreation)
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

                    if (candidate is InvocationExpressionSyntax invocation)
                    {
                        if (returnedSymbol == KnownSymbol.RxDisposable.Create &&
                            invocation.ArgumentList != null &&
                            invocation.ArgumentList.Arguments.TrySingle(out ArgumentSyntax argument) &&
                            argument.Expression is ParenthesizedLambdaExpressionSyntax lambda)
                        {
                            var body = lambda.Body;
                            using (var pooledInvocations = InvocationWalker.Borrow(body))
                            {
                                foreach (var disposeCandidate in pooledInvocations.Invocations)
                                {
                                    if (IsDisposing(disposeCandidate, symbol, semanticModel, cancellationToken))
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

        internal static bool IsAssignedToFieldOrProperty(ISymbol symbol, SyntaxNode scope, SemanticModel semanticModel, CancellationToken cancellationToken, PooledHashSet<ISymbol> visited = null)
        {
            if (AssignmentExecutionWalker.FirstWith(symbol, scope, Search.TopLevel, semanticModel, cancellationToken, out var assignment))
            {
                var left = semanticModel.GetSymbolSafe(assignment.Left, cancellationToken) ??
                           semanticModel.GetSymbolSafe((assignment.Left as ElementAccessExpressionSyntax)?.Expression, cancellationToken);
                if (left.IsEither<IParameterSymbol, ILocalSymbol>())
                {
                    using (visited = PooledHashSet<ISymbol>.BorrowOrIncrementUsage(visited))
                    {
                        return visited.Add(left) &&
                               IsAssignedToFieldOrProperty(left, scope, semanticModel, cancellationToken, visited);
                    }
                }

                return left.IsEither<IFieldSymbol, IPropertySymbol>();
            }

            return false;
        }

        internal static bool IsAddedToFieldOrProperty(ISymbol symbol, SyntaxNode scope, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            using (var pooledInvocations = InvocationWalker.Borrow(scope))
            {
                foreach (var invocation in pooledInvocations.Invocations)
                {
                    if (invocation.ArgumentList.Arguments.TryFirst(x => x.Expression is IdentifierNameSyntax identifierName && identifierName.Identifier.ValueText == symbol.Name, out var argument))
                    {
                        if (invocation.TryGetInvokedMethodName(out var name))
                        {
                            if (name == "Add" &&
                                symbol.Equals(semanticModel.GetSymbolSafe(argument.Expression, cancellationToken)))
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        internal static bool ShouldDispose(ILocalSymbol local, SyntaxNode location, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (location is AssignmentExpressionSyntax assignment &&
                assignment.Left is IdentifierNameSyntax identifierName &&
                identifierName.Identifier.ValueText == local.Name &&
                assignment.Parent is UsingStatementSyntax)
            {
                return false;
            }

            if (local.TrySingleDeclaration(cancellationToken, out SyntaxNode declaration))
            {
                if (declaration.Parent is UsingStatementSyntax ||
                    declaration.Parent is AnonymousFunctionExpressionSyntax)
                {
                    return false;
                }

                if (declaration.FirstAncestorOrSelf<MemberDeclarationSyntax>() is MemberDeclarationSyntax block)
                {
                    return !IsReturned(local, block, semanticModel, cancellationToken) &&
                           !IsAssignedToFieldOrProperty(local, block, semanticModel, cancellationToken) &&
                           !IsAddedToFieldOrProperty(local, block, semanticModel, cancellationToken) &&
                           !IsDisposedAfter(local, location, semanticModel, cancellationToken);
                }
            }

            return false;
        }

        internal static bool ShouldDispose(IParameterSymbol parameter, SyntaxNode location, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (location is AssignmentExpressionSyntax assignment &&
                assignment.Left is IdentifierNameSyntax identifierName &&
                identifierName.Identifier.ValueText == parameter.Name &&
                assignment.Parent is UsingStatementSyntax)
            {
                return false;
            }

            if (parameter.TrySingleDeclaration(cancellationToken, out var declaration) &&
                declaration.Parent is ParameterListSyntax parameterList &&
                parameterList.Parent is BaseMethodDeclarationSyntax methodDeclaration &&
                methodDeclaration.Body is BlockSyntax block)
            {
                return !IsReturned(parameter, block, semanticModel, cancellationToken) &&
                       !IsAssignedToFieldOrProperty(parameter, block, semanticModel, cancellationToken) &&
                       !IsAddedToFieldOrProperty(parameter, block, semanticModel, cancellationToken) &&
                       !IsDisposedAfter(parameter, location, semanticModel, cancellationToken);
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
                return IsArgumentDisposedByReturnValue(argument, semanticModel, cancellationToken).IsEither(Result.No, Result.AssumeNo) &&
                       IsArgumentAssignedToDisposable(argument, semanticModel, cancellationToken).IsEither(Result.No, Result.AssumeNo);
            }

            if (node.Parent is MemberAccessExpressionSyntax memberAccess)
            {
                return IsArgumentDisposedByInvocationReturnValue(memberAccess, semanticModel, cancellationToken).IsEither(Result.No, Result.AssumeNo);
            }

            return false;
        }
    }
}
