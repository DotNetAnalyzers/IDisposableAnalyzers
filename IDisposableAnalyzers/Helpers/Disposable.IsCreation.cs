namespace IDisposableAnalyzers
{
    using System;
    using System.Collections.Immutable;
    using System.Threading;

    using Gu.Roslyn.AnalyzerExtensions;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static partial class Disposable
    {
        internal static Result IsAlreadyAssignedWithCreated(ExpressionSyntax disposable, SemanticModel semanticModel, CancellationToken cancellationToken, out ISymbol? assignedSymbol)
        {
            if (!IsPotentiallyAssignableFrom(disposable, semanticModel, cancellationToken))
            {
                assignedSymbol = null;
                return Result.No;
            }

            if (disposable is { Parent: AssignmentExpressionSyntax { Parent: ArrowExpressionClauseSyntax { Parent: ConstructorDeclarationSyntax _ } } })
            {
                assignedSymbol = null;
                return Result.No;
            }

            var symbol = semanticModel.GetSymbolSafe(disposable, cancellationToken);
            if (symbol is null)
            {
                assignedSymbol = null;
                return Result.No;
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

                assignedSymbol = null;
                var result = Result.No;
                foreach (var candidate in assignedSymbols)
                {
                    switch (IsAssignedWithCreated(candidate, disposable, semanticModel, cancellationToken))
                    {
                        case Result.Unknown:
                            if (result == Result.No)
                            {
                                assignedSymbol = candidate;
                                result = Result.Unknown;
                            }

                            break;
                        case Result.Yes:
                            assignedSymbol = candidate;
                            return Result.Yes;
                        case Result.AssumeYes:
                            assignedSymbol = candidate;
                            result = Result.AssumeYes;
                            break;
                        case Result.No:
                        case Result.AssumeNo:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(disposable), disposable, "Unknown result type");
                    }
                }

                return result;
            }

            if (symbol is IParameterSymbol &&
                disposable.TryFirstAncestor<ArrowExpressionClauseSyntax>(out _))
            {
                assignedSymbol = null;
                return Result.No;
            }

            using var assignedValues = AssignedValueWalker.Borrow(disposable, semanticModel, cancellationToken);
            assignedSymbol = assignedValues.CurrentSymbol;
            if (assignedValues.Count == 1 &&
                disposable.Parent is AssignmentExpressionSyntax { Parent: ParenthesizedExpressionSyntax { Parent: BinaryExpressionSyntax { } binary } } &&
                binary.IsKind(SyntaxKind.CoalesceExpression))
            {
                // lazy
                return Result.No;
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

        internal static Result IsAssignedWithCreated(ISymbol symbol, ExpressionSyntax location, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (symbol.Kind == SymbolKind.Discard)
            {
                return Result.No;
            }

            using var assignedValues = AssignedValueWalker.Borrow(symbol, location, semanticModel, cancellationToken);
            using var recursive = RecursiveValues.Borrow(assignedValues, semanticModel, cancellationToken);
            return IsAnyCreation(recursive, semanticModel, cancellationToken);
        }

        /// <summary>
        /// Check if any path returns a created IDisposable.
        /// </summary>
        internal static Result IsCreation(ExpressionSyntax candidate, SemanticModel semanticModel, CancellationToken cancellationToken)
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
                    return Result.No;
            }

            if (!IsPotentiallyAssignableFrom(candidate, semanticModel, cancellationToken))
            {
                return Result.No;
            }

            if (candidate is IdentifierNameSyntax { Identifier: { ValueText: "value" } } &&
                candidate.FirstAncestor<AccessorDeclarationSyntax>() is { } accessor &&
                accessor.IsKind(SyntaxKind.SetAccessorDeclaration))
            {
                return Result.No;
            }

            if (candidate is ObjectCreationExpressionSyntax)
            {
                return Result.Yes;
            }

            using var recursive = RecursiveValues.Borrow(new[] { candidate }, semanticModel, cancellationToken);
            return IsAnyCreation(recursive, semanticModel, cancellationToken);
        }

        internal static Result IsAnyCreation(RecursiveValues values, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (values.IsEmpty)
            {
                return Result.No;
            }

            values.Reset();
            var result = Result.No;
            while (values.MoveNext())
            {
                switch (IsCreationCore(values.Current, semanticModel, cancellationToken))
                {
                    case Result.Unknown:
                        if (result == Result.No)
                        {
                            result = Result.Unknown;
                        }

                        break;
                    case Result.Yes:
                        return Result.Yes;
                    case Result.AssumeYes:
                        result = Result.AssumeYes;
                        break;
                    case Result.No:
                        break;
                    case Result.AssumeNo:
                        if (result == Result.No)
                        {
                            result = Result.AssumeNo;
                        }

                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(values), values, "Unhandled result type.");
                }
            }

            return result;
        }

        /// <summary>
        /// Check if any path returns a created IDisposable.
        /// </summary>
        private static Result IsCreationCore(ExpressionSyntax candidate, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (candidate.IsMissing)
            {
                return Result.Unknown;
            }

            if (candidate is LiteralExpressionSyntax ||
                candidate is DefaultExpressionSyntax ||
                candidate is TypeOfExpressionSyntax ||
                candidate is InstanceExpressionSyntax ||
                candidate is ElementAccessExpressionSyntax)
            {
                return Result.No;
            }

            if (candidate is ObjectCreationExpressionSyntax ||
                candidate is ArrayCreationExpressionSyntax ||
                candidate is ImplicitArrayCreationExpressionSyntax ||
                candidate is InitializerExpressionSyntax)
            {
                switch (semanticModel.GetType(candidate, cancellationToken))
                {
                    case { } type:
                        return IsAssignableFrom(type, semanticModel.Compilation) ? Result.Yes : Result.No;
                    case null:
                        return Result.Unknown;
                }
            }

            if (semanticModel.TryGetSymbol(candidate, cancellationToken, out var symbol))
            {
                return symbol switch
                {
                    IParameterSymbol _ => Result.No,
                    ILocalSymbol _ => Result.No,
                    IFieldSymbol _ => Result.No,
                    IPropertySymbol { ContainingType: { MetadataName: "PasswordBox" }, MetadataName: "SecurePassword" } => Result.Yes,
                    IPropertySymbol _ => Result.AssumeNo,
                    IMethodSymbol { MetadataName: "CreateConnection" } method => InferFromReturnType(method),
                    IMethodSymbol { MetadataName: nameof(ToString) } => Result.No,
                    IMethodSymbol { MetadataName: nameof(GetHashCode) } => Result.No,
                    IMethodSymbol { MetadataName: nameof(Equals) } => Result.No,
                    IMethodSymbol { MetadataName: nameof(ReferenceEquals) } => Result.No,
                    IMethodSymbol { ContainingType: { IsGenericType: true }, MetadataName: "GetEnumerator" } => Result.Yes,
                    IMethodSymbol { ContainingType: { IsGenericType: false }, MetadataName: "GetEnumerator" } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "Activator" }, MetadataName: "CreateInstance", IsGenericMethod: false } => InferFromUse(),
                    IMethodSymbol { ContainingType: { MetadataName: "Activator" }, MetadataName: "CreateInstance", IsGenericMethod: true } method => InferFromReturnType(method),
                    IMethodSymbol { ContainingType: { MetadataName: "ActivatorUtilities" }, MetadataName: "CreateInstance", IsGenericMethod: true } method => InferFromReturnType(method),
                    IMethodSymbol { ContainingType: { MetadataName: "ConstructorInfo" }, MetadataName: "Invoke" } => InferFromUse(),
                    IMethodSymbol { ContainingType: { MetadataName: "Enumerable" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "HttpResponseMessage" }, MetadataName: "EnsureSuccessStatusCode" } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "ResourceManager" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "Task" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "Task`1" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "ValueTask`1" } } => Result.No,
                    IMethodSymbol { ReturnType: { MetadataName: "Task" } } => Result.No,
                    IMethodSymbol { IsExtensionMethod: true, ReturnType: { MetadataName: "ILoggerFactory" } } => Result.No,
                    IMethodSymbol method => IsMethodCreating(method, semanticModel.Compilation),
                    _ => Result.Unknown,
                };

                Result InferFromUse()
                {
                    if (candidate.Parent is CastExpressionSyntax { Type: { } castType } &&
                        semanticModel.TryGetType(castType, cancellationToken, out var type))
                    {
                        if (IsAssignableFrom(type, semanticModel.Compilation))
                        {
                            return Result.Yes;
                        }

                        if (type.IsSealed)
                        {
                            return Result.No;
                        }
                    }

                    return Result.AssumeNo;
                }

                Result InferFromReturnType(IMethodSymbol method)
                {
                    if (IsAssignableFrom(method.ReturnType, semanticModel.Compilation))
                    {
                        return Result.Yes;
                    }

                    if (method.ReturnType.IsSealed)
                    {
                        return Result.No;
                    }

                    return Result.AssumeNo;
                }

                static Result IsMethodCreating(IMethodSymbol method, Compilation compilation)
                {
                    if (method.ReturnType is INamedTypeSymbol { IsGenericType: true } returnType &&
                        method.ReturnType == KnownSymbol.TaskOfT)
                    {
                        return returnType.TypeArguments.TrySingle(out var typeArg) &&
                               IsAssignableFrom(typeArg, compilation)
                            ? Result.AssumeYes
                            : Result.No;
                    }

                    if (method.ContainingType is { IsGenericType: true } &&
                        AnyMatch(method.ReturnType, method.ContainingType.TypeArguments))
                    {
                        return Result.AssumeNo;
                    }

                    if (method.IsGenericMethod &&
                        AnyMatch(method.ReturnType, method.TypeArguments))
                    {
                        return Result.AssumeNo;
                    }

                    if (!IsAssignableFrom(method.ReturnType, compilation))
                    {
                        return Result.AssumeNo;
                    }

                    if (method.TryGetThisParameter(out var thisParameter) &&
                        TypeSymbolComparer.Equal(thisParameter.Type, method.ReturnType))
                    {
                        return Result.AssumeNo;
                    }

                    return IsAssignableFrom(method.ReturnType, compilation)
                               ? Result.AssumeYes
                               : Result.No;

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

            return Result.Unknown;
        }
    }
}
