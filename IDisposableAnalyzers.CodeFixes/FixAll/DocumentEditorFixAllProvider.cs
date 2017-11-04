namespace IDisposableAnalyzers
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Editing;

    internal class DocumentEditorFixAllProvider : FixAllProvider
    {
        public static readonly DocumentEditorFixAllProvider Default = new DocumentEditorFixAllProvider();

        private static readonly ImmutableArray<FixAllScope> SupportedFixAllScopes = ImmutableArray.Create(FixAllScope.Document);

        private DocumentEditorFixAllProvider()
        {
        }

        public override IEnumerable<FixAllScope> GetSupportedFixAllScopes()
        {
            return SupportedFixAllScopes;
        }

        public override async Task<CodeAction> GetFixAsync(FixAllContext fixAllContext)
        {
            var diagnostics = await fixAllContext.GetDocumentDiagnosticsAsync(fixAllContext.Document)
                                                 .ConfigureAwait(false);
            var documentEditorActions = new List<DocumentEditorAction>();
            foreach (var diagnostic in diagnostics)
            {
                var codeFixContext = new CodeFixContext(
                    fixAllContext.Document,
                    diagnostic,
                    (a, _) =>
                    {
                        if (a.EquivalenceKey == fixAllContext.CodeActionEquivalenceKey)
                        {
                            if (a is DocumentEditorAction docAction)
                            {
                                documentEditorActions.Add(docAction);
                            }
                            else
                            {
                                throw new InvalidOperationException("When using DocumentEditorFixAllProvider all registered code actions must be of type DocumentEditorAction");
                            }
                        }
                    },
                    fixAllContext.CancellationToken);
                await fixAllContext.CodeFixProvider.RegisterCodeFixesAsync(codeFixContext)
                                   .ConfigureAwait(false);
            }

            if (documentEditorActions.Count == 0)
            {
                return null;
            }

            return CodeAction.Create(documentEditorActions[0].Title, c => FixDocumentAsync(fixAllContext.Document, documentEditorActions, c));
        }

        private static async Task<Document> FixDocumentAsync(Document document, IReadOnlyList<DocumentEditorAction> documentEditorActions, CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken)
                                             .ConfigureAwait(false);
            foreach (var action in documentEditorActions)
            {
                action.Action(editor, cancellationToken);
            }

            return editor.GetChangedDocument();
        }
    }
}