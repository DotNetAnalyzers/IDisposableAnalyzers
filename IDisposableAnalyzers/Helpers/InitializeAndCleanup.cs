namespace IDisposableAnalyzers
{
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class InitializeAndCleanup
    {
        internal static bool IsAssignedAndDisposed(FieldOrProperty fieldOrProperty, TypeDeclarationSyntax scope, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (AssignmentExecutionWalker.SingleFor(fieldOrProperty.Symbol, scope, SearchScope.Member, semanticModel, cancellationToken, out var assignment) &&
                assignment.FirstAncestor<MethodDeclarationSyntax>() is { } methodDeclaration)
            {
                var cleanup = FindCleanup(KnownSymbol.NUnitSetUpAttribute, KnownSymbol.NUnitTearDownAttribute) ??
                              FindCleanup(KnownSymbol.NUnitOneTimeSetUpAttribute, KnownSymbol.NUnitOneTimeTearDownAttribute) ??
                              FindCleanup(KnownSymbol.TestInitializeAttribute, KnownSymbol.TestCleanupAttribute) ??
                              FindCleanup(KnownSymbol.ClassInitializeAttribute, KnownSymbol.ClassCleanupAttribute) ??
                    FindStopAsync();
                return cleanup is { } &&
                       DisposableMember.IsDisposed(fieldOrProperty, cleanup, semanticModel, cancellationToken);
            }

            return false;

            IMethodSymbol? FindCleanup(QualifiedType initialize, QualifiedType cleanup)
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

            IMethodSymbol? FindStopAsync()
            {
                return fieldOrProperty.ContainingType.IsAssignableTo(KnownSymbol.IHostedService, semanticModel.Compilation) &&
                       methodDeclaration is { Identifier: { ValueText: "StartAsync" }, ParameterList: { Parameters: { Count: 1 } parameters } } &&
                       parameters[0].Type == KnownSymbol.CancellationToken &&
                       fieldOrProperty.ContainingType.TryFindFirstMethod("StopAsync", x => x == KnownSymbol.IHostedService.StopAsync, out var stopAsync)
                    ? stopAsync
                    : null;
            }
        }

        internal static bool IsAssignedInInitialize(FieldOrPropertyAndDeclaration fieldOrProperty, SemanticModel semanticModel, CancellationToken cancellationToken, [NotNullWhen(true)] out AssignmentExpressionSyntax? assignment, [NotNullWhen(true)] out AttributeSyntax? attribute)
        {
            return IsAssignedInInitialize(fieldOrProperty.FieldOrProperty, (TypeDeclarationSyntax)fieldOrProperty.Declaration.Parent, semanticModel, cancellationToken, out assignment, out attribute);
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

        internal static MethodDeclarationSyntax? FindCleanup(AttributeSyntax setupAttribute, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var typeDeclarationSyntax = setupAttribute.FirstAncestor<TypeDeclarationSyntax>();
            if (typeDeclarationSyntax is null)
            {
                return null;
            }

            if (TearDownAttribute(semanticModel.GetTypeInfoSafe(setupAttribute, cancellationToken).Type) is { } tearDownAttributeType)
            {
                foreach (var member in typeDeclarationSyntax.Members)
                {
                    if (member is MethodDeclarationSyntax methodDeclaration)
                    {
                        if (Attribute.TryFind(methodDeclaration, tearDownAttributeType, semanticModel, cancellationToken, out _))
                        {
                            return methodDeclaration;
                        }
                    }
                }
            }

            return null;

            static QualifiedType? TearDownAttribute(ITypeSymbol? initialize)
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
