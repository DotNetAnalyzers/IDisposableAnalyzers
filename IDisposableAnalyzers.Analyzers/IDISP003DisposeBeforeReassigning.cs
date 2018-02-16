namespace IDisposableAnalyzers
{
    using System.Collections.Immutable;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class IDISP003DisposeBeforeReassigning : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "IDISP003";

        internal static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Dispose previous before re-assigning.",
            messageFormat: "Dispose previous before re-assigning.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: AnalyzerConstants.EnabledByDefault,
            description: "Dispose previous before re-assigning.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(HandleArgument, SyntaxKind.Argument);
        }

        private static void HandleArgument(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            var argument = (ArgumentSyntax)context.Node;
            if (argument.RefOrOutKeyword.IsKind(SyntaxKind.None))
            {
                return;
            }

            var invocation = argument.FirstAncestor<InvocationExpressionSyntax>();
            if (invocation == null)
            {
                return;
            }

            var method = context.SemanticModel.GetSymbolSafe(invocation, context.CancellationToken);
            if (method == null ||
                method.DeclaringSyntaxReferences.Length == 0)
            {
                return;
            }

            if (Disposable.IsCreation(argument, context.SemanticModel, context.CancellationToken)
                          .IsEither(Result.No, Result.AssumeNo, Result.Unknown))
            {
                return;
            }

            var symbol = context.SemanticModel.GetSymbolSafe(argument.Expression, context.CancellationToken);
            if (Disposable.IsAssignedWithCreated(symbol, argument.FirstAncestor<InvocationExpressionSyntax>(), context.SemanticModel, context.CancellationToken)
                          .IsEither(Result.No, Result.AssumeNo, Result.Unknown))
            {
                return;
            }

            if (Disposable.IsDisposedBefore(symbol, argument.Expression, context.SemanticModel, context.CancellationToken))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(Descriptor, argument.GetLocation()));
        }
    }
}
