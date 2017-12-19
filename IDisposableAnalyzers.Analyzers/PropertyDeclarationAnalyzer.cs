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
            IDISP002DisposeMember.Descriptor);

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

            if (Disposable.IsAssignedWithCreatedAndNotCachedOrInjected(property, context.SemanticModel, context.CancellationToken))
            {
                if (Disposable.IsMemberDisposed(property, context.Node.FirstAncestorOrSelf<TypeDeclarationSyntax>(), context.SemanticModel, context.CancellationToken)
                              .IsEither(Result.No, Result.AssumeNo, Result.Unknown))
                {
                    if (TestFixture.IsAssignedAndDisposedInSetupAndTearDown(property, context.Node.FirstAncestor<TypeDeclarationSyntax>(), context.SemanticModel, context.CancellationToken))
                    {
                        return;
                    }

                    context.ReportDiagnostic(Diagnostic.Create(IDISP002DisposeMember.Descriptor, context.Node.GetLocation()));
                }
            }
        }
    }
}
