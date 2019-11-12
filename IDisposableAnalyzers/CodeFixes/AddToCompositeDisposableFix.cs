namespace IDisposableAnalyzers
{
    using System.Collections.Immutable;
    using System.Composition;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Gu.Roslyn.AnalyzerExtensions;
    using Gu.Roslyn.CodeFixExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Editing;
    using Microsoft.CodeAnalysis.Formatting;
    using Microsoft.CodeAnalysis.Simplification;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddToCompositeDisposableFix))]
    [Shared]
    internal class AddToCompositeDisposableFix : DocumentEditorCodeFixProvider
    {
        private static readonly TypeSyntax CompositeDisposableType = SyntaxFactory.ParseTypeName("System.Reactive.Disposables.CompositeDisposable")
                                                                                  .WithAdditionalAnnotations(Simplifier.Annotation);

        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(
            IDISP004DontIgnoreCreated.DiagnosticId);

        protected override DocumentEditorFixAllProvider FixAllProvider() => null;

        /// <inheritdoc/>
        protected override async Task RegisterCodeFixesAsync(DocumentEditorCodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                                   .ConfigureAwait(false);
            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken)
                                                      .ConfigureAwait(false);
            if (semanticModel.Compilation.ReferencedAssemblyNames.Any(x => x.Name.Contains("System.Reactive")))
            {
                foreach (var diagnostic in context.Diagnostics)
                {
                    if (syntaxRoot.TryFindNodeOrAncestor(diagnostic, out ExpressionStatementSyntax statement))
                    {
                        if (TryGetField(statement, semanticModel, context.CancellationToken, out var field))
                        {
                            context.RegisterCodeFix(
                                $"Add to {field.Identifier.ValueText}.",
                                (editor, _) => AddToExisting(editor, statement, field),
                                (string)null,
                                diagnostic);
                        }
                        else
                        {
                            context.RegisterCodeFix(
                                "Add to new CompositeDisposable.",
                                (editor, cancellationToken) => CreateAndInitialize(editor, statement, cancellationToken),
                                (string)null,
                                diagnostic);
                        }
                    }
                }
            }
        }

        private static void AddToExisting(DocumentEditor editor, ExpressionStatementSyntax statement, VariableDeclaratorSyntax field)
        {
            if (TryGetPreviousStatement(out var previous) &&
                TryGetCreateCompositeDisposable(out var compositeDisposableCreation))
            {
                editor.RemoveNode(statement);
                editor.AddItemToCollectionInitializer(
                    compositeDisposableCreation,
                    statement.Expression,
                    statement.GetTrailingTrivia());
            }
            else
            {
                var code = editor.SemanticModel.UnderscoreFields()
                    ? $"{field.Identifier.ValueText}.Add({statement.Expression})"
                    : $"this.{field.Identifier.ValueText}.Add({statement.Expression})";

                _ = editor.ReplaceNode(
                    statement.Expression,
                    x => SyntaxFactory.ParseExpression(code)
                                      .WithTriviaFrom(x));
            }

            bool TryGetPreviousStatement(out StatementSyntax result)
            {
                result = null;
                if (statement.Parent is BlockSyntax block)
                {
                    return block.Statements.TryElementAt(block.Statements.IndexOf(statement) - 1, out result);
                }

                return false;
            }

            bool TryGetCreateCompositeDisposable(out ObjectCreationExpressionSyntax result)
            {
                if (previous is ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax { Left: { } left, Right: ObjectCreationExpressionSyntax objectCreation } })
                {
                    switch (left)
                    {
                        case IdentifierNameSyntax identifierName
                            when identifierName.Identifier.ValueText == field.Identifier.ValueText:
                        case MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax _, Name: { } name }
                                when name.Identifier.ValueText == field.Identifier.ValueText:
                            result = objectCreation;
                            return true;
                    }
                }

                result = null;
                return false;
            }
        }

        private static void CreateAndInitialize(DocumentEditor editor, ExpressionStatementSyntax statement, CancellationToken cancellationToken)
        {
            var containingType = statement.FirstAncestor<TypeDeclarationSyntax>();
            var usesUnderscoreNames = editor.SemanticModel.UnderscoreFields();
            var field = editor.AddField(
                containingType,
                usesUnderscoreNames
                    ? "_disposable"
                    : "disposable",
                Accessibility.Private,
                DeclarationModifiers.ReadOnly,
                CompositeDisposableType,
                cancellationToken);

            var fieldAccess = usesUnderscoreNames
                                  ? SyntaxFactory.IdentifierName(field.Declaration.Variables[0].Identifier.ValueText)
                                  : SyntaxFactory.ParseExpression($"this.{field.Declaration.Variables[0].Identifier.ValueText}");

            var trailingTrivia = statement.GetTrailingTrivia();
            if (trailingTrivia.Any(SyntaxKind.SingleLineCommentTrivia))
            {
                var padding = new string(' ', statement.GetLeadingTrivia().Span.Length);
                var code = StringBuilderPool.Borrow()
                                            .AppendLine($"{padding}{fieldAccess} = new System.Reactive.Disposables.CompositeDisposable")
                                            .AppendLine($"{padding}{{")
                                            .AppendLine($"    {statement.GetLeadingTrivia()}{statement.Expression},{trailingTrivia.ToString().Trim('\r', '\n')}")
                                            .AppendLine($"{padding}}};")
                                            .Return();

                editor.ReplaceNode(
                    statement,
                    SyntaxFactory.ParseStatement(code)
                                 .WithSimplifiedNames());
            }
            else
            {
                editor.ReplaceNode(
                    statement,
                    SyntaxFactory.ParseStatement($"{fieldAccess} = new System.Reactive.Disposables.CompositeDisposable {{ {statement.Expression} }};")
                                 .WithAdditionalAnnotations(Formatter.Annotation)
                                 .WithSimplifiedNames());
            }
        }

        private static bool TryGetField(SyntaxNode node, SemanticModel semanticModel, CancellationToken cancellationToken, out VariableDeclaratorSyntax field)
        {
            if (node.TryFirstAncestor(out TypeDeclarationSyntax containingType))
            {
                foreach (var member in containingType.Members)
                {
                    if (member is FieldDeclarationSyntax { Declaration: { Type: { } type, Variables: { Count: 1 } variables } } &&
                        semanticModel.TryGetType(type, cancellationToken, out var typeSymbol) &&
                        typeSymbol == KnownSymbol.CompositeDisposable)
                    {
                        field = variables[0];
                        return true;
                    }
                }
            }

            field = null;
            return false;
        }
    }
}
