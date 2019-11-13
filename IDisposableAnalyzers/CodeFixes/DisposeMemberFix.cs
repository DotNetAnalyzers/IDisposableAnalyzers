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
                    semanticModel.TryGetSymbol(member, context.CancellationToken, out ISymbol symbol) &&
                    FieldOrProperty.TryCreate(symbol, out var disposable))
                {
                    if (DisposeMethod.TryFindVirtualDispose(symbol.ContainingType, semanticModel.Compilation, Search.TopLevel, out var disposeSymbol) &&
                        disposeSymbol.TrySingleDeclaration(context.CancellationToken, out MethodDeclarationSyntax disposeDeclaration))
                    {
                        if (TryFindIfDisposing(disposeDeclaration, out var ifDisposing))
                        {
                            context.RegisterCodeFix(
                                $"{symbol.Name}.Dispose() in {disposeSymbol}",
                                (editor, cancellationToken) => editor.ReplaceNode(
                                    ifDisposing.Statement,
                                    x => x is BlockSyntax block
                                        ? block.AddStatements(IDisposableFactory.DisposeStatement(disposable, editor.SemanticModel, cancellationToken))
                                        : SyntaxFactory.Block(x, IDisposableFactory.DisposeStatement(disposable, editor.SemanticModel, cancellationToken))),
                                "Dispose member.",
                                diagnostic);
                        }
                        else if (TryFindIfNotDisposingReturn(disposeDeclaration, out var ifNotDisposing) &&
                                 ifNotDisposing.Parent is BlockSyntax)
                        {
                            context.RegisterCodeFix(
                                $"{symbol.Name}.Dispose() in {disposeSymbol}",
                                (editor, cancellationToken) =>
                                {
                                    if (DisposeMethod.TryFindBaseCall(disposeDeclaration, editor.SemanticModel, cancellationToken, out var baseCall))
                                    {
                                        editor.InsertBefore(
                                            baseCall,
                                            IDisposableFactory.DisposeStatement(disposable, editor.SemanticModel, cancellationToken));
                                    }
                                    else
                                    {
                                        editor.InsertAfter(
                                            ifNotDisposing,
                                            IDisposableFactory.DisposeStatement(disposable, editor.SemanticModel, cancellationToken));
                                    }
                                },
                                "Dispose member.",
                                diagnostic);
                        }
                        else if (disposeDeclaration.Body is { } block)
                        {
                            context.RegisterCodeFix(
                                $"{symbol.Name}.Dispose() in {disposeSymbol}",
                                (editor, cancellationToken) =>
                                {
                                    ifDisposing = SyntaxFactory.IfStatement(
                                        SyntaxFactory.IdentifierName(disposeDeclaration.ParameterList.Parameters[0].Identifier),
                                        SyntaxFactory.Block(IDisposableFactory.DisposeStatement(disposable, editor.SemanticModel, cancellationToken)));
                                    if (DisposeMethod.TryFindBaseCall(disposeDeclaration, editor.SemanticModel, cancellationToken, out var baseCall))
                                    {
                                        editor.InsertBefore(
                                            baseCall.Parent,
                                            ifDisposing);
                                    }
                                    else
                                    {
                                        _ = editor.ReplaceNode(
                                            block,
                                            x => x.AddStatements(ifDisposing));
                                    }
                                },
                                "Dispose member.",
                                diagnostic);
                        }
                    }
                    else if (DisposeMethod.TryFindIDisposableDispose(symbol.ContainingType, semanticModel.Compilation, Search.TopLevel, out disposeSymbol) &&
                             disposeSymbol.TrySingleDeclaration(context.CancellationToken, out disposeDeclaration))
                    {
                        switch (disposeDeclaration)
                        {
                            case { ExpressionBody: { Expression: { } expression } }:
                                context.RegisterCodeFix(
                                    $"{symbol.Name}.Dispose() in {disposeSymbol}",
                                    (editor, cancellationToken) => editor.ReplaceNode(
                                        disposeDeclaration,
                                        x => x.AsBlockBody(
                                            SyntaxFactory.ExpressionStatement(expression),
                                            IDisposableFactory.DisposeStatement(disposable, editor.SemanticModel, cancellationToken))),
                                    "Dispose member.",
                                    diagnostic);
                                break;
                            case { Body: { } body }:
                                context.RegisterCodeFix(
                                    $"{symbol.Name}.Dispose() in {disposeSymbol}",
                                    (editor, cancellationToken) => editor.ReplaceNode(
                                        body,
                                        x => x.AddStatements(IDisposableFactory.DisposeStatement(disposable, editor.SemanticModel, cancellationToken))),
                                    "Dispose member.",
                                    diagnostic);
                                break;
                        }
                    }
                }
            }
        }

        private static bool TryFindIfDisposing(MethodDeclarationSyntax disposeMethod, out IfStatementSyntax result)
        {
            if (disposeMethod is { ParameterList: { Parameters: { Count: 1 } parameters }, Body: { } body } &&
                parameters[0] is { Type: { } type, Identifier: { ValueText: { } valueText } } &&
                type == KnownSymbol.Boolean)
            {
                foreach (var statement in body.Statements)
                {
                    if (statement is IfStatementSyntax { Condition: IdentifierNameSyntax condition } ifStatement &&
                        condition.Identifier.ValueText == valueText)
                    {
                        result = ifStatement;
                        return true;
                    }
                }
            }

            result = null;
            return false;
        }

        private static bool TryFindIfNotDisposingReturn(MethodDeclarationSyntax disposeMethod, out IfStatementSyntax result)
        {
            if (disposeMethod is { ParameterList: { Parameters: { Count: 1 } parameters }, Body: { } body } &&
                parameters[0] is { Type: { } type, Identifier: { ValueText: { } valueText } } &&
                type == KnownSymbol.Boolean)
            {
                foreach (var statement in body.Statements)
                {
                    if (statement is IfStatementSyntax { Condition: PrefixUnaryExpressionSyntax { Operand: IdentifierNameSyntax operand } condition } ifStatement &&
                        condition.IsKind(SyntaxKind.LogicalNotExpression) &&
                        operand.Identifier.ValueText == valueText &&
                        IsReturn(ifStatement.Statement))
                    {
                        result = ifStatement;
                        return true;
                    }
                }
            }

            result = null;
            return false;

            bool IsReturn(StatementSyntax statement)
            {
                return statement switch
                {
                    ReturnStatementSyntax _ => true,
                    BlockSyntax { Statements: { } statements } => statements.LastOrDefault() is ReturnStatementSyntax,
                    _ => false,
                };
            }
        }
    }
}
