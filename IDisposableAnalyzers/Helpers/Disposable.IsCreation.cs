namespace IDisposableAnalyzers
{
    using System.Collections.Immutable;
    using System.Threading;

    using Gu.Roslyn.AnalyzerExtensions;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static partial class Disposable
    {
        internal static bool IsAlreadyAssignedWithCreated(ExpressionSyntax disposable, SemanticModel semanticModel, CancellationToken cancellationToken, out ISymbol? assignedSymbol)
        {
            if (!IsPotentiallyAssignableFrom(disposable, semanticModel, cancellationToken))
            {
                assignedSymbol = null;
                return false;
            }

            if (disposable is { Parent: AssignmentExpressionSyntax { Parent: ArrowExpressionClauseSyntax { Parent: ConstructorDeclarationSyntax _ } } })
            {
                assignedSymbol = null;
                return false;
            }

            var symbol = semanticModel.GetSymbolSafe(disposable, cancellationToken);
            if (symbol is null)
            {
                assignedSymbol = null;
                return false;
            }

            if (symbol is IPropertySymbol { SetMethod: { } } property &&
                IsAssignableFrom(property.Type, semanticModel.Compilation) &&
                property.TryGetSetter(cancellationToken, out var setter) &&
                (setter.ExpressionBody != null || setter.Body != null))
            {
                using var assignedSymbols = PooledSet<ISymbol>.Borrow();
                using (var pooledAssigned = AssignmentExecutionWalker.Borrow(setter, SearchScope.Recursive, semanticModel, cancellationToken))
                {
                    foreach (var assigned in pooledAssigned.Assignments)
                    {
                        if (assigned is { Left: { } left, Right: IdentifierNameSyntax { Identifier: { ValueText: "value" } } } &&
                            IsPotentiallyAssignableFrom(left, semanticModel, cancellationToken) &&
                            semanticModel.GetSymbolSafe(left, cancellationToken) is { } candidate &&
                            candidate.IsEitherKind(SymbolKind.Field, SymbolKind.Property))
                        {
                            assignedSymbols.Add(candidate);
                        }
                    }
                }

                foreach (var candidate in assignedSymbols)
                {
                    if (IsAssignedWithCreated(candidate, disposable, semanticModel, cancellationToken))
                    {
                        assignedSymbol = candidate;
                        return true;
                    }
                }

                assignedSymbol = null;
                return false;
            }

            if (symbol is IParameterSymbol &&
                disposable.TryFirstAncestor<ArrowExpressionClauseSyntax>(out _))
            {
                assignedSymbol = null;
                return false;
            }

            using var assignedValues = AssignedValueWalker.Borrow(disposable, semanticModel, cancellationToken);
            assignedSymbol = assignedValues.CurrentSymbol;
            if (assignedValues.Count == 1 &&
                disposable.Parent is AssignmentExpressionSyntax { Parent: ParenthesizedExpressionSyntax { Parent: BinaryExpressionSyntax { } binary } } &&
                binary.IsKind(SyntaxKind.CoalesceExpression))
            {
                // lazy
                return false;
            }

            if (symbol.IsEither<IParameterSymbol, ILocalSymbol>())
            {
                assignedValues.RemoveAll(x => IsReturnedBefore(x));
            }

            using var recursive = RecursiveValues.Borrow(assignedValues, semanticModel, cancellationToken);
            return IsAnyCreation(recursive, semanticModel, cancellationToken);

            bool IsReturnedBefore(ExpressionSyntax expression)
            {
                if (expression.TryFirstAncestor(out BlockSyntax? block) &&
                    block.Statements.TryFirstOfType(out ReturnStatementSyntax _))
                {
                    if (expression.TryFirstAncestor<ForEachStatementSyntax>(out _) ||
                        expression.TryFirstAncestor<ForStatementSyntax>(out _) ||
                        expression.TryFirstAncestor<WhileStatementSyntax>(out _))
                    {
                        return true;
                    }

                    return !block.Contains(disposable) &&
                           block.SharesAncestor(disposable, out MemberDeclarationSyntax _);
                }

                return false;
            }
        }

        internal static bool IsAssignedWithCreated(ISymbol symbol, ExpressionSyntax location, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (symbol.Kind == SymbolKind.Discard)
            {
                return false;
            }

            using var assignedValues = AssignedValueWalker.Borrow(symbol, location, semanticModel, cancellationToken);
            using var recursive = RecursiveValues.Borrow(assignedValues, semanticModel, cancellationToken);
            return IsAnyCreation(recursive, semanticModel, cancellationToken);
        }

        /// <summary>
        /// Check if any path returns a created IDisposable.
        /// </summary>
        internal static bool IsCreation(ExpressionSyntax candidate, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            switch (candidate.Kind())
            {
                case SyntaxKind.NullLiteralExpression:
                case SyntaxKind.StringLiteralExpression:
                case SyntaxKind.NumericLiteralExpression:
                case SyntaxKind.TrueLiteralExpression:
                case SyntaxKind.FalseLiteralExpression:
                case SyntaxKind.BaseExpression:
                case SyntaxKind.ThisExpression:
                    return false;
            }

            if (!IsPotentiallyAssignableFrom(candidate, semanticModel, cancellationToken))
            {
                return false;
            }

            if (candidate is IdentifierNameSyntax { Identifier: { ValueText: "value" } } &&
                candidate.FirstAncestor<AccessorDeclarationSyntax>() is { } accessor &&
                accessor.IsKind(SyntaxKind.SetAccessorDeclaration))
            {
                return false;
            }

            if (candidate is ObjectCreationExpressionSyntax)
            {
                return true;
            }

            using var recursive = RecursiveValues.Borrow(new[] { candidate }, semanticModel, cancellationToken);
            return IsAnyCreation(recursive, semanticModel, cancellationToken);
        }

        internal static bool IsAnyCreation(RecursiveValues values, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (values.IsEmpty)
            {
                return false;
            }

            values.Reset();
            while (values.MoveNext())
            {
                if (IsCreationCore(values.Current, semanticModel, cancellationToken))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Check if any path returns a created IDisposable.
        /// </summary>
        private static bool IsCreationCore(ExpressionSyntax candidate, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (candidate.IsMissing)
            {
                return false;
            }

            if (candidate is LiteralExpressionSyntax ||
                candidate is DefaultExpressionSyntax ||
                candidate is TypeOfExpressionSyntax ||
                candidate is InstanceExpressionSyntax ||
                candidate is ElementAccessExpressionSyntax)
            {
                return false;
            }

            if (candidate is ObjectCreationExpressionSyntax ||
                candidate is ArrayCreationExpressionSyntax ||
                candidate is ImplicitArrayCreationExpressionSyntax ||
                candidate is InitializerExpressionSyntax)
            {
                switch (semanticModel.GetType(candidate, cancellationToken))
                {
                    case { } type:
                        return IsAssignableFrom(type, semanticModel.Compilation);
                    case null:
                        return false;
                }
            }

            if (semanticModel.TryGetSymbol(candidate, cancellationToken, out var symbol))
            {
                return symbol switch
                {
                    IParameterSymbol _ => false,
                    ILocalSymbol _ => false,
                    IFieldSymbol _ => false,
                    IPropertySymbol { ContainingType: { MetadataName: "PasswordBox" }, MetadataName: "SecurePassword" } => true,
                    IPropertySymbol _ => false,
                    IMethodSymbol { MetadataName: "CreateConnection" } method => InferFromReturnType(method),
                    IMethodSymbol { MetadataName: nameof(ToString) } => false,
                    IMethodSymbol { MetadataName: nameof(GetHashCode) } => false,
                    IMethodSymbol { MetadataName: nameof(Equals) } => false,
                    IMethodSymbol { MetadataName: nameof(ReferenceEquals) } => false,
                    IMethodSymbol { ContainingType: { IsGenericType: true }, MetadataName: "GetEnumerator" } => true,
                    IMethodSymbol { ContainingType: { IsGenericType: false }, MetadataName: "GetEnumerator" } => false,
                    IMethodSymbol { ContainingType: { MetadataName: "Activator" }, MetadataName: "CreateInstance", IsGenericMethod: false } => InferFromUse(),
                    IMethodSymbol { ContainingType: { MetadataName: "Activator" }, MetadataName: "CreateInstance", IsGenericMethod: true } method => InferFromReturnType(method),
                    IMethodSymbol { ContainingType: { MetadataName: "ActivatorUtilities" }, MetadataName: "CreateInstance", IsGenericMethod: true } method => InferFromReturnType(method),
                    IMethodSymbol { ContainingType: { MetadataName: "ConstructorInfo" }, MetadataName: "Invoke" } => InferFromUse(),
                    IMethodSymbol { ContainingType: { MetadataName: "Enumerable" } } => false,
                    IMethodSymbol { ContainingType: { MetadataName: "HttpResponseMessage" }, MetadataName: "EnsureSuccessStatusCode" } => false,
                    IMethodSymbol { ContainingType: { MetadataName: "ResourceManager" } } => false,
                    IMethodSymbol { ContainingType: { MetadataName: "Task" } } => false,
                    IMethodSymbol { ContainingType: { MetadataName: "Task`1" } } => false,
                    IMethodSymbol { ContainingType: { MetadataName: "ValueTask`1" } } => false,
                    IMethodSymbol { ReturnType: { MetadataName: "Task" } } => false,
                    IMethodSymbol { IsExtensionMethod: true, ReturnType: { MetadataName: "ILoggerFactory" } } => false,
                    IMethodSymbol method => IsMethodCreating(method, semanticModel.Compilation),
                    _ => false,
                };

                bool InferFromUse()
                {
                    if (candidate.Parent is CastExpressionSyntax { Type: { } castType } &&
                        semanticModel.TryGetType(castType, cancellationToken, out var type))
                    {
                        if (IsAssignableFrom(type, semanticModel.Compilation))
                        {
                            return true;
                        }

                        if (type.IsSealed)
                        {
                            return false;
                        }
                    }

                    return false;
                }

                bool InferFromReturnType(IMethodSymbol method)
                {
                    if (IsAssignableFrom(method.ReturnType, semanticModel.Compilation))
                    {
                        return true;
                    }

                    if (method.ReturnType.IsSealed)
                    {
                        return false;
                    }

                    return false;
                }

                static bool IsMethodCreating(IMethodSymbol method, Compilation compilation)
                {
                    if (method.ReturnType is INamedTypeSymbol { IsGenericType: true } returnType &&
                        method.ReturnType == KnownSymbol.TaskOfT)
                    {
                        return returnType.TypeArguments.TrySingle(out var typeArg) &&
                               IsAssignableFrom(typeArg, compilation);
                    }

                    if (method.ContainingType is { IsGenericType: true } &&
                        AnyMatch(method.ReturnType, method.ContainingType.TypeArguments))
                    {
                        return false;
                    }

                    if (method.IsGenericMethod &&
                        AnyMatch(method.ReturnType, method.TypeArguments))
                    {
                        return false;
                    }

                    if (!IsAssignableFrom(method.ReturnType, compilation))
                    {
                        return false;
                    }

                    if (method.TryGetThisParameter(out var thisParameter) &&
                        TypeSymbolComparer.Equal(thisParameter.Type, method.ReturnType))
                    {
                        return false;
                    }

                    return IsAssignableFrom(method.ReturnType, compilation);

                    static bool AnyMatch(ITypeSymbol returnType, ImmutableArray<ITypeSymbol> typeArguments)
                    {
                        foreach (var typeArgument in typeArguments)
                        {
                            if (TypeSymbolComparer.Equal(returnType, typeArgument))
                            {
                                return true;
                            }
                        }

                        return false;
                    }
                }
            }

            return false;
        }
    }
}
