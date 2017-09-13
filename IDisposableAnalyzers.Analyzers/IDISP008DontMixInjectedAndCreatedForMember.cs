namespace IDisposableAnalyzers
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class IDISP008DontMixInjectedAndCreatedForMember : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "IDISP008";

        private static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Don't assign member with injected and created disposables.",
            messageFormat: "Don't assign member with injected and created disposables.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: AnalyzerConstants.EnabledByDefault,
            description: "Don't assign member with injected and created disposables. It creates a confusing ownership situation.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(HandleField, SyntaxKind.FieldDeclaration);
            context.RegisterSyntaxNodeAction(HandleProperty, SyntaxKind.PropertyDeclaration);
            context.RegisterSyntaxNodeAction(HandleAssignment, SyntaxKind.SimpleAssignmentExpression);
        }

        private static void HandleField(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            var field = (IFieldSymbol)context.ContainingSymbol;
            if (field.IsStatic || field.IsConst)
            {
                return;
            }

            if (field.DeclaredAccessibility != Accessibility.Private &&
                !field.IsReadOnly)
            {
                if (Disposable.IsAssignedWithCreated(field, context.SemanticModel, context.CancellationToken).IsEither(Result.Yes, Result.Maybe))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Descriptor, context.Node.GetLocation()));
                }
            }
            else if (Disposable.IsAssignedWithCreatedAndInjected(field, context.SemanticModel, context.CancellationToken))
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptor, context.Node.GetLocation()));
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

            if (property.SetMethod != null &&
                property.SetMethod.DeclaredAccessibility != Accessibility.Private)
            {
                if (Disposable.IsAssignedWithCreated(property, context.SemanticModel, context.CancellationToken)
                              .IsEither(Result.Yes, Result.Maybe))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Descriptor, context.Node.GetLocation()));
                }
            }
            else if (Disposable.IsAssignedWithCreatedAndInjected(property, context.SemanticModel, context.CancellationToken))
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptor, context.Node.GetLocation()));
            }
        }

        private static void HandleAssignment(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            var assignment = context.Node as AssignmentExpressionSyntax;
            if (assignment == null)
            {
                return;
            }

            var parameter = context.SemanticModel.GetSymbolSafe(assignment.Left, context.CancellationToken) as IParameterSymbol;
            if (parameter == null ||
                parameter.ContainingSymbol.DeclaredAccessibility == Accessibility.Private ||
                parameter.RefKind != RefKind.Ref)
            {
                return;
            }

            if (Disposable.IsAssignableTo(context.SemanticModel.GetTypeInfoSafe(assignment.Right, context.CancellationToken).Type))
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptor, context.Node.GetLocation()));
            }
        }
    }
}