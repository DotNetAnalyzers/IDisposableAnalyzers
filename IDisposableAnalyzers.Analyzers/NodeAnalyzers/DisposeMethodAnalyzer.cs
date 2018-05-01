namespace IDisposableAnalyzers
{
    using System.Collections.Immutable;
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
                    !method.ContainingType.IsAssignableTo(KnownSymbol.IDisposable, context.SemanticModel.Compilation))
                {
                    context.ReportDiagnostic(Diagnostic.Create(IDISP009IsIDisposable.Descriptor, context.Node.GetLocation()));
                }

                if (method.Parameters.Length == 1 &&
                    method.Parameters[0].Type == KnownSymbol.Boolean &&
                    method.IsOverride &&
                    method.OverriddenMethod is IMethodSymbol overridden)
                {
                    if (overridden.DeclaringSyntaxReferences.Length == 0 &&
                        !CallsBase(methodDeclaration, overridden, context))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(IDISP010CallBaseDispose.Descriptor, context.Node.GetLocation()));
                    }
                    else
                    {
                        using (var disposeWalker = Disposable.DisposeWalker.Borrow(overridden, context.SemanticModel, context.CancellationToken))
                        {
                            foreach (var disposeCall in disposeWalker)
                            {
                                if (Disposable.TryGetDisposedRootMember(disposeCall, context.SemanticModel, context.CancellationToken, out var disposed) &&
                                    context.SemanticModel.TryGetSymbol(disposed, context.CancellationToken, out ISymbol member) &&
                                    !Disposable.IsMemberDisposed(member, method, context.SemanticModel, context.CancellationToken))
                                {
                                    context.ReportDiagnostic(Diagnostic.Create(IDISP010CallBaseDispose.Descriptor, context.Node.GetLocation()));
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        private static bool CallsBase(MemberDeclarationSyntax method, IMethodSymbol overridden, SyntaxNodeAnalysisContext context)
        {
            using (var invocations = InvocationWalker.Borrow(method))
            {
                foreach (var invocation in invocations)
                {
                    if (invocation.TryGetMethodName(out var name) &&
                        name != overridden.Name)
                    {
                        continue;
                    }

                    if (context.SemanticModel.TryGetSymbol(invocation, context.CancellationToken, out var target) &&
                        SymbolComparer.Equals(target, overridden))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
