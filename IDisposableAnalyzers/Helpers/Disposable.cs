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
        internal static bool IsPotentiallyAssignableFrom(ExpressionSyntax candidate, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (candidate == null ||
                candidate.IsMissing ||
                candidate is LiteralExpressionSyntax)
            {
                return false;
            }

            if (candidate is ObjectCreationExpressionSyntax objectCreation)
            {
                return semanticModel.TryGetType(objectCreation, cancellationToken, out var type) &&
                       IsAssignableFrom(type, semanticModel.Compilation);
            }
            else
            {
                return semanticModel.TryGetType(candidate, cancellationToken, out var type) &&
                       IsPotentiallyAssignableFrom(type, semanticModel.Compilation);
            }
        }

        internal static bool IsPotentiallyAssignableFrom(ITypeSymbol type, Compilation compilation)
        {
            if (type == null ||
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
                   type.IsAssignableTo(KnownSymbol.IDisposable, compilation);
        }

        internal static bool IsNop(ExpressionSyntax candidate, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (semanticModel.TryGetSymbol(candidate, cancellationToken, out ISymbol symbol) &&
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
                return type.IsSealed &&
                       type.BaseType == KnownSymbol.Object &&
                       type.TryFindSingleMethod("Dispose", out var disposeMethod) &&
                       disposeMethod.Parameters.Length == 0 &&
                       disposeMethod.TrySingleDeclaration(cancellationToken, out MethodDeclarationSyntax declaration) &&
                       declaration.Body is BlockSyntax body &&
                       body.Statements.Count == 0;
            }
        }

        internal static bool ShouldDispose(LocalOrParameter localOrParameter, ExpressionSyntax location, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            switch (localOrParameter.Symbol)
            {
                case ILocalSymbol local:
                    return ShouldDispose(local, location, semanticModel, cancellationToken);
                case IParameterSymbol parameter:
                    return ShouldDispose(parameter, location, semanticModel, cancellationToken);
            }

            throw new InvalidOperationException("Should never get here.");
        }

        internal static bool ShouldDispose(ILocalSymbol local, ExpressionSyntax location, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (location is AssignmentExpressionSyntax assignment &&
                assignment.Left is IdentifierNameSyntax identifierName &&
                identifierName.Identifier.ValueText == local.Name &&
                assignment.Parent is UsingStatementSyntax)
            {
                return false;
            }

            if (local.TrySingleDeclaration(cancellationToken, out var declaration))
            {
                if (declaration.Parent is UsingStatementSyntax ||
                    declaration.Parent is AnonymousFunctionExpressionSyntax)
                {
                    return false;
                }

                if (LocalOrParameter.TryCreate(local, out var localOrParameter))
                {
                    return DisposableWalker.ShouldDispose(localOrParameter, semanticModel, cancellationToken);
                }
            }

            return false;
        }

        internal static bool ShouldDispose(IParameterSymbol parameter, ExpressionSyntax location, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (location is AssignmentExpressionSyntax assignment &&
                assignment.Left is IdentifierNameSyntax identifierName &&
                identifierName.Identifier.ValueText == parameter.Name &&
                assignment.Parent is UsingStatementSyntax)
            {
                return false;
            }

            if (parameter.RefKind == RefKind.None &&
                parameter.TrySingleDeclaration(cancellationToken, out var declaration) &&
                declaration.Parent is ParameterListSyntax parameterList &&
                parameterList.Parent is BaseMethodDeclarationSyntax methodDeclaration &&
                methodDeclaration.Body.IsKind(SyntaxKind.Block) &&
                LocalOrParameter.TryCreate(parameter, out var localOrParameter))
            {
                return DisposableWalker.ShouldDispose(localOrParameter, semanticModel, cancellationToken);
            }

            return false;
        }
    }
}
