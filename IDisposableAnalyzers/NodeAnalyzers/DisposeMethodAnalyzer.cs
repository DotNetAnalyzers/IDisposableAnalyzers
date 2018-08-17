namespace IDisposableAnalyzers
{
    using System.Collections.Immutable;
    using System.Linq;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class DisposeMethodAnalyzer : DiagnosticAnalyzer
    {
        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
            IDISP009IsIDisposable.Descriptor,
            IDISP010CallBaseDispose.Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(HandleDisposeMethod, SyntaxKind.MethodDeclaration);
        }

        private static void HandleDisposeMethod(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            if (context.ContainingSymbol is IMethodSymbol method &&
                context.Node is MethodDeclarationSyntax methodDeclaration &&
                method.Name == "Dispose" &&
                method.ReturnsVoid)
            {
                if (method.Parameters.Length == 0 &&
                    method.DeclaredAccessibility == Accessibility.Public &&
                    method.GetAttributes().Length == 0 &&
                    method.ReturnsVoid &&
                    method.OverriddenMethod == null &&
                    !method.ExplicitInterfaceImplementations.Any() &&
                    !IsInterfaceImplementation(method))
                {
                    context.ReportDiagnostic(Diagnostic.Create(IDISP009IsIDisposable.Descriptor, context.Node.GetLocation()));
                }

                if (method.Parameters.TrySingle(out var parameter) &&
                    parameter.Type == KnownSymbol.Boolean &&
                    method.IsOverride &&
                    method.OverriddenMethod is IMethodSymbol overridden)
                {
                    if (overridden.DeclaringSyntaxReferences.Length == 0 &&
                        !DisposeMethod.TryGetBaseCall(methodDeclaration, context.SemanticModel, context.CancellationToken, out _))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(IDISP010CallBaseDispose.Descriptor, context.Node.GetLocation(), parameter.Name));
                    }
                    else
                    {
                        using (var disposeWalker = DisposeWalker.Borrow(overridden, context.SemanticModel, context.CancellationToken))
                        {
                            foreach (var disposeCall in disposeWalker)
                            {
                                if (DisposeCall.TryGetDisposed(disposeCall, context.SemanticModel, context.CancellationToken, out var disposed) &&
                                    FieldOrProperty.TryCreate(disposed, out var fieldOrProperty) &&
                                    !DisposableMember.IsDisposed(fieldOrProperty, method, context.SemanticModel, context.CancellationToken))
                                {
                                    context.ReportDiagnostic(Diagnostic.Create(IDISP010CallBaseDispose.Descriptor, context.Node.GetLocation(), parameter.Name));
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        private static bool IsInterfaceImplementation(IMethodSymbol method)
        {
            if (method.ContainingType.TypeKind == TypeKind.Interface)
            {
                return true;
            }

            foreach (var @interface in method.ContainingType.AllInterfaces)
            {
                foreach (var member in @interface.GetMembers())
                {
                    if (member is IMethodSymbol candidate &&
                        candidate.Name == "Dispose" &&
                        candidate.ReturnsVoid &&
                        candidate.DeclaredAccessibility == Accessibility.Public &&
                        candidate.Parameters.Length == 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
