namespace IDisposableAnalyzers
{
    using System.Collections.Immutable;
    using System.Composition;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Editing;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddBaseCallCodeFixProvider))]
    [Shared]
    internal class AddBaseCallCodeFixProvider : CodeFixProvider
    {
        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(IDISP010CallBaseDispose.DiagnosticId);

        /// <inheritdoc/>
        public override FixAllProvider GetFixAllProvider() => DocumentEditorFixAllProvider.Default;

        /// <inheritdoc/>
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                          .ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                var token = syntaxRoot.FindToken(diagnostic.Location.SourceSpan.Start);
                if (string.IsNullOrEmpty(token.ValueText) ||
                    token.IsMissing)
                {
                    continue;
                }

                var member = (MemberDeclarationSyntax)syntaxRoot.FindNode(diagnostic.Location.SourceSpan);
                if (member is MethodDeclarationSyntax disposeMethod)
                {
                    if (disposeMethod.ParameterList != null &&
                        disposeMethod.ParameterList.Parameters.TryGetSingle(out var parameter))
                    {
                        context.RegisterDocumentEditorFix(
                            $"Call base.Dispose({parameter.Identifier.ValueText})",
                            (editor, _) => AddBaseCall(editor, disposeMethod),
                            "Call base.Dispose()",
                            diagnostic);
                    }

                    continue;
                }
            }
        }

        private static void AddBaseCall(DocumentEditor editor, MethodDeclarationSyntax disposeMethod)
        {
            if (disposeMethod.ParameterList.Parameters.TryGetSingle(out var parameter))
            {
                var baseCall = SyntaxFactory.ParseStatement($"base.{disposeMethod.Identifier.ValueText}({parameter.Identifier.ValueText});")
                                            .WithLeadingTrivia(SyntaxFactory.ElasticMarker)
                                            .WithTrailingTrivia(SyntaxFactory.ElasticMarker);
                editor.SetStatements(disposeMethod, disposeMethod.Body.Statements.Add(baseCall));
            }
        }
    }
}