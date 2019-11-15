namespace IDisposableAnalyzers
{
    using System;
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static partial class Disposable
    {
        internal static Result IsAlreadyAssignedWithCreated(ExpressionSyntax disposable, SemanticModel semanticModel, CancellationToken cancellationToken, out ISymbol assignedSymbol)
        {
            if (!IsPotentiallyAssignableFrom(disposable, semanticModel, cancellationToken))
            {
                assignedSymbol = null;
                return Result.No;
            }

            var symbol = semanticModel.GetSymbolSafe(disposable, cancellationToken);
            if (symbol is IPropertySymbol { SetMethod: { } } property &&
                IsAssignableFrom(property.Type, semanticModel.Compilation) &&
                property.TryGetSetter(cancellationToken, out var setter) &&
                (setter.ExpressionBody != null || setter.Body != null))
            {
                using (var assignedSymbols = PooledSet<ISymbol>.Borrow())
                {
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
            }

            if (symbol is IParameterSymbol &&
                disposable.TryFirstAncestor<ArrowExpressionClauseSyntax>(out _))
            {
                assignedSymbol = null;
                return Result.No;
            }

            using (var assignedValues = AssignedValueWalker.Borrow(disposable, semanticModel, cancellationToken))
            {
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

                using (var recursive = RecursiveValues.Borrow(assignedValues, semanticModel, cancellationToken))
                {
                    return IsAnyCreation(recursive, semanticModel, cancellationToken);
                }
            }

            bool IsReturnedBefore(ExpressionSyntax expression)
            {
                if (expression.TryFirstAncestor(out BlockSyntax block) &&
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
            if (symbol?.Kind == SymbolKind.Discard)
            {
                return Result.No;
            }

            using (var assignedValues = AssignedValueWalker.Borrow(symbol, location, semanticModel, cancellationToken))
            {
                using (var recursive = RecursiveValues.Borrow(assignedValues, semanticModel, cancellationToken))
                {
                    return IsAnyCreation(recursive, semanticModel, cancellationToken);
                }
            }
        }

        /// <summary>
        /// Check if any path returns a created IDisposable.
        /// </summary>
        internal static Result IsCreation(ExpressionSyntax candidate, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (candidate == null)
            {
                return Result.Unknown;
            }

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

            using (var recursive = RecursiveValues.Borrow(new[] { candidate }, semanticModel, cancellationToken))
            {
                return IsAnyCreation(recursive, semanticModel, cancellationToken);
            }
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
            if (candidate == null ||
                candidate.IsMissing)
            {
                return Result.Unknown;
            }

            if (!IsPotentiallyAssignableFrom(semanticModel.GetTypeInfoSafe(candidate, cancellationToken).Type, semanticModel.Compilation))
            {
                return Result.No;
            }

            if (candidate is LiteralExpressionSyntax ||
                candidate is DefaultExpressionSyntax ||
                candidate is TypeOfExpressionSyntax ||
                candidate is ElementAccessExpressionSyntax)
            {
                return Result.No;
            }

            if (candidate is ObjectCreationExpressionSyntax ||
                candidate is ArrayCreationExpressionSyntax ||
                candidate is ImplicitArrayCreationExpressionSyntax ||
                candidate is InitializerExpressionSyntax)
            {
                if (semanticModel.TryGetType(candidate, cancellationToken, out var type) &&
                    IsAssignableFrom(type, semanticModel.Compilation))
                {
                    return Result.Yes;
                }

                return Result.No;
            }

            if (semanticModel.TryGetSymbol(candidate, cancellationToken, out var symbol))
            {
                switch (symbol)
                {
                    case IPropertySymbol _:
                    case IMethodSymbol _:
                        return IsCreationCore(symbol, semanticModel.Compilation);
                }
            }

            return Result.No;
        }

        private static Result IsCreationCore(ISymbol candidate, Compilation compilation)
        {
            if (candidate == null)
            {
                return Result.Unknown;
            }

            if (candidate is IFieldSymbol ||
                candidate is IParameterSymbol ||
                candidate is ILocalSymbol)
            {
                return Result.No;
            }

            if (candidate is IPropertySymbol property)
            {
                if (property.DeclaringSyntaxReferences.Length == 0)
                {
                    return property == KnownSymbol.PasswordBox.SecurePassword
                        ? Result.Yes
                        : Result.No;
                }

                return Result.Unknown;
            }

            if (candidate is IMethodSymbol method)
            {
                if (method.DeclaringSyntaxReferences.Length == 0)
                {
                    if (!IsAssignableFrom(method.ReturnType, compilation))
                    {
                        return Result.No;
                    }

                    if (method == KnownSymbol.IEnumerableOfT.GetEnumerator ||
                        method.ContainingType == KnownSymbol.File ||
                        method.ContainingType == KnownSymbol.FileInfo ||
                        method.ContainingType == KnownSymbol.RegistryKey)
                    {
                        return Result.Yes;
                    }

                    if (method.ContainingType.IsAssignableTo(KnownSymbol.IDictionary, compilation) ||
                        method.ContainingType == KnownSymbol.Enumerable ||
                        method.ContainingType == KnownSymbol.ListOfT ||
                        method.ContainingType == KnownSymbol.StackOfT ||
                        method.ContainingType == KnownSymbol.QueueOfT ||
                        method.ContainingType == KnownSymbol.LinkedListOfT ||
                        method.ContainingType == KnownSymbol.SortedSetOfT ||

                        method.ContainingType == KnownSymbol.DictionaryOfTKeyTValue ||
                        method.ContainingType == KnownSymbol.SortedListOfTKeyTValue ||
                        method.ContainingType == KnownSymbol.SortedDictionaryOfTKeyTValue ||

                        method.ContainingType == KnownSymbol.ImmutableHashSetOfT ||
                        method.ContainingType == KnownSymbol.ImmutableListOfT ||
                        method.ContainingType == KnownSymbol.ImmutableQueueOfT ||
                        method.ContainingType == KnownSymbol.ImmutableSortedSetOfT ||
                        method.ContainingType == KnownSymbol.ImmutableStackOfT ||

                        method.ContainingType == KnownSymbol.ImmutableDictionaryOfTKeyTValue ||
                        method.ContainingType == KnownSymbol.ImmutableSortedDictionaryOfTKeyTValue ||

                        method.ContainingType == KnownSymbol.ConditionalWeakTable ||
                        method.ContainingType == KnownSymbol.ResourceManager ||
                        method == KnownSymbol.IEnumerable.GetEnumerator ||
                        method == KnownSymbol.Task.Run ||
                        method == KnownSymbol.Task.RunOfT ||
                        method == KnownSymbol.Task.ConfigureAwait ||
                        method == KnownSymbol.Task.FromResult ||
                        method == KnownSymbol.HttpResponseMessage.EnsureSuccessStatusCode)
                    {
                        return Result.No;
                    }

                    if (method.ReturnType == KnownSymbol.Task)
                    {
                        return Result.No;
                    }

                    if (method.ReturnType == KnownSymbol.TaskOfT)
                    {
                        return method.TypeArguments.TrySingle(out var typeArg) &&
                               IsAssignableFrom(typeArg, compilation)
                            ? Result.AssumeYes
                            : Result.No;
                    }

                    if (method.IsGenericMethod &&
                        ReferenceEquals(method.TypeArguments[0], method.ReturnType))
                    {
                        return Result.AssumeNo;
                    }

                    if (method.TryGetThisParameter(out var thisParameter) &&
                        thisParameter.Type.Equals(method.ReturnType))
                    {
                        if (method.ReturnType == KnownSymbol.ILoggerFactory)
                        {
                            return Result.No;
                        }

                        return Result.AssumeNo;
                    }

                    return IsAssignableFrom(method.ReturnType, compilation)
                               ? Result.AssumeYes
                               : Result.No;
                }

                return Result.Unknown;
            }

            return Result.Unknown;
        }
    }
}
