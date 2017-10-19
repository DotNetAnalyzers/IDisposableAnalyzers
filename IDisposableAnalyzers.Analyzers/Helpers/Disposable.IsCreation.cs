namespace IDisposableAnalyzers
{
    using System;
    using System.Diagnostics;
    using System.Threading;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static partial class Disposable
    {
        /// <summary>
        /// Check if any path returns a created IDisposable
        /// </summary>
        internal static Result IsAssignedWithCreated(ExpressionSyntax disposable, SemanticModel semanticModel, CancellationToken cancellationToken, out ISymbol assignedSymbol)
        {
            if (!IsPotentiallyAssignableTo(disposable, semanticModel, cancellationToken))
            {
                assignedSymbol = null;
                return Result.No;
            }

            if (semanticModel.GetSymbolSafe(disposable, cancellationToken) is IPropertySymbol property &&
                property.TryGetSetter(cancellationToken, out AccessorDeclarationSyntax setter))
            {
                using (var pooledSet = PooledHashSet<ISymbol>.Borrow())
                {
                    using (var pooledAssigned = AssignmentWalker.Borrow(setter, Search.Recursive, semanticModel, cancellationToken))
                    {
                        foreach (var assigned in pooledAssigned.Assignments)
                        {
                            var symbol = semanticModel.GetSymbolSafe(assigned.Left, cancellationToken);
                            if (IsPotentiallyAssignableTo(assigned.Left, semanticModel, cancellationToken) &&
                                (symbol is IFieldSymbol ||
                                symbol is IPropertySymbol))
                            {
                                pooledSet.Add(symbol).IgnoreReturnValue();
                            }
                        }
                    }

                    assignedSymbol = null;
                    var result = Result.No;
                    foreach (var symbol in pooledSet)
                    {
                        switch (IsAssignedWithCreated(symbol, disposable, semanticModel, cancellationToken))
                        {
                            case Result.Unknown:
                                if (result == Result.No)
                                {
                                    assignedSymbol = symbol;
                                    result = Result.Unknown;
                                }

                                break;
                            case Result.Yes:
                                assignedSymbol = symbol;
                                return Result.Yes;
                            case Result.No:
                                break;
                            case Result.Maybe:
                                assignedSymbol = symbol;
                                result = Result.Maybe;
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }

                    return result;
                }
            }

            using (var assignedValues = AssignedValueWalker.Borrow(disposable, semanticModel, cancellationToken))
            {
                using (var recursive = RecursiveValues.Create(assignedValues, semanticModel, cancellationToken))
                {
                    assignedSymbol = assignedValues.CurrentSymbol;
                    return IsAssignedWithCreated(recursive, semanticModel, cancellationToken);
                }
            }
        }

        internal static Result IsAssignedWithCreated(IFieldSymbol field, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (!IsPotentiallyAssignableTo(field?.Type))
            {
                return Result.No;
            }

            using (var assignedValues = AssignedValueWalker.Borrow(field, semanticModel, cancellationToken))
            {
                using (var recursive = RecursiveValues.Create(assignedValues, semanticModel, cancellationToken))
                {
                    return IsAssignedWithCreated(recursive, semanticModel, cancellationToken);
                }
            }
        }

        internal static Result IsAssignedWithCreated(IPropertySymbol property, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (!IsPotentiallyAssignableTo(property?.Type))
            {
                return Result.No;
            }

            using (var assignedValues = AssignedValueWalker.Borrow(property, semanticModel, cancellationToken))
            {
                using (var recursive = RecursiveValues.Create(assignedValues, semanticModel, cancellationToken))
                {
                    return IsAssignedWithCreated(recursive, semanticModel, cancellationToken);
                }
            }
        }

        internal static Result IsAssignedWithCreated(ISymbol symbol, SyntaxNode context, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            using (var assignedValues = AssignedValueWalker.Borrow(symbol, context, semanticModel, cancellationToken))
            {
                using (var recursive = RecursiveValues.Create(assignedValues, semanticModel, cancellationToken))
                {
                    return IsAssignedWithCreated(recursive, semanticModel, cancellationToken);
                }
            }
        }

        /// <summary>
        /// Check if any path returns a created IDisposable
        /// </summary>
        internal static Result IsCreation(ExpressionSyntax candidate, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (!IsPotentiallyAssignableTo(candidate, semanticModel, cancellationToken) ||
                candidate is ThisExpressionSyntax ||
                candidate is BaseExpressionSyntax)
            {
                return Result.No;
            }

            if (candidate is ObjectCreationExpressionSyntax)
            {
                return Result.Yes;
            }

            using (var walker = ReturnValueWalker.Borrow(candidate, Search.Recursive, semanticModel, cancellationToken))
            {
                if (walker.Count == 0)
                {
                    var symbol = semanticModel.GetSymbolSafe(candidate, cancellationToken);
                    if (symbol != null && symbol.DeclaringSyntaxReferences.Length == 0)
                    {
                        return IsCreationCore(candidate, semanticModel, cancellationToken);
                    }

                    using (var recursive = RecursiveValues.Create(new[] { candidate }, semanticModel, cancellationToken))
                    {
                        return IsCreationCore(recursive, semanticModel, cancellationToken);
                    }
                }

                using (var recursive = RecursiveValues.Create(walker, semanticModel, cancellationToken))
                {
                    return IsCreationCore(recursive, semanticModel, cancellationToken);
                }
            }
        }

        /// <summary>
        /// Check if any path returns a created IDisposable
        /// </summary>
        internal static Result IsCreation(ArgumentSyntax candidate, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (candidate == null)
            {
                return Result.No;
            }

            Debug.Assert(!candidate.RefOrOutKeyword.IsKind(SyntaxKind.None), "Only valid for ref or out parameter.");
            var invocation = candidate.FirstAncestor<InvocationExpressionSyntax>();
            if (invocation.TryGetMatchingParameter(candidate, semanticModel, cancellationToken, out IParameterSymbol parameter))
            {
                return IsAssignedWithCreated(parameter, null, semanticModel, cancellationToken);
            }

            return Result.Unknown;
        }

        private static Result IsAssignedWithCreated(RecursiveValues walker, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (walker.Count == 0)
            {
                return Result.No;
            }

            return IsCreationCore(walker, semanticModel, cancellationToken);
        }

        private static Result IsCreationCore(RecursiveValues values, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
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
                    case Result.No:
                        break;
                    case Result.Maybe:
                        result = Result.Maybe;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return result;
        }

        /// <summary>
        /// Check if any path returns a created IDisposable
        /// </summary>
        private static Result IsCreationCore(ExpressionSyntax candidate, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (candidate == null ||
                candidate.IsMissing)
            {
                return Result.Unknown;
            }

            if (!IsPotentiallyAssignableTo(semanticModel.GetTypeInfoSafe(candidate, cancellationToken).Type))
            {
                return Result.No;
            }

            if (candidate is LiteralExpressionSyntax ||
                candidate is DefaultExpressionSyntax ||
                candidate is TypeOfExpressionSyntax)
            {
                return Result.No;
            }

            if (candidate is ObjectCreationExpressionSyntax ||
                candidate is ArrayCreationExpressionSyntax ||
                candidate is ImplicitArrayCreationExpressionSyntax ||
                candidate is InitializerExpressionSyntax)
            {
                if (IsAssignableTo(semanticModel.GetTypeInfoSafe(candidate, cancellationToken).Type))
                {
                    return Result.Yes;
                }

                return Result.No;
            }

            var symbol = semanticModel.GetSymbolSafe(candidate, cancellationToken);
            if (symbol == null ||
                symbol is ILocalSymbol)
            {
                return Result.Unknown;
            }

            if (symbol is IFieldSymbol)
            {
                return Result.No;
            }

            if (symbol is IPropertySymbol property)
            {
                if (property.DeclaringSyntaxReferences.Length == 0)
                {
                    return property == KnownSymbol.PasswordBox.SecurePassword
                        ? Result.Yes
                        : Result.No;
                }

                return Result.Unknown;
            }

            if (symbol is IMethodSymbol method)
            {
                if (method.DeclaringSyntaxReferences.Length == 0)
                {
                    if (method == KnownSymbol.IEnumerableOfT.GetEnumerator)
                    {
                        return Result.Yes;
                    }

                    if (method.ContainingType.Is(KnownSymbol.IDictionary) ||
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
                        method == KnownSymbol.IEnumerable.GetEnumerator ||
                        method == KnownSymbol.Task.Run ||
                        method == KnownSymbol.Task.RunOfT ||
                        method == KnownSymbol.Task.ConfigureAwait ||
                        method == KnownSymbol.Task.FromResult)
                    {
                        return Result.No;
                    }

                    if (method.ReturnType == KnownSymbol.Task)
                    {
                        return Result.No;
                    }

                    if (method.ReturnType == KnownSymbol.TaskOfT)
                    {
                        return IsAssignableTo(((INamedTypeSymbol)method.ReturnType).TypeArguments[0])
                            ? Result.Maybe
                            : Result.No;
                    }

                    if (method.ContainingType == KnownSymbol.File &&
                        IsAssignableTo(method.ReturnType))
                    {
                        return Result.Yes;
                    }

                    return IsAssignableTo(method.ReturnType)
                               ? Result.Maybe
                               : Result.No;
                }

                return Result.Unknown;
            }

            return Result.Unknown;
        }
    }
}