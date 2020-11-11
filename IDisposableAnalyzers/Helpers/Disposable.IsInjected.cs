namespace IDisposableAnalyzers
{
    using System.Threading;

    using Gu.Roslyn.AnalyzerExtensions;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static partial class Disposable
    {
        internal static bool IsCachedOrInjectedOnly(ExpressionSyntax value, ExpressionSyntax location, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (semanticModel.TryGetSymbol(value, cancellationToken, out var symbol))
            {
                using var assignedValues = AssignedValueWalker.Borrow(symbol, location, semanticModel, cancellationToken);
                if (assignedValues.Count == 0)
                {
                    return value switch
                    {
                        IdentifierNameSyntax { Parent: MemberAccessExpressionSyntax { Expression: { } parent, Name: { } name } }
                            when value == name
                            => IsCachedOrInjectedOnly(parent, location, semanticModel, cancellationToken),
                        IdentifierNameSyntax { Parent: MemberBindingExpressionSyntax { Parent: MemberAccessExpressionSyntax { Parent: InvocationExpressionSyntax { Parent: ConditionalAccessExpressionSyntax { Expression: { } parent, Parent: ExpressionStatementSyntax _ } } } } }
                            => IsCachedOrInjectedOnly(parent, location, semanticModel, cancellationToken),
                        IdentifierNameSyntax { Parent: MemberBindingExpressionSyntax { Parent: ConditionalAccessExpressionSyntax { Parent: ConditionalAccessExpressionSyntax { Expression: { } parent, Parent: ExpressionStatementSyntax _ } } } }
                            => IsCachedOrInjectedOnly(parent, location, semanticModel, cancellationToken),
                        _ => IsInjectedCore(symbol),
                    };
                }

                using var recursive = RecursiveValues.Borrow(assignedValues, semanticModel, cancellationToken);
                return IsAnyCachedOrInjected(recursive, semanticModel, cancellationToken) &&
                       !IsAnyCreation(recursive, semanticModel, cancellationToken);
            }

            return false;
        }

        internal static bool IsCachedOrInjected(ExpressionSyntax value, ExpressionSyntax location, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (semanticModel.TryGetSymbol(value, cancellationToken, out var symbol))
            {
                if (IsInjectedCore(symbol))
                {
                    return true;
                }

                return IsAssignedWithInjected(symbol, location, semanticModel, cancellationToken);
            }

            return false;
        }

        internal static bool IsAnyCachedOrInjected(RecursiveValues values, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (values.IsEmpty)
            {
                return false;
            }

            values.Reset();
            while (values.MoveNext())
            {
                if (values.Current is ElementAccessExpressionSyntax elementAccess &&
                    semanticModel.TryGetSymbol(elementAccess.Expression, cancellationToken, out var symbol))
                {
                    if (IsInjectedCore(symbol))
                    {
                        return true;
                    }

                    using var assignedValues = AssignedValueWalker.Borrow(values.Current, semanticModel, cancellationToken);
                    using var recursive = RecursiveValues.Borrow(assignedValues, semanticModel, cancellationToken);
                    if (IsAnyCachedOrInjected(recursive, semanticModel, cancellationToken))
                    {
                        return true;
                    }
                }
                else if (semanticModel.TryGetSymbol(values.Current, cancellationToken, out symbol))
                {
                    if (IsInjectedCore(symbol))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        internal static bool IsAssignedWithInjected(ISymbol symbol, ExpressionSyntax location, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            using var assignedValues = AssignedValueWalker.Borrow(symbol, location, semanticModel, cancellationToken);
            using var recursive = RecursiveValues.Borrow(assignedValues, semanticModel, cancellationToken);
            return IsAnyCachedOrInjected(recursive, semanticModel, cancellationToken);
        }

        private static bool IsInjectedCore(ISymbol symbol)
        {
            if (symbol is ILocalSymbol)
            {
                return false;
            }

            if (symbol is IParameterSymbol)
            {
                return true;
            }

            if (symbol is IFieldSymbol field)
            {
                if (field.IsStatic ||
                    field.IsAbstract ||
                    field.IsVirtual)
                {
                    return true;
                }

                if (field.IsReadOnly)
                {
                    return false;
                }

                return field.DeclaredAccessibility != Accessibility.Private;
            }

            if (symbol is IPropertySymbol property)
            {
                if (property.IsStatic ||
                    property.IsVirtual ||
                    property.IsAbstract)
                {
                    return true;
                }

                if (property.IsReadOnly ||
                    property.SetMethod is null)
                {
                    return false;
                }

                return property.DeclaredAccessibility != Accessibility.Private &&
                       property.SetMethod.DeclaredAccessibility != Accessibility.Private;
            }

            return false;
        }
    }
}
