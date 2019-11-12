namespace IDisposableAnalyzers
{
    using System;
    using System.Threading;
    using Gu.Roslyn.CodeFixExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Editing;

    [Obsolete("Use DocumentEditorCodeFixProvider")]
    internal static class CodeFixContextExt
    {
        internal static void RegisterDocumentEditorFix(
            this CodeFixContext context,
            string title,
            Action<DocumentEditor, CancellationToken> action,
            Diagnostic diagnostic)
        {
            RegisterDocumentEditorFix(context, title, action, title, diagnostic);
        }

        internal static void RegisterDocumentEditorFix(
            this CodeFixContext context,
            string title,
            Action<DocumentEditor, CancellationToken> action,
            string equivalenceKey,
            Diagnostic diagnostic)
        {
            context.RegisterCodeFix(
                new DocumentEditorAction(title, context.Document, action, equivalenceKey),
                diagnostic);
        }
    }
}
