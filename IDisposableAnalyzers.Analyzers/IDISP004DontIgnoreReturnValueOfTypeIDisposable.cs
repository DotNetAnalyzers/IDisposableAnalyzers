namespace IDisposableAnalyzers
{
    using System.Collections.Immutable;
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class IDISP004DontIgnoreReturnValueOfTypeIDisposable : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "IDISP004";

        private static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Don't ignore return value of type IDisposable.",
            messageFormat: "Don't ignore return value of type IDisposable.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: AnalyzerConstants.EnabledByDefault,
            description: "Don't ignore return value of type IDisposable.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(Handle, SyntaxKind.ObjectCreationExpression, SyntaxKind.InvocationExpression, SyntaxKind.SimpleMemberAccessExpression);
        }

        private static void Handle(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            if (context.Node is ObjectCreationExpressionSyntax objectCreation &&
                MustBeHandled(objectCreation, context.SemanticModel, context.CancellationToken))
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptor, context.Node.GetLocation()));
            }

            if (context.Node is InvocationExpressionSyntax invocation &&
                context.SemanticModel.GetSymbolSafe(invocation, context.CancellationToken) is IMethodSymbol method &&
                !method.ReturnsVoid &&
                Disposable.IsPotentiallyAssignableTo(method.ReturnType) &&
                MustBeHandled(invocation, context.SemanticModel, context.CancellationToken))
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptor, context.Node.GetLocation()));
            }

            if (context.Node is MemberAccessExpressionSyntax memberAccess &&
                context.SemanticModel.GetSymbolSafe(memberAccess.Expression, context.CancellationToken) is IPropertySymbol property &&
                Disposable.IsPotentiallyAssignableTo(property.Type) &&
                MustBeHandled(memberAccess.Expression, context.SemanticModel, context.CancellationToken))
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptor, context.Node.GetLocation()));
            }
        }

        private static bool MustBeHandled(ExpressionSyntax node, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (node.Parent is AnonymousFunctionExpressionSyntax ||
                node.Parent is UsingStatementSyntax ||
                node.Parent is EqualsValueClauseSyntax ||
                node.Parent is ReturnStatementSyntax ||
                node.Parent is ArrowExpressionClauseSyntax)
            {
                return false;
            }

            if (Disposable.IsCreation(node, semanticModel, cancellationToken)
                          .IsEither(Result.No, Result.AssumeNo, Result.Unknown))
            {
                return false;
            }

            if (node.Parent is StatementSyntax)
            {
                return true;
            }

            if (node.Parent is ArgumentSyntax argument)
            {
                return Disposable.IsArgumentDisposedByReturnValue(argument, semanticModel, cancellationToken)
                                 .IsEither(Result.No, Result.AssumeNo);
            }

            if (node.Parent is MemberAccessExpressionSyntax memberAccess)
            {
                return Disposable.IsArgumentDisposedByInvocationReturnValue(memberAccess, semanticModel, cancellationToken)
                                 .IsEither(Result.No, Result.AssumeNo);
            }

            return false;
        }
    }
}
