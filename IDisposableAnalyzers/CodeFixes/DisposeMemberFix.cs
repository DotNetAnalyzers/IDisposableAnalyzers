namespace IDisposableAnalyzers
{
    using System;
    using System.Collections.Immutable;
    using System.Composition;
    using System.Diagnostics.CodeAnalysis;
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
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(Descriptors.IDISP002DisposeMember.Id);

        protected override async Task RegisterCodeFixesAsync(DocumentEditorCodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                          .ConfigureAwait(false);

            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken)
                                             .ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                if (syntaxRoot.TryFindNode<MemberDeclarationSyntax>(diagnostic, out var member) &&
                    semanticModel.TryGetSymbol(member, context.CancellationToken, out ISymbol? symbol) &&
                    FieldOrProperty.TryCreate(symbol, out var disposable))
                {
                    if (diagnostic.AdditionalLocations.TrySingle(out var additionalLocation) &&
                        syntaxRoot.TryFindNodeOrAncestor(additionalLocation, out MethodDeclarationSyntax? method))
                    {
                        switch (method)
                        {
                            case { Identifier: { ValueText: "DisposeAsync" } }
                                when IDisposableFactory.MemberAccessContext.Create(disposable, method, semanticModel, context.CancellationToken) is { NotNull: { } }:
                                context.RegisterCodeFix(
                                    $"{symbol.Name}.DisposeAsync() in {method}",
                                    (editor, cancellationToken) => editor.ReplaceNode(
                                        method,
                                        x => DisposeAsync(x, editor, cancellationToken)),
                                    "DisposeAsync",
                                    diagnostic);

                                MethodDeclarationSyntax DisposeAsync(MethodDeclarationSyntax old, DocumentEditor editor, CancellationToken cancellationToken)
                                {
                                    return old switch
                                    {
                                        { ExpressionBody: { Expression: { } expression } }
                                        => old.AsBlockBody(
                                            SyntaxFactory.ExpressionStatement(expression),
                                            IDisposableFactory.DisposeAsyncStatement(disposable, method!, editor.SemanticModel, cancellationToken))
                                              .WithAsync(),
                                        { Body: { } body }
                                        => old.WithBody(
                                            body.AddStatements(IDisposableFactory.DisposeAsyncStatement(disposable, method!, editor.SemanticModel, cancellationToken)))
                                              .WithAsync(),
                                        _ => throw new InvalidOperationException("Error generating DisposeAsync"),
                                    };
                                }

                                break;
                            case { Identifier: { ValueText: "Dispose" }, ParameterList: { Parameters: { Count: 1 } parameters }, Body: { } body }:
                                context.RegisterCodeFix(
                                    $"{symbol.Name}.Dispose() in {method}",
                                    (editor, token) => DisposeInVirtual(editor, token),
                                    "Dispose member.",
                                    diagnostic);

                                void DisposeInVirtual(DocumentEditor editor, CancellationToken cancellationToken)
                                {
                                    if (TryFindIfNotDisposingReturn(method!, out var ifNotDisposingReturn) &&
                                        ifNotDisposingReturn.Parent is BlockSyntax)
                                    {
                                        editor.InsertAfter(
                                            ifNotDisposingReturn,
                                            IDisposableFactory.DisposeStatement(disposable, method!, editor.SemanticModel, cancellationToken));
                                    }
                                    else if (TryFindIfDisposing(method!, out var ifDisposing))
                                    {
                                        _ = editor.ReplaceNode(
                                            ifDisposing.Statement,
                                            x => x is BlockSyntax ifBlock
                                                ? ifBlock.AddStatements(IDisposableFactory.DisposeStatement(disposable, method!, editor.SemanticModel, cancellationToken))
                                                : SyntaxFactory.Block(x, IDisposableFactory.DisposeStatement(disposable, method!, editor.SemanticModel, cancellationToken)));
                                    }
                                    else
                                    {
                                        ifDisposing = SyntaxFactory.IfStatement(
                                            SyntaxFactory.IdentifierName(parameters[0].Identifier),
                                            SyntaxFactory.Block(IDisposableFactory.DisposeStatement(disposable, method!, editor.SemanticModel, cancellationToken)));
                                        if (DisposeMethod.FindBaseCall(method!, editor.SemanticModel, cancellationToken) is { Parent: { } } baseCall)
                                        {
                                            editor.InsertBefore(baseCall.Parent, ifDisposing);
                                        }
                                        else
                                        {
                                            _ = editor.ReplaceNode(body!, x => x.AddStatements(ifDisposing));
                                        }
                                    }
                                }

                                break;
                            case { Identifier: { ValueText: "Dispose" }, ParameterList: { Parameters: { Count: 0 } }, Body: { } body }:
                                context.RegisterCodeFix(
                                    $"{symbol.Name}.Dispose() in {method}",
                                    (editor, cancellationToken) => DisposeWhenNoParameter(editor, cancellationToken),
                                    "Dispose member.",
                                    diagnostic);

                                void DisposeWhenNoParameter(DocumentEditor editor, CancellationToken cancellationToken)
                                {
                                    if (DisposeMethod.FindBaseCall(method!, editor.SemanticModel, cancellationToken) is { Parent: { } } baseCall)
                                    {
                                        editor.InsertBefore(
                                            baseCall.Parent,
                                            IDisposableFactory.DisposeStatement(disposable, method!, editor.SemanticModel, cancellationToken));
                                    }
                                    else
                                    {
                                        _ = editor.ReplaceNode(
                                            body!,
                                            x => x.AddStatements(IDisposableFactory.DisposeStatement(disposable, method!, editor.SemanticModel, cancellationToken)));
                                    }
                                }

                                break;

                            case { Identifier: { ValueText: "Dispose" }, ParameterList: { Parameters: { Count: 0 } }, ExpressionBody: { Expression: { } expression } }:
                                context.RegisterCodeFix(
                                    $"{symbol.Name}.Dispose() in {method}",
                                    (editor, cancellationToken) => editor.ReplaceNode(
                                        method,
                                        x => x.AsBlockBody(
                                            SyntaxFactory.ExpressionStatement(expression),
                                            IDisposableFactory.DisposeStatement(disposable, method!, editor.SemanticModel, cancellationToken))),
                                    "Dispose member.",
                                    diagnostic);
                                break;
                            case { Identifier: { ValueText: "Dispose" } }:
                            case { Identifier: { ValueText: "DisposeAsync" } }:
                                break;
                            default:
                                context.RegisterCodeFix(
                                    $"{symbol.Name}.Dispose() in {method}",
                                    (editor, cancellationToken) => editor.ReplaceNode(
                                        method,
                                        x => Dispose(x, editor, cancellationToken)),
                                    "TearDown",
                                    diagnostic);

                                MethodDeclarationSyntax Dispose(MethodDeclarationSyntax old, DocumentEditor editor, CancellationToken cancellationToken)
                                {
                                    return old switch
                                    {
                                        { ExpressionBody: { Expression: { } expression } }
                                        => old.AsBlockBody(
                                            SyntaxFactory.ExpressionStatement(expression),
                                            IDisposableFactory.DisposeStatement(disposable, method!, editor.SemanticModel, cancellationToken)),
                                        { Body: { } body }
                                        => old.WithBody(body.AddStatements(
                                                            IDisposableFactory.DisposeStatement(disposable, method!, editor.SemanticModel, cancellationToken))),
                                        _ => throw new InvalidOperationException("Error generating Dispose"),
                                    };
                                }

                                break;
                        }
                    }
                }
            }
        }

        private static bool TryFindIfDisposing(MethodDeclarationSyntax disposeMethod, [NotNullWhen(true)] out IfStatementSyntax? result)
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

        private static bool TryFindIfNotDisposingReturn(MethodDeclarationSyntax disposeMethod, [NotNullWhen(true)] out IfStatementSyntax? result)
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

            static bool IsReturn(StatementSyntax statement)
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
