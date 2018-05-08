namespace IDisposableAnalyzers
{
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static partial class Disposable
    {
        /// <summary>
        /// Check if any path returns a created IDisposable
        /// </summary>
        internal static bool IsCachedOrInjected(ExpressionSyntax value, SyntaxNode location, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var symbol = semanticModel.GetSymbolSafe(value, cancellationToken);
            if (IsInjectedCore(symbol).IsEither(Result.Yes, Result.AssumeYes))
            {
                return true;
            }

            if (symbol is IPropertySymbol property &&
                !property.IsAutoProperty())
            {
                using (var returnValues = ReturnValueWalker.Borrow(value, ReturnValueSearch.TopLevel, semanticModel, cancellationToken))
                {
                    using (var recursive = RecursiveValues.Create(returnValues, semanticModel, cancellationToken))
                    {
                        return IsAnyCachedOrInjected(recursive, semanticModel, cancellationToken)
                            .IsEither(Result.Yes, Result.AssumeYes);
                    }
                }
            }

            return IsAssignedWithInjected(symbol, location, semanticModel, cancellationToken);
        }

        internal static Result IsAnyCachedOrInjected(RecursiveValues values, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (values.Count == 0)
            {
                return Result.No;
            }

            var result = Result.No;
            values.Reset();
            while (values.MoveNext())
            {
                if (values.Current is ElementAccessExpressionSyntax elementAccess)
                {
                    var symbol = semanticModel.GetSymbolSafe(elementAccess.Expression, cancellationToken);
                    var isInjected = IsInjectedCore(symbol);
                    if (isInjected == Result.Yes)
                    {
                        return Result.Yes;
                    }

                    if (isInjected == Result.AssumeYes)
                    {
                        result = Result.AssumeYes;
                    }

                    using (var assignedValues = AssignedValueWalker.Borrow(values.Current, semanticModel, cancellationToken))
                    {
                        using (var recursive = RecursiveValues.Create(assignedValues, semanticModel, cancellationToken))
                        {
                            isInjected = IsAnyCachedOrInjected(recursive, semanticModel, cancellationToken);
                            if (isInjected == Result.Yes)
                            {
                                return Result.Yes;
                            }

                            if (isInjected == Result.AssumeYes)
                            {
                                result = Result.AssumeYes;
                            }
                        }
                    }
                }
                else
                {
                    var symbol = semanticModel.GetSymbolSafe(values.Current, cancellationToken);
                    var isInjected = IsInjectedCore(symbol);
                    if (isInjected == Result.Yes)
                    {
                        return Result.Yes;
                    }

                    if (isInjected == Result.AssumeYes)
                    {
                        result = Result.AssumeYes;
                    }
                }
            }

            return result;
        }

        private static Result IsInjectedCore(ISymbol symbol)
        {
            if (symbol == null)
            {
                return Result.Unknown;
            }

            if (symbol is ILocalSymbol)
            {
                return Result.Unknown;
            }

            if (symbol is IParameterSymbol)
            {
                return Result.Yes;
            }

            if (symbol is IFieldSymbol field)
            {
                if (field.IsStatic ||
                    field.IsAbstract ||
                    field.IsVirtual)
                {
                    return Result.Yes;
                }

                if (field.IsReadOnly)
                {
                    return Result.No;
                }

                return field.DeclaredAccessibility != Accessibility.Private
                           ? Result.AssumeYes
                           : Result.No;
            }

            if (symbol is IPropertySymbol property)
            {
                if (property.IsStatic ||
                    property.IsVirtual ||
                    property.IsAbstract)
                {
                    return Result.Yes;
                }

                if (property.IsReadOnly ||
                    property.SetMethod == null)
                {
                    return Result.No;
                }

                return property.DeclaredAccessibility != Accessibility.Private &&
                       property.SetMethod.DeclaredAccessibility != Accessibility.Private
                           ? Result.AssumeYes
                           : Result.No;
            }

            return Result.No;
        }

        private static bool IsAssignedWithInjected(ISymbol symbol, SyntaxNode location, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            using (var assignedValues = AssignedValueWalker.Borrow(symbol, location, semanticModel, cancellationToken))
            {
                using (var recursive = RecursiveValues.Create(assignedValues, semanticModel, cancellationToken))
                {
                    return IsAnyCachedOrInjected(recursive, semanticModel, cancellationToken).IsEither(Result.Yes, Result.AssumeYes);
                }
            }
        }
    }
}
