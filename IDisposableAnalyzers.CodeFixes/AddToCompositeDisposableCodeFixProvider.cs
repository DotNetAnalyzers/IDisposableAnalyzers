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

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddToCompositeDisposableCodeFixProvider))]
    [Shared]
    internal class AddToCompositeDisposableCodeFixProvider : CodeFixProvider
    {
        private static readonly TypeSyntax CompositeDisposableType = SyntaxFactory.ParseTypeName("System.Reactive.Disposables.CompositeDisposable")
                                                                                  .WithAdditionalAnnotations(Simplifier.Annotation);

        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(
            IDISP004DontIgnoreReturnValueOfTypeIDisposable.DiagnosticId);

        public override FixAllProvider GetFixAllProvider() => null;

        /// <inheritdoc/>
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                          .ConfigureAwait(false);
            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken)
                                             .ConfigureAwait(false);
            foreach (var diagnostic in context.Diagnostics)
            {
                if (syntaxRoot.FindNode(diagnostic.Location.SourceSpan)
                              .TryFirstAncestorOrSelf<ExpressionStatementSyntax>(out var statement))
                {
                    if (TryGetField(statement, semanticModel, context.CancellationToken, out var field))
                    {
                        context.RegisterDocumentEditorFix(
                            "Add to CompositeDisposable.",
                            (editor, cancellationToken) => AddToExisting(editor, statement, field),
                            diagnostic);
                    }
                    else
                    {
                        if (semanticModel.Compilation.ReferencedAssemblyNames.Any(x => x.Name.Contains("System.Reactive")))
                        {
                            context.RegisterDocumentEditorFix(
                                "Add to new CompositeDisposable.",
                                (editor, cancellationToken) =>
                                    CreateAndInitialize(editor, statement, cancellationToken),
                                diagnostic);
                        }
                    }
                }
            }
        }

        private static void AddToExisting(DocumentEditor editor, ExpressionStatementSyntax statement, IFieldSymbol field)
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
                    ? $"{field.Name}.Add({statement.Expression});"
                    : $"this.{field.Name}.Add({statement.Expression});";

                editor.ReplaceNode(
                    statement,
                    SyntaxNodeExtensions.WithTriviaFrom(SyntaxFactory.ParseStatement(code), statement));
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
                if (previous is ExpressionStatementSyntax expressionStatement &&
                    expressionStatement.Expression is AssignmentExpressionSyntax assignment &&
                    assignment.Right is ObjectCreationExpressionSyntax objectCreation)
                {
                    if ((assignment.Left is IdentifierNameSyntax identifierName &&
                         identifierName.Identifier.ValueText == field.Name) ||
                        (assignment.Left is MemberAccessExpressionSyntax memberAccess &&
                         memberAccess.Expression is ThisExpressionSyntax &&
                         memberAccess.Name.Identifier.ValueText == field.Name))
                    {
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
                                  ? SyntaxFactory.IdentifierName(field.Name())
                                  : SyntaxFactory.ParseExpression($"this.{field.Name()}");

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

        private static bool TryGetField(SyntaxNode node, SemanticModel semanticModel, CancellationToken cancellationToken, out IFieldSymbol field)
        {
            field = null;
            var typeDeclaration = node.FirstAncestor<TypeDeclarationSyntax>();
            if (typeDeclaration == null)
            {
                return false;
            }

            var type = semanticModel.GetDeclaredSymbolSafe(typeDeclaration, cancellationToken);
            if (type == null)
            {
                return false;
            }

            foreach (var member in type.GetMembers())
            {
                if (member is IFieldSymbol candidateField &&
                    candidateField.Type == KnownSymbol.CompositeDisposable)
                {
                    field = candidateField;
                    return true;
                }
            }

            return false;
        }
    }
}
