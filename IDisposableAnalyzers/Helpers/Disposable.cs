namespace IDisposableAnalyzers;

using System.Threading;

using Gu.Roslyn.AnalyzerExtensions;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

internal static partial class Disposable
{
    internal static bool IsPotentiallyAssignableFrom(ExpressionSyntax candidate, SemanticModel semanticModel, CancellationToken cancellationToken) => candidate switch
    {
        { IsMissing: true } => false,
        LiteralExpressionSyntax _ => false,
        ObjectCreationExpressionSyntax objectCreation
            => semanticModel.TryGetType(objectCreation, cancellationToken, out var type) &&
               IsAssignableFrom(type, semanticModel.Compilation),
        _ => semanticModel.TryGetType(candidate, cancellationToken, out var type) &&
             IsPotentiallyAssignableFrom(type, semanticModel.Compilation),
    };

    internal static bool IsPotentiallyAssignableFrom(ITypeSymbol type, Compilation compilation)
    {
        if (type is IErrorTypeSymbol)
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

    internal static bool IsAssignableFrom(ITypeSymbol type, Compilation compilation) => type switch
    {
        null => false,
        //// https://blogs.msdn.microsoft.com/pfxteam/2012/03/25/do-i-need-to-dispose-of-tasks/
        { ContainingNamespace: { MetadataName: "Tasks", ContainingNamespace: { MetadataName: "Threading", ContainingNamespace.MetadataName: "System" } }, MetadataName: "Task" } => false,
        INamedTypeSymbol { ContainingNamespace: { MetadataName: "Tasks", ContainingNamespace: { MetadataName: "Threading", ContainingNamespace.MetadataName: "System" } }, MetadataName: "Task`1", TypeArguments: { Length: 1 } arguments }
            => IsAssignableFrom(arguments[0], compilation),
        { IsRefLikeType: true } => DisposeMethod.IsAccessibleOn(type, compilation),
        _ => type.IsAssignableTo(KnownSymbols.IDisposable, compilation),
    };

    internal static bool IsNop(ExpressionSyntax candidate, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        if (semanticModel.TryGetSymbol(candidate, cancellationToken, out var symbol) &&
            FieldOrProperty.TryCreate(symbol, out var fieldOrProperty) &&
            fieldOrProperty.IsStatic &&
            IsAssignableFrom(fieldOrProperty.Type, semanticModel.Compilation))
        {
            if (fieldOrProperty.Type == KnownSymbols.Task ||
                symbol == KnownSymbols.RxDisposable.Empty)
            {
                return true;
            }

            using (var walker = ReturnValueWalker.Borrow(candidate, ReturnValueSearch.Recursive, semanticModel, cancellationToken))
            {
                if (walker.Values.Count > 0)
                {
                    return walker.Values.TrySingle(out var value) &&
                           semanticModel.TryGetType(value, cancellationToken, out var type) &&
                           IsNopCore(type);
                }
            }

            using (var walker = AssignedValueWalker.Borrow(symbol, semanticModel, cancellationToken))
            {
                return walker.Values.TrySingle(out var value) &&
                       semanticModel.TryGetType(value, cancellationToken, out var type) &&
                       IsNopCore(type);
            }
        }

        return false;

        bool IsNopCore(ITypeSymbol type)
        {
            return type is { IsSealed: true, BaseType.SpecialType: SpecialType.System_Object } &&
                   type.TryFindSingleMethod("Dispose", out var disposeMethod) &&
                   disposeMethod.Parameters.Length == 0 &&
                   disposeMethod.TrySingleDeclaration(cancellationToken, out MethodDeclarationSyntax? declaration) &&
                   declaration is { Body.Statements.Count: 0, ExpressionBody: null };
        }
    }
}
