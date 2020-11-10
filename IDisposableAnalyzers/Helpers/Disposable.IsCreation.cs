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
                    IPropertySymbol _ => Result.No,
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
                    IMethodSymbol { ContainingType: { MetadataName: "ArraySegment`1" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "BindingList`1" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "BlockingCollection`1" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "ChangeAwareList`1" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "Collection`1" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "ConcurrentBag`1" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "ConcurrentDictionary`2" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "ConcurrentQueue`1" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "ConcurrentStack`1" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "ConditionalWeakTable`2" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "ConstructorInfo" }, MetadataName: "Invoke" } => InferFromUse(),
                    IMethodSymbol { ContainingType: { MetadataName: "DbSet`1" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "Dictionary`2" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "EditableBindingList`1" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "EditableList`1" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "EntityCollection`1" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "Enumerable" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "EnumerableQuery`1" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "File" }, MetadataName: "OpenText" } => Result.Yes,
                    IMethodSymbol { ContainingType: { MetadataName: "File" }, MetadataName: "CreateText" } => Result.Yes,
                    IMethodSymbol { ContainingType: { MetadataName: "File" }, MetadataName: "AppendText" } => Result.Yes,
                    IMethodSymbol { ContainingType: { MetadataName: "File" }, MetadataName: "Copy" } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "File" }, MetadataName: "Create" } => Result.Yes,
                    IMethodSymbol { ContainingType: { MetadataName: "File" }, MetadataName: "Delete" } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "File" }, MetadataName: "Exists" } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "File" }, MetadataName: "Open" } => Result.Yes,
                    IMethodSymbol { ContainingType: { MetadataName: "File" }, MetadataName: "SetCreationTime" } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "File" }, MetadataName: "SetCreationTimeUtc" } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "File" }, MetadataName: "GetCreationTime" } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "File" }, MetadataName: "GetCreationTimeUtc" } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "File" }, MetadataName: "SetLastAccessTime" } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "File" }, MetadataName: "SetLastAccessTimeUtc" } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "File" }, MetadataName: "GetLastAccessTime" } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "File" }, MetadataName: "GetLastAccessTimeUtc" } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "File" }, MetadataName: "SetLastWriteTime" } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "File" }, MetadataName: "SetLastWriteTimeUtc" } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "File" }, MetadataName: "GetLastWriteTime" } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "File" }, MetadataName: "GetLastWriteTimeUtc" } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "File" }, MetadataName: "GetAttributes" } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "File" }, MetadataName: "SetAttributes" } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "File" }, MetadataName: "GetAccessControl" } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "File" }, MetadataName: "SetAccessControl" } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "File" }, MetadataName: "OpenRead" } => Result.Yes,
                    IMethodSymbol { ContainingType: { MetadataName: "File" }, MetadataName: "OpenWrite" } => Result.Yes,
                    IMethodSymbol { ContainingType: { MetadataName: "File" }, MetadataName: "ReadAllText" } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "File" }, MetadataName: "WriteAllText" } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "File" }, MetadataName: "ReadAllBytes" } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "File" }, MetadataName: "WriteAllBytes" } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "File" }, MetadataName: "ReadAllLines" } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "File" }, MetadataName: "ReadLines" } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "File" }, MetadataName: "WriteAllLines" } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "File" }, MetadataName: "AppendAllText" } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "File" }, MetadataName: "AppendAllLines" } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "File" }, MetadataName: "Move" } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "File" }, MetadataName: "Replace" } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "File" }, MetadataName: "Decrypt" } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "File" }, MetadataName: "Encrypt" } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "FileInfo" }, MetadataName: "Delete" } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "FileInfo" }, MetadataName: "GetAccessControl" } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "FileInfo" }, MetadataName: "SetAccessControl" } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "FileInfo" }, MetadataName: "OpenText" } => Result.Yes,
                    IMethodSymbol { ContainingType: { MetadataName: "FileInfo" }, MetadataName: "CreateText" } => Result.Yes,
                    IMethodSymbol { ContainingType: { MetadataName: "FileInfo" }, MetadataName: "AppendText" } => Result.Yes,
                    IMethodSymbol { ContainingType: { MetadataName: "FileInfo" }, MetadataName: "CopyTo" } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "FileInfo" }, MetadataName: "Create" } => Result.Yes,
                    IMethodSymbol { ContainingType: { MetadataName: "FileInfo" }, MetadataName: "Decrypt" } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "FileInfo" }, MetadataName: "Encrypt" } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "FileInfo" }, MetadataName: "Open" } => Result.Yes,
                    IMethodSymbol { ContainingType: { MetadataName: "FileInfo" }, MetadataName: "OpenRead" } => Result.Yes,
                    IMethodSymbol { ContainingType: { MetadataName: "FileInfo" }, MetadataName: "OpenWrite" } => Result.Yes,
                    IMethodSymbol { ContainingType: { MetadataName: "FileInfo" }, MetadataName: "MoveTo" } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "FileInfo" }, MetadataName: "Replace" } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "FreezableCollection`1" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "GenericDictionaryAdapter`1" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "HashSet`1" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "HttpHeaderValueCollection`1" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "HttpResponseMessage" }, MetadataName: "EnsureSuccessStatusCode" } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "IAggregateChangeSet`1" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "IBindingList`1" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "IChangeSet`1" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "IChangeSet`2" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "ICollection`1" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "IDbSet`1" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "IDictionary`2" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "IDistinctChangeSet`1" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "IEnumerable`1" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "IExtendedList`1" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "IGroupChangeSet`3" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "IGrouping`2" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "IImmutableDictionary`2" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "IImmutableGroupChangeSet`3" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "IImmutableList`1" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "IImmutableQueue`1" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "IImmutableSet`1" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "IImmutableStack`1" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "IKeyValueCollection`2" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "IList`1" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "ILookup`2" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "ImmutableArray`1" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "ImmutableDictionary`2" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "ImmutableHashSet`1" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "ImmutableList`1" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "ImmutableQueue`1" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "ImmutableSortedDictionary`2" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "ImmutableSortedSet`1" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "ImmutableStack`1" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "IObservableCollection`1" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "IOrderedEnumerable`1" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "IOrderedQueryable`1" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "IProducerConsumerCollection`1" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "IQueryable`1" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "IReadOnlyCollection`1" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "IReadOnlyDictionary`2" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "IReadOnlyList`1" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "ISet`1" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "KeyedCollection`2" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "LinkedList`1" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "List`1" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "Lookup`2" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "ObservableCollection`1" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "ObservableCollectionExtended`1" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "Queue`1" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "ReadOnlyCollection`1" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "RegistryKey" }, MetadataName: "Dispose" } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "RegistryKey" }, MetadataName: "Close" } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "RegistryKey" }, MetadataName: "Flush" } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "RegistryKey" }, MetadataName: "CreateSubKey" } => Result.Yes,
                    IMethodSymbol { ContainingType: { MetadataName: "RegistryKey" }, MetadataName: "DeleteSubKey" } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "RegistryKey" }, MetadataName: "DeleteSubKeyTree" } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "RegistryKey" }, MetadataName: "DeleteValue" } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "RegistryKey" }, MetadataName: "OpenSubKey" } => Result.Yes,
                    IMethodSymbol { ContainingType: { MetadataName: "RegistryKey" }, MetadataName: "GetSubKeyNames" } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "RegistryKey" }, MetadataName: "GetValueNames" } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "RegistryKey" }, MetadataName: "GetValue" } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "RegistryKey" }, MetadataName: "GetValueKind" } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "RegistryKey" }, MetadataName: "SetValue" } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "RegistryKey" }, MetadataName: "GetAccessControl" } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "RegistryKey" }, MetadataName: "SetAccessControl" } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "ResourceManager" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "ReadOnlyCollectionBuilder`1" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "ReadOnlyDictionary`2" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "ReadOnlyMetadataCollection`1" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "ReadOnlyObservableCollection`1" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "SetProjection`1" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "SortedDictionary`2" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "SortedList`2" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "SortedSet`1" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "Stack`1" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "Task" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "Task`1" } } => Result.No,
                    IMethodSymbol { ContainingType: { MetadataName: "ValueTask`1" } } => Result.No,
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
                    if (method.ReturnType == KnownSymbol.Task)
                    {
                        return Result.No;
                    }

                    if (method.ReturnType is INamedTypeSymbol { IsGenericType: true } returnType &&
                        method.ReturnType == KnownSymbol.TaskOfT)
                    {
                        return returnType.TypeArguments.TrySingle(out var typeArg) &&
                               IsAssignableFrom(typeArg, compilation)
                            ? Result.AssumeYes
                            : Result.No;
                    }

                    if (!IsAssignableFrom(method.ReturnType, compilation))
                    {
                        return Result.No;
                    }

                    if (method.IsGenericMethod &&
                        TypeSymbolComparer.Equal(method.TypeArguments[0], method.ReturnType))
                    {
                        return Result.AssumeNo;
                    }

                    if (method.TryGetThisParameter(out var thisParameter) &&
                        TypeSymbolComparer.Equal(thisParameter.Type, method.ReturnType))
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
            }

            return Result.Unknown;
        }
    }
}
