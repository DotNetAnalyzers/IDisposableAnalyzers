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
                return IsStartAsync(methodDeclaration, semanticModel, cancellationToken) &&
                       fieldOrProperty.ContainingType.TryFindFirstMethod("StopAsync", x => x == KnownSymbol.IHostedService.StopAsync, out var stopAsync)
                    ? stopAsync
                    : null;
            }
        }

        internal static bool IsAssignedInInitialize(FieldOrPropertyAndDeclaration fieldOrProperty, SemanticModel semanticModel, CancellationToken cancellationToken, [NotNullWhen(true)] out AssignmentExpressionSyntax? assignment, [NotNullWhen(true)] out MethodDeclarationSyntax? initialize)
        {
            return IsAssignedInInitialize(fieldOrProperty.FieldOrProperty, (TypeDeclarationSyntax)fieldOrProperty.Declaration.Parent, semanticModel, cancellationToken, out assignment, out initialize);
        }

        internal static bool IsAssignedInInitialize(FieldOrProperty fieldOrProperty, TypeDeclarationSyntax scope, SemanticModel semanticModel, CancellationToken cancellationToken, [NotNullWhen(true)] out AssignmentExpressionSyntax? assignment, [NotNullWhen(true)] out MethodDeclarationSyntax? initialize)
        {
            if (AssignmentExecutionWalker.SingleFor(fieldOrProperty.Symbol, scope, SearchScope.Member, semanticModel, cancellationToken, out assignment) &&
                assignment.FirstAncestor<MethodDeclarationSyntax>() is { } methodDeclaration)
            {
                initialize = methodDeclaration;
                return Attribute.TryFind(methodDeclaration, KnownSymbol.NUnitSetUpAttribute, semanticModel, cancellationToken, out _) ||
                       Attribute.TryFind(methodDeclaration, KnownSymbol.NUnitOneTimeSetUpAttribute, semanticModel, cancellationToken, out _) ||
                       Attribute.TryFind(methodDeclaration, KnownSymbol.TestInitializeAttribute, semanticModel, cancellationToken, out _) ||
                       Attribute.TryFind(methodDeclaration, KnownSymbol.ClassInitializeAttribute, semanticModel, cancellationToken, out _) ||
                       IsStartAsync(methodDeclaration, semanticModel, cancellationToken);
            }

            initialize = null;
            return false;
        }

        internal static MethodDeclarationSyntax? FindCleanup(MethodDeclarationSyntax initialize, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (initialize.FirstAncestor<TypeDeclarationSyntax>() is { } typeDeclarationSyntax)
            {
                foreach (var attributeList in initialize.AttributeLists)
                {
                    foreach (var attribute in attributeList.Attributes)
                    {
                        if (TearDownAttribute(semanticModel.GetTypeInfoSafe(attribute, cancellationToken).Type) is { } tearDownAttributeType)
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
                    }
                }

                if (IsStartAsync(initialize, semanticModel, cancellationToken))
                {
                    foreach (var member in typeDeclarationSyntax.Members)
                    {
                        if (member is MethodDeclarationSyntax methodDeclaration &&
                            methodDeclaration is { Identifier: { ValueText: "StopAsync" }, ParameterList: { Parameters: { Count: 1 } parameters } } &&
                            parameters[0].Type == KnownSymbol.CancellationToken)
                        {
                            return methodDeclaration;
                        }
                    }
                }
            }

            return null;

            static QualifiedType? TearDownAttribute(ITypeSymbol? initializeAttribute)
            {
                if (initializeAttribute is null)
                {
                    return null;
                }

                if (initializeAttribute == KnownSymbol.NUnitSetUpAttribute)
                {
                    return KnownSymbol.NUnitTearDownAttribute;
                }

                if (initializeAttribute == KnownSymbol.NUnitOneTimeSetUpAttribute)
                {
                    return KnownSymbol.NUnitOneTimeTearDownAttribute;
                }

                if (initializeAttribute == KnownSymbol.TestInitializeAttribute)
                {
                    return KnownSymbol.TestCleanupAttribute;
                }

                if (initializeAttribute == KnownSymbol.ClassInitializeAttribute)
                {
                    return KnownSymbol.ClassCleanupAttribute;
                }

                return null;
            }
        }

        private static bool IsStartAsync(MethodDeclarationSyntax methodDeclaration, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            return methodDeclaration is { Identifier: { ValueText: "StartAsync" }, ParameterList: { Parameters: { Count: 1 } parameters } } &&
                   parameters[0].Type == KnownSymbol.CancellationToken &&
                   semanticModel.TryGetSymbol(methodDeclaration, cancellationToken, out var method) &&
                   method == KnownSymbol.IHostedService.StartAsync;
        }
    }
}
