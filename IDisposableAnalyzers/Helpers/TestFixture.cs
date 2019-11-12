namespace IDisposableAnalyzers
{
    using System.Linq;
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class TestFixture
    {
        internal static bool IsAssignedAndDisposedInSetupAndTearDown(FieldOrProperty fieldOrProperty, TypeDeclarationSyntax scope, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (AssignmentExecutionWalker.SingleFor(fieldOrProperty.Symbol, scope, Scope.Member, semanticModel, cancellationToken, out var assignment) &&
                assignment.FirstAncestor<MethodDeclarationSyntax>() is MethodDeclarationSyntax methodDeclaration)
            {
                if (Attribute.TryFind(methodDeclaration, KnownSymbol.NUnitSetUpAttribute, semanticModel, cancellationToken, out _))
                {
                    if (fieldOrProperty.ContainingType.TryFindFirstMethodRecursive(x => x.GetAttributes().Any(a => a.AttributeClass == KnownSymbol.NUnitTearDownAttribute), out var tearDown))
                    {
                        return DisposableMember.IsDisposed(fieldOrProperty, tearDown, semanticModel, cancellationToken);
                    }
                }

                if (Attribute.TryFind(methodDeclaration, KnownSymbol.NUnitOneTimeSetUpAttribute, semanticModel, cancellationToken, out _))
                {
                    if (fieldOrProperty.ContainingType.TryFindFirstMethodRecursive(
                        x => x.GetAttributes().Any(a => a.AttributeClass == KnownSymbol.NUnitOneTimeTearDownAttribute),
                        out var tearDown))
                    {
                        return DisposableMember.IsDisposed(fieldOrProperty, tearDown, semanticModel, cancellationToken);
                    }
                }
            }

            return false;
        }

        internal static bool IsAssignedInSetUp(FieldOrProperty fieldOrProperty, TypeDeclarationSyntax scope, SemanticModel semanticModel, CancellationToken cancellationToken, out AssignmentExpressionSyntax assignment, out AttributeSyntax attribute)
        {
            if (AssignmentExecutionWalker.SingleFor(fieldOrProperty.Symbol, scope, Scope.Member, semanticModel, cancellationToken, out assignment) &&
                assignment.FirstAncestor<MethodDeclarationSyntax>() is { } methodDeclaration)
            {
                return Attribute.TryFind(methodDeclaration, KnownSymbol.NUnitSetUpAttribute, semanticModel, cancellationToken, out attribute) ||
                       Attribute.TryFind(methodDeclaration, KnownSymbol.NUnitOneTimeSetUpAttribute, semanticModel, cancellationToken, out attribute);
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
                    if (Attribute.TryFind(methodDeclaration, teardOwnAttributeType, semanticModel, cancellationToken, out _))
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
