namespace IDisposableAnalyzers
{
    using System.Collections.Immutable;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class IDISP002DisposeMember : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "IDISP002";

        private static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Dispose member.",
            messageFormat: "Dispose member.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: AnalyzerConstants.EnabledByDefault,
            description: "Dispose the member as it is assigned with a created `IDisposable`.",
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
            context.RegisterSyntaxNodeAction(HandleDisposeMethod, SyntaxKind.MethodDeclaration);
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

            if (Disposable.IsAssignedWithCreatedAndNotCachedOrInjected(field, context.SemanticModel, context.CancellationToken))
            {
                if (Disposable.IsMemberDisposed(field, context.Node.FirstAncestorOrSelf<TypeDeclarationSyntax>(), context.SemanticModel, context.CancellationToken)
                              .IsEither(Result.No, Result.Unknown))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Descriptor, context.Node.GetLocation()));
                }
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
                if (Disposable.IsMemberDisposed(property, context.Node.FirstAncestorOrSelf<TypeDeclarationSyntax>(), context.SemanticModel, context.CancellationToken)
                              .IsEither(Result.No, Result.Unknown))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Descriptor, context.Node.GetLocation()));
                }
            }
        }

        private static void HandleDisposeMethod(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            if (context.ContainingSymbol is IMethodSymbol method &&
                method.IsOverride &&
                method.Name == "Dispose")
            {
                var overridden = method.OverriddenMethod;
                if (overridden == null)
                {
                    return;
                }

                using (var invocations = InvocationWalker.Borrow(context.Node))
                {
                    foreach (var invocation in invocations)
                    {
                        if (
                            SymbolComparer.Equals(
                                context.SemanticModel.GetSymbolSafe(invocation, context.CancellationToken), overridden))
                        {
                            return;
                        }
                    }
                }

                if (overridden.DeclaringSyntaxReferences.Length == 0)
                {
                    context.ReportDiagnostic(Diagnostic.Create(Descriptor, context.Node.GetLocation()));
                    return;
                }

                using (
                    var disposeWalker = Disposable.DisposeWalker.Borrow(overridden, context.SemanticModel, context.CancellationToken))
                {
                    foreach (var disposeCall in disposeWalker)
                    {
                        if (Disposable.TryGetDisposedRootMember(disposeCall, context.SemanticModel, context.CancellationToken, out ExpressionSyntax disposed))
                        {
                            var member = context.SemanticModel.GetSymbolSafe(disposed, context.CancellationToken);
                            if (
                                !Disposable.IsMemberDisposed(member, method, context.SemanticModel, context.CancellationToken))
                            {
                                context.ReportDiagnostic(Diagnostic.Create(Descriptor, context.Node.GetLocation()));
                                return;
                            }
                        }
                    }
                }
            }
        }
    }
}