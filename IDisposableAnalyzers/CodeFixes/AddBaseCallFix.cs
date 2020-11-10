namespace IDisposableAnalyzers
{
    using System.Collections.Immutable;
    using System.Composition;
    using System.Threading.Tasks;
    using Gu.Roslyn.AnalyzerExtensions;
    using Gu.Roslyn.CodeFixExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddBaseCallFix))]
    [Shared]
    internal class AddBaseCallFix : DocumentEditorCodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(Descriptors.IDISP010CallBaseDispose.Id);

        protected override DocumentEditorFixAllProvider? FixAllProvider() => null;

        protected override async Task RegisterCodeFixesAsync(DocumentEditorCodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                                   .ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                if (syntaxRoot?.FindNode(diagnostic.Location.SourceSpan) is MethodDeclarationSyntax { Body: { } body } disposeMethod)
                {
                    if (disposeMethod is { ParameterList: { Parameters: { Count: 1 } parameters } } &&
                        parameters.TrySingle(out var parameter))
                    {
                        context.RegisterCodeFix(
                            $"base.Dispose({parameter.Identifier.ValueText})",
                            (e, _) => e.ReplaceNode(
                                body,
                                x => x.AddStatements(
                                    SyntaxFactory.ExpressionStatement(
                                        SyntaxFactory.InvocationExpression(
                                            SyntaxFactory.MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                SyntaxFactory.BaseExpression(),
                                                SyntaxFactory.IdentifierName(disposeMethod.Identifier)),
                                            SyntaxFactory.ArgumentList(
                                                SyntaxFactory.SingletonSeparatedList(
                                                    SyntaxFactory.Argument(SyntaxFactory.IdentifierName(parameter.Identifier)))))))),
                            "base.Dispose()",
                            diagnostic);
                    }
                    else if (disposeMethod is { ParameterList: { Parameters: { Count: 0 } } })
                    {
                        context.RegisterCodeFix(
                            "base.Dispose()",
                            (e, _) => e.ReplaceNode(
                                body,
                                x => x.AddStatements(
                                    SyntaxFactory.ExpressionStatement(
                                        SyntaxFactory.InvocationExpression(
                                            SyntaxFactory.MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                SyntaxFactory.BaseExpression(),
                                                SyntaxFactory.IdentifierName(disposeMethod.Identifier)))))),
                            "base.Dispose()",
                            diagnostic);
                    }
                }
            }
        }
    }
}
