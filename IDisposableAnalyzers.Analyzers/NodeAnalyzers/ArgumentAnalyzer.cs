namespace IDisposableAnalyzers
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class ArgumentAnalyzer : DiagnosticAnalyzer
    {
        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(IDISP003DisposeBeforeReassigning.Descriptor);

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

            if (context.Node is ArgumentSyntax argument &&
                argument.Parent is ArgumentListSyntax argumentList &&
                argumentList.Parent is InvocationExpressionSyntax invocation &&
                argument.RefOrOutKeyword.IsEitherKind(SyntaxKind.RefKeyword, SyntaxKind.OutKeyword) &&
                context.SemanticModel.GetSymbolSafe(invocation, context.CancellationToken) is IMethodSymbol method &&
                method.TrySingleDeclaration(context.CancellationToken, out _))
            {
                if (Disposable.IsCreation(argument, context.SemanticModel, context.CancellationToken)
                              .IsEither(Result.No, Result.AssumeNo, Result.Unknown))
                {
                    return;
                }

                var symbol = context.SemanticModel.GetSymbolSafe(argument.Expression, context.CancellationToken);
                if (Disposable.IsAssignedWithCreated(symbol, invocation, context.SemanticModel, context.CancellationToken)
                              .IsEither(Result.No, Result.AssumeNo, Result.Unknown))
                {
                    return;
                }

                if (Disposable.IsDisposedBefore(symbol, argument.Expression, context.SemanticModel, context.CancellationToken))
                {
                    return;
                }

                context.ReportDiagnostic(Diagnostic.Create(IDISP003DisposeBeforeReassigning.Descriptor, argument.GetLocation()));
            }
        }
    }
}
