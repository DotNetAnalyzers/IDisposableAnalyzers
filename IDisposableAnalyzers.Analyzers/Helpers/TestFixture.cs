namespace IDisposableAnalyzers
{
    using System.Linq;
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class TestFixture
    {
        internal static bool IsAssignedAndDisposedInSetupAndTearDown(ISymbol fieldOrProperty, TypeDeclarationSyntax scope, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (fieldOrProperty == null)
            {
                return false;
            }

            if (AssignmentWalker.SingleForSymbol(fieldOrProperty, scope, Search.TopLevel, semanticModel, cancellationToken, out var assignment) &&
                assignment.FirstAncestor<MethodDeclarationSyntax>() is MethodDeclarationSyntax methodDeclaration)
            {
                if (Attribute.TryGetAttribute(methodDeclaration, KnownSymbol.NUnitSetUpAttribute, semanticModel, cancellationToken, out _))
                {
                    if (fieldOrProperty.ContainingType.TryGetFirstMethod(
                        x => x.GetAttributes().Any(a => a.AttributeClass == KnownSymbol.NUnitTearDownAttribute),
                        out var tearDown))
                    {
                        return Disposable.IsMemberDisposed(fieldOrProperty, tearDown, semanticModel, cancellationToken);
                    }
                }

                if (Attribute.TryGetAttribute(methodDeclaration, KnownSymbol.NUnitOneTimeSetUpAttribute, semanticModel, cancellationToken, out _))
                {
                    if (fieldOrProperty.ContainingType.TryGetFirstMethod(
                        x => x.GetAttributes().Any(a => a.AttributeClass == KnownSymbol.NUnitOneTimeTearDownAttribute),
                        out var tearDown))
                    {
                        return Disposable.IsMemberDisposed(fieldOrProperty, tearDown, semanticModel, cancellationToken);
                    }
                }
            }

            return false;
        }
    }
}