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

            if (AssignmentExecutionWalker.SingleForSymbol(fieldOrProperty, scope, Search.TopLevel, semanticModel, cancellationToken, out var assignment) &&
                assignment.FirstAncestor<MethodDeclarationSyntax>() is MethodDeclarationSyntax methodDeclaration)
            {
                if (Attribute.TryGetAttribute(methodDeclaration, KnownSymbol.NUnitSetUpAttribute, semanticModel, cancellationToken, out _))
                {
                    if (fieldOrProperty.ContainingType.TryFirstMethodRecursive(
                        x => x.GetAttributes().Any(a => a.AttributeClass == KnownSymbol.NUnitTearDownAttribute),
                        out var tearDown))
                    {
                        return Disposable.IsMemberDisposed(fieldOrProperty, tearDown, semanticModel, cancellationToken);
                    }
                }

                if (Attribute.TryGetAttribute(methodDeclaration, KnownSymbol.NUnitOneTimeSetUpAttribute, semanticModel, cancellationToken, out _))
                {
                    if (fieldOrProperty.ContainingType.TryFirstMethodRecursive(
                        x => x.GetAttributes().Any(a => a.AttributeClass == KnownSymbol.NUnitOneTimeTearDownAttribute),
                        out var tearDown))
                    {
                        return Disposable.IsMemberDisposed(fieldOrProperty, tearDown, semanticModel, cancellationToken);
                    }
                }
            }

            return false;
        }

        internal static bool IsAssignedInSetUp(ISymbol fieldOrProperty, TypeDeclarationSyntax scope, SemanticModel semanticModel, CancellationToken cancellationToken, out AttributeSyntax attribute)
        {
            if (AssignmentExecutionWalker.SingleForSymbol(fieldOrProperty, scope, Search.TopLevel, semanticModel, cancellationToken, out var assignment) &&
                assignment.FirstAncestor<MethodDeclarationSyntax>() is MethodDeclarationSyntax methodDeclaration)
            {
                return Attribute.TryGetAttribute(methodDeclaration, KnownSymbol.NUnitSetUpAttribute, semanticModel, cancellationToken, out attribute) ||
                       Attribute.TryGetAttribute(methodDeclaration, KnownSymbol.NUnitOneTimeSetUpAttribute, semanticModel, cancellationToken, out attribute);
            }

            attribute = null;
            return false;
        }

        internal static bool TryGetTearDownMethod(AttributeSyntax setupAttribute, SemanticModel semanticModel, CancellationToken cancellationToken, out MethodDeclarationSyntax result)
        {
            result = null;
            var typeDeclarationSyntax = setupAttribute.FirstAncestor<TypeDeclarationSyntax>();
            if (typeDeclarationSyntax == null)
            {
                return false;
            }

            var attributeType = semanticModel.GetTypeInfoSafe(setupAttribute, cancellationToken).Type;

            var teardOwnAttributeType = attributeType == KnownSymbol.NUnitSetUpAttribute
                ? KnownSymbol.NUnitTearDownAttribute
                : KnownSymbol.NUnitOneTimeTearDownAttribute;
            foreach (var member in typeDeclarationSyntax.Members)
            {
                if (member is MethodDeclarationSyntax methodDeclaration)
                {
                    if (Attribute.TryGetAttribute(methodDeclaration, teardOwnAttributeType, semanticModel, cancellationToken, out _))
                    {
                        result = methodDeclaration;
                        return true;
                    }
                }
            }

            return false;
        }
    }
}