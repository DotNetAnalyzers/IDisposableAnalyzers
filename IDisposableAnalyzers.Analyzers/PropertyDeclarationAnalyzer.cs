namespace IDisposableAnalyzers
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class PropertyDeclarationAnalyzer : DiagnosticAnalyzer
    {
        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
            IDISP002DisposeMember.Descriptor,
            IDISP006ImplementIDisposable.Descriptor,
            IDISP008DontMixInjectedAndCreatedForMember.Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(HandleProperty, SyntaxKind.PropertyDeclaration);
        }

        private static void HandleProperty(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            var property = (IPropertySymbol)context.ContainingSymbol;
            if (property.IsStatic ||
                property.IsIndexer)
            {
                return;
            }

            var propertyDeclaration = (PropertyDeclarationSyntax)context.Node;
            if (propertyDeclaration.ExpressionBody != null)
            {
                return;
            }

            if (propertyDeclaration.TryGetSetAccessorDeclaration(out var setter) &&
                setter.Body != null)
            {
                // Handle the backing field
                return;
            }

            if (Disposable.IsPotentiallyAssignableTo(property.Type))
            {
                using (var assignedValues = AssignedValueWalker.Borrow(property, context.SemanticModel, context.CancellationToken))
                {
                    using (var recursive = RecursiveValues.Create(assignedValues, context.SemanticModel, context.CancellationToken))
                    {
                        if (Disposable.IsAnyCreation(recursive, context.SemanticModel, context.CancellationToken).IsEither(Result.Yes, Result.AssumeYes))
                        {
                            if (property.DeclaredAccessibility != Accessibility.Private &&
                                !property.IsReadOnly &&
                                !property.ContainingType.IsSealed)
                            {
                                context.ReportDiagnostic(Diagnostic.Create(IDISP008DontMixInjectedAndCreatedForMember.Descriptor, context.Node.GetLocation()));
                            }
                            else if (Disposable.IsAnyCachedOrInjected(recursive, context.SemanticModel, context.CancellationToken) == Result.Yes)
                            {
                                context.ReportDiagnostic(Diagnostic.Create(IDISP008DontMixInjectedAndCreatedForMember.Descriptor, context.Node.GetLocation()));
                            }
                            else if (Disposable.IsMemberDisposed(property, context.Node.FirstAncestorOrSelf<TypeDeclarationSyntax>(), context.SemanticModel, context.CancellationToken)
                                               .IsEither(Result.No, Result.AssumeNo, Result.Unknown) &&
                                     !TestFixture.IsAssignedAndDisposedInSetupAndTearDown(property, context.Node.FirstAncestor<TypeDeclarationSyntax>(), context.SemanticModel, context.CancellationToken))
                            {
                                if (Disposable.IsAssignableTo(property.ContainingType) &&
                                    Disposable.TryGetDisposeMethod(property.ContainingType, Search.TopLevel, out IMethodSymbol _))
                                {
                                    context.ReportDiagnostic(
                                        Diagnostic.Create(
                                            IDISP002DisposeMember.Descriptor,
                                            context.Node.GetLocation()));
                                }
                                else
                                {
                                    if (TestFixture.IsAssignedInSetUp(property, context.Node.FirstAncestorOrSelf<TypeDeclarationSyntax>(), context.SemanticModel, context.CancellationToken, out _))
                                    {
                                        context.ReportDiagnostic(
                                            Diagnostic.Create(
                                                IDISP002DisposeMember.Descriptor,
                                                context.Node.GetLocation()));
                                    }

                                    context.ReportDiagnostic(
                                        Diagnostic.Create(
                                            IDISP006ImplementIDisposable.Descriptor,
                                            context.Node.GetLocation()));
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
