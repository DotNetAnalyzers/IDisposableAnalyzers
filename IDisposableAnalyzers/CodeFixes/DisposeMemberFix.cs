namespace IDisposableAnalyzers
{
    using System.Collections.Immutable;
    using System.Composition;
    using System.Threading;
    using System.Threading.Tasks;
    using Gu.Roslyn.AnalyzerExtensions;
    using Gu.Roslyn.CodeFixExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Editing;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DisposeMemberFix))]
    [Shared]
    internal class DisposeMemberFix : DocumentEditorCodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(IDISP002DisposeMember.DiagnosticId);

        protected override async Task RegisterCodeFixesAsync(DocumentEditorCodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                          .ConfigureAwait(false);

            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken)
                                             .ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                if (syntaxRoot.TryFindNode<MemberDeclarationSyntax>(diagnostic, out var member) &&
                    semanticModel.TryGetSymbol(member, context.CancellationToken, out ISymbol memberSymbol))
                {
                    if (DisposeMethod.TryFindVirtualDispose(memberSymbol.ContainingType, semanticModel.Compilation, Search.TopLevel, out var disposeMethod) &&
                        disposeMethod.TrySingleDeclaration(context.CancellationToken, out MethodDeclarationSyntax disposeMethodDeclaration))
                    {
                        context.RegisterCodeFix(
                            $"{memberSymbol.Name}.Dispose() in {disposeMethod}",
                            (editor, cancellationToken) => DisposeInVirtualDisposeMethod(editor, memberSymbol, disposeMethodDeclaration, cancellationToken),
                            "Dispose member.",
                            diagnostic);
                    }
                    else if (DisposeMethod.TryFindIDisposableDispose(memberSymbol.ContainingType, semanticModel.Compilation, Search.TopLevel, out disposeMethod) &&
                             disposeMethod.TrySingleDeclaration(context.CancellationToken, out disposeMethodDeclaration))
                    {
                        switch (disposeMethodDeclaration)
                        {
                            case { ExpressionBody: { Expression: { } expression } }:
                                context.RegisterCodeFix(
                                    $"{memberSymbol.Name}.Dispose() in {disposeMethod}",
                                    (editor, cancellationToken) => editor.ReplaceNode(
                                        disposeMethodDeclaration,
                                        x => x.AsBlockBody(
                                            SyntaxFactory.ExpressionStatement(expression),
                                            Snippet.DisposeStatement(memberSymbol, editor.SemanticModel, cancellationToken))),
                                    "Dispose member.",
                                    diagnostic);
                                break;
                            case { Body: { } body }:
                                context.RegisterCodeFix(
                                    $"{memberSymbol.Name}.Dispose() in {disposeMethod}",
                                    (editor, cancellationToken) => editor.ReplaceNode(
                                        body,
                                        x => x.AddStatements(Snippet.DisposeStatement(memberSymbol, editor.SemanticModel, cancellationToken))),
                                    "Dispose member.",
                                    diagnostic);
                                break;
                        }

                    }
                }
            }
        }

        private static void DisposeInVirtualDisposeMethod(DocumentEditor editor, ISymbol memberSymbol, MethodDeclarationSyntax disposeMethodDeclaration, CancellationToken cancellationToken)
        {
            var disposeStatement = Snippet.DisposeStatement(memberSymbol, editor.SemanticModel, cancellationToken);
            if (TryFindIfDisposing(disposeMethodDeclaration, out var ifDisposing))
            {
                if (ifDisposing.Statement is BlockSyntax block)
                {
                    var statements = block.Statements.Add(disposeStatement);
                    var newBlock = block.WithStatements(statements);
                    editor.ReplaceNode(block, newBlock);
                }
                else if (ifDisposing.Statement is StatementSyntax statement)
                {
                    editor.ReplaceNode(
                        ifDisposing,
                        ifDisposing.WithStatement(SyntaxFactory.Block(statement, disposeStatement)));
                }
                else
                {
                    editor.ReplaceNode(
                        ifDisposing,
                        ifDisposing.WithStatement(SyntaxFactory.Block(disposeStatement)));
                }
            }
            else if (disposeMethodDeclaration.Body is BlockSyntax block)
            {
                ifDisposing = SyntaxFactory.IfStatement(
                    SyntaxFactory.IdentifierName(disposeMethodDeclaration.ParameterList.Parameters[0].Identifier),
                    SyntaxFactory.Block(disposeStatement));

                if (DisposeMethod.TryFindBaseCall(disposeMethodDeclaration, editor.SemanticModel, cancellationToken, out var baseCall))
                {
                    if (baseCall.TryFirstAncestor(out ExpressionStatementSyntax expressionStatement))
                    {
                        editor.InsertBefore(expressionStatement, ifDisposing);
                    }
                }
                else
                {
                    editor.ReplaceNode(
                        block,
                        block.AddStatements(ifDisposing));
                }
            }
        }

        private static SyntaxList<StatementSyntax> CreateStatements(MethodDeclarationSyntax method, StatementSyntax newStatement)
        {
            if (method.ExpressionBody != null)
            {
                return SyntaxFactory.List(new[] { SyntaxFactory.ExpressionStatement(method.ExpressionBody.Expression), newStatement });
            }

            return method.Body.Statements.Add(newStatement);
        }

        private static bool TryFindIfDisposing(MethodDeclarationSyntax disposeMethod, out IfStatementSyntax result)
        {
            if (disposeMethod.ParameterList is ParameterListSyntax parameterList &&
                parameterList.Parameters.TrySingle(out var parameter) &&
                parameter.Type == KnownSymbol.Boolean)
            {
                foreach (var statement in disposeMethod.Body.Statements)
                {
                    if (statement is IfStatementSyntax ifStatement &&
                        ifStatement.Condition is IdentifierNameSyntax identifierName &&
                        identifierName.Identifier.ValueText == parameter.Identifier.ValueText)
                    {
                        result = ifStatement;
                        return true;
                    }
                }
            }

            result = null;
            return false;
        }
    }
}
