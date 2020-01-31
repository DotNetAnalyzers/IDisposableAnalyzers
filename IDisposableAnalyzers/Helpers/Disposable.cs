namespace IDisposableAnalyzers
{
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static partial class Disposable
    {
        internal static bool IsPotentiallyAssignableFrom(ExpressionSyntax candidate, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            switch (candidate)
            {
                case { IsMissing: true }:
                case LiteralExpressionSyntax _:
                    return false;
                case ObjectCreationExpressionSyntax objectCreation:
                    return semanticModel.TryGetType(objectCreation, cancellationToken, out var type) &&
                           IsAssignableFrom(type, semanticModel.Compilation);
                default:
                    return semanticModel.TryGetType(candidate, cancellationToken, out type) &&
                           IsPotentiallyAssignableFrom(type, semanticModel.Compilation);
            }
        }

        internal static bool IsPotentiallyAssignableFrom(ITypeSymbol type, Compilation compilation)
        {
            if (type is null ||
                type is IErrorTypeSymbol)
            {
                return false;
            }

            if (type.IsValueType &&
                !IsAssignableFrom(type, compilation))
            {
                return false;
            }

            if (type.IsSealed &&
                !IsAssignableFrom(type, compilation))
            {
                return false;
            }

            return true;
        }

        internal static bool IsAssignableFrom(ITypeSymbol type, Compilation compilation)
        {
            if (type is null)
            {
                return false;
            }

            // https://blogs.msdn.microsoft.com/pfxteam/2012/03/25/do-i-need-to-dispose-of-tasks/
            if (type == KnownSymbol.Task)
            {
                return false;
            }

            return type == KnownSymbol.IDisposable ||
                   type.IsAssignableTo(KnownSymbol.IDisposable, compilation);
        }

        internal static bool IsNop(ExpressionSyntax candidate, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (semanticModel.TryGetSymbol(candidate, cancellationToken, out var symbol) &&
                FieldOrProperty.TryCreate(symbol, out var fieldOrProperty) &&
                fieldOrProperty.IsStatic &&
                IsAssignableFrom(fieldOrProperty.Type, semanticModel.Compilation))
            {
                if (fieldOrProperty.Type == KnownSymbol.Task ||
                    symbol == KnownSymbol.RxDisposable.Empty)
                {
                    return true;
                }

                using (var walker = ReturnValueWalker.Borrow(candidate, ReturnValueSearch.Recursive, semanticModel, cancellationToken))
                {
                    if (walker.Count > 0)
                    {
                        return walker.TrySingle(out var value) &&
                               semanticModel.TryGetType(value, cancellationToken, out var type) &&
                               IsNop(type);
                    }
                }

                using (var walker = AssignedValueWalker.Borrow(symbol, semanticModel, cancellationToken))
                {
                    return walker.TrySingle(out var value) &&
                           semanticModel.TryGetType(value, cancellationToken, out var type) &&
                           IsNop(type);
                }
            }

            return false;

            bool IsNop(ITypeSymbol type)
            {
                return type is { IsSealed: true, BaseType: { SpecialType: SpecialType.System_Object } } &&
                       type.TryFindSingleMethod("Dispose", out var disposeMethod) &&
                       disposeMethod.Parameters.Length == 0 &&
                       disposeMethod.TrySingleDeclaration(cancellationToken, out MethodDeclarationSyntax? declaration) &&
                       declaration is { Body: { Statements: { Count: 0 } }, ExpressionBody: null };
            }
        }
    }
}
