namespace IDisposableAnalyzers
{
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class TestFixture
    {
        internal static bool IsAssignedInInitializeAndDisposedInCleanup(FieldOrProperty fieldOrProperty, TypeDeclarationSyntax scope, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (AssignmentExecutionWalker.SingleFor(fieldOrProperty.Symbol, scope, SearchScope.Member, semanticModel, cancellationToken, out var assignment) &&
                assignment.FirstAncestor<MethodDeclarationSyntax>() is { } methodDeclaration)
            {
                var cleanup = TearDown(KnownSymbol.NUnitSetUpAttribute, KnownSymbol.NUnitTearDownAttribute) ??
                              TearDown(KnownSymbol.NUnitOneTimeSetUpAttribute, KnownSymbol.NUnitOneTimeTearDownAttribute) ??
                              TearDown(KnownSymbol.TestInitializeAttribute,  KnownSymbol.TestCleanupAttribute) ??
                              TearDown(KnownSymbol.ClassInitializeAttribute, KnownSymbol.ClassCleanupAttribute);
                return cleanup is { } &&
                       DisposableMember.IsDisposed(fieldOrProperty, cleanup, semanticModel, cancellationToken);
            }

            return false;

            IMethodSymbol? TearDown(QualifiedType initialize, QualifiedType cleanup)
            {
                if (Attribute.TryFind(methodDeclaration, initialize, semanticModel, cancellationToken, out _))
                {
                    if (fieldOrProperty.ContainingType.TryFindFirstMethodRecursive(x => x.GetAttributes().Any(a => a.AttributeClass == cleanup), out var tearDown))
                    {
                        return tearDown;
                    }
                }

                return null;
            }
        }

        internal static bool IsAssignedInInitialize(FieldOrProperty fieldOrProperty, TypeDeclarationSyntax scope, SemanticModel semanticModel, CancellationToken cancellationToken, [NotNullWhen(true)] out AssignmentExpressionSyntax? assignment, [NotNullWhen(true)] out AttributeSyntax? attribute)
        {
            if (AssignmentExecutionWalker.SingleFor(fieldOrProperty.Symbol, scope, SearchScope.Member, semanticModel, cancellationToken, out assignment) &&
                assignment.FirstAncestor<MethodDeclarationSyntax>() is { } methodDeclaration)
            {
                return Attribute.TryFind(methodDeclaration, KnownSymbol.NUnitSetUpAttribute, semanticModel, cancellationToken, out attribute) ||
                       Attribute.TryFind(methodDeclaration, KnownSymbol.NUnitOneTimeSetUpAttribute, semanticModel, cancellationToken, out attribute) ||
                       Attribute.TryFind(methodDeclaration, KnownSymbol.TestInitializeAttribute, semanticModel, cancellationToken, out attribute) ||
                       Attribute.TryFind(methodDeclaration, KnownSymbol.ClassInitializeAttribute, semanticModel, cancellationToken, out attribute);
            }

            attribute = null;
            return false;
        }

        internal static bool TryGetTearDownMethod(AttributeSyntax setupAttribute, SemanticModel semanticModel, CancellationToken cancellationToken, [NotNullWhen(true)] out MethodDeclarationSyntax? result)
        {
            result = null;
            var typeDeclarationSyntax = setupAttribute.FirstAncestor<TypeDeclarationSyntax>();
            if (typeDeclarationSyntax == null)
            {
                return false;
            }

            if (TearDown(semanticModel.GetTypeInfoSafe(setupAttribute, cancellationToken).Type) is { } tearDownAttributeType)
            {
                foreach (var member in typeDeclarationSyntax.Members)
                {
                    if (member is MethodDeclarationSyntax methodDeclaration)
                    {
                        if (Attribute.TryFind(methodDeclaration, tearDownAttributeType, semanticModel, cancellationToken, out _))
                        {
                            result = methodDeclaration;
                            return true;
                        }
                    }
                }
            }

            return false;

            QualifiedType? TearDown(ITypeSymbol? initialize)
            {
                if (initialize is null)
                {
                    return null;
                }

                if (initialize == KnownSymbol.NUnitSetUpAttribute)
                {
                    return KnownSymbol.NUnitTearDownAttribute;
                }

                if (initialize == KnownSymbol.NUnitOneTimeSetUpAttribute)
                {
                    return KnownSymbol.NUnitOneTimeTearDownAttribute;
                }

                if (initialize == KnownSymbol.TestInitializeAttribute)
                {
                    return KnownSymbol.TestCleanupAttribute;
                }

                if (initialize == KnownSymbol.ClassInitializeAttribute)
                {
                    return KnownSymbol.ClassCleanupAttribute;
                }

                return null;
            }
        }
    }
}
