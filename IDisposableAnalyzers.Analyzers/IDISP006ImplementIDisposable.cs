namespace IDisposableAnalyzers
{
    using System.Collections.Immutable;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class IDISP006ImplementIDisposable : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "IDISP006";

        private static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Implement IDisposable.",
            messageFormat: "Implement IDisposable.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: AnalyzerConstants.EnabledByDefault,
            description: "The member is assigned with a created `IDisposable`s within the type. Implement IDisposable and dispose it.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(HandleField, SyntaxKind.FieldDeclaration);
            context.RegisterSyntaxNodeAction(HandleProperty, SyntaxKind.PropertyDeclaration);
        }

        private static void HandleField(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            var field = (IFieldSymbol)context.ContainingSymbol;
            if (field.IsStatic)
            {
                return;
            }

            if (Disposable.IsAssignedWithCreatedAndNotCachedOrInjected(field, context.SemanticModel, context.CancellationToken) &&
                !TestFixture.IsAssignedAndDisposedInSetupAndTearDown(field, context.Node.FirstAncestor<TypeDeclarationSyntax>(), context.SemanticModel, context.CancellationToken))
            {
                CheckThatIDisposalbeIsImplemented(context);
            }
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

            if (propertyDeclaration.TryGetSetAccessorDeclaration(out AccessorDeclarationSyntax setter) &&
                setter.Body != null)
            {
                // Handle the backing field
                return;
            }

            if (Disposable.IsAssignedWithCreatedAndNotCachedOrInjected(property, context.SemanticModel, context.CancellationToken))
            {
                CheckThatIDisposalbeIsImplemented(context);
            }
        }

        private static void CheckThatIDisposalbeIsImplemented(SyntaxNodeAnalysisContext context)
        {
            var containingType = context.ContainingSymbol.ContainingType;
            if (!Disposable.IsAssignableTo(containingType) ||
                !Disposable.TryGetDisposeMethod(containingType, Search.TopLevel, out IMethodSymbol _))
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptor, context.Node.GetLocation()));
            }
        }
    }
}