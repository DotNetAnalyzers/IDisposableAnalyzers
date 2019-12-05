namespace IDisposableAnalyzers
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.Diagnostics.CodeAnalysis;
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
        private static readonly TypeSyntax CompositeDisposableType = SyntaxFactory.QualifiedName(
                                                                                      SyntaxFactory.QualifiedName(
                                                                                          SyntaxFactory.QualifiedName(
                                                                                              SyntaxFactory.IdentifierName("System"),
                                                                                              SyntaxFactory.IdentifierName("Reactive")),
                                                                                          SyntaxFactory.IdentifierName("Disposables")),
                                                                                      SyntaxFactory.IdentifierName("CompositeDisposable"))
                                                                                  .WithAdditionalAnnotations(Simplifier.Annotation);

        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(
            Descriptors.IDISP004DoNotIgnoreCreated.Id);

        protected override DocumentEditorFixAllProvider? FixAllProvider() => null;

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
                    if (syntaxRoot.TryFindNodeOrAncestor(diagnostic, out ExpressionStatementSyntax? statement) &&
                        statement is { Expression: { } expression })
                    {
                        if (TryGetField(statement, semanticModel, context.CancellationToken, out var field))
                        {
                            context.RegisterCodeFix(
                                $"Add to {field.Identifier.ValueText}.",
                                (editor, cancellationToken) => AddToExisting(editor, cancellationToken),
                                (string?)null,
                                diagnostic);

                            void AddToExisting(DocumentEditor editor, CancellationToken cancellationToken)
                            {
                                if (PreviousStatement() is ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax { Left: { } left, Right: ObjectCreationExpressionSyntax objectCreation } } &&
                                    IsField(left))
                                {
                                    switch (objectCreation)
                                    {
                                        case { Initializer: { } initializer }:
                                            editor.RemoveNode(statement);
                                            editor.ReplaceNode(
                                                initializer,
                                                x => SyntaxFactory.InitializerExpression(
                                                         SyntaxKind.CollectionInitializerExpression,
                                                         SyntaxFactory.SeparatedList(Expressions(x), Separators(x)))
                                                                  .WithAdditionalAnnotations(Formatter.Annotation));

                                            IEnumerable<ExpressionSyntax> Expressions(InitializerExpressionSyntax old)
                                            {
                                                if (old.Expressions.Count == 0)
                                                {
                                                    yield return expression.WithAdditionalAnnotations(Formatter.Annotation);
                                                }
                                                else
                                                {
                                                    foreach (var e in old.Expressions.Take(old.Expressions.Count - 1))
                                                    {
                                                        yield return e;
                                                    }

                                                    yield return old.Expressions.Last().WithoutTrailingTrivia();
                                                    yield return expression.WithAdditionalAnnotations(Formatter.Annotation);
                                                }
                                            }

                                            IEnumerable<SyntaxToken> Separators(InitializerExpressionSyntax old)
                                            {
                                                var separators = old.Expressions.GetSeparators();
                                                if (old.Expressions.SeparatorCount < old.Expressions.Count)
                                                {
                                                    var last = old.Expressions.Last();
                                                    if (last.HasTrailingTrivia &&
                                                        last.GetTrailingTrivia() is { } trivia)
                                                    {
                                                        if (trivia.Last().IsKind(SyntaxKind.EndOfLineTrivia))
                                                        {
                                                            separators = separators.Append(SyntaxFactory.Token(default, SyntaxKind.CommaToken, trivia));
                                                        }
                                                        else
                                                        {
                                                            separators = separators.Append(SyntaxFactory.Token(default, SyntaxKind.CommaToken, trivia.Add(SyntaxFactory.LineFeed)));
                                                        }
                                                    }
                                                    else
                                                    {
                                                        separators = separators.Append(SyntaxFactory.Token(default, SyntaxKind.CommaToken, SyntaxFactory.TriviaList(SyntaxFactory.LineFeed)));
                                                    }
                                                }

                                                return separators.Append(SyntaxFactory.Token(default, SyntaxKind.CommaToken, statement!.GetTrailingTrivia()));
                                            }

                                            break;
                                        case { ArgumentList: { } argumentList }
                                            when argumentList.Arguments.TryFirst(x => !(x.Expression is LiteralExpressionSyntax), out _):
                                            editor.RemoveNode(statement);
                                            editor.ReplaceNode(
                                                argumentList,
                                                x => x.AddArguments(SyntaxFactory.Argument(expression)));
                                            break;
                                        default:
                                            editor.RemoveNode(statement);
                                            if (objectCreation.ArgumentList is { Arguments: { Count: 0 } } empty)
                                            {
                                                editor.RemoveNode(empty);
                                            }

                                            editor.ReplaceNode(
                                                objectCreation,
                                                x => x.WithInitializer(CreateInitializer(statement!)));
                                            break;
                                    }

                                    return;
                                }

                                _ = editor.ReplaceNode(
                                    expression,
                                    x => SyntaxFactory.InvocationExpression(
                                                          SyntaxFactory.MemberAccessExpression(
                                                              SyntaxKind.SimpleMemberAccessExpression,
                                                              IDisposableFactory.MemberAccess(field!.Identifier, semanticModel, cancellationToken),
                                                              SyntaxFactory.IdentifierName("Add")),
                                                          SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(x))))
                                                      .WithTriviaFrom(x));

                                StatementSyntax? PreviousStatement()
                                {
                                    return statement is { Parent: BlockSyntax block } &&
                                           block.Statements.TryElementAt(block.Statements.IndexOf(statement) - 1, out var result)
                                           ? result
                                           : null;
                                }

                                bool IsField(ExpressionSyntax e)
                                {
                                    switch (e)
                                    {
                                        case IdentifierNameSyntax identifierName
                                            when identifierName.Identifier.ValueText == field!.Identifier.ValueText:
                                        case MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax _, Name: { } name }
                                            when name.Identifier.ValueText == field!.Identifier.ValueText:
                                            return true;
                                        default:
                                            return false;
                                    }
                                }
                            }
                        }
                        else if (statement.FirstAncestor<TypeDeclarationSyntax>() is { } containingType)
                        {
                            context.RegisterCodeFix(
                                "Add to new CompositeDisposable.",
                                (editor, cancellationToken) => CreateAndInitializeAsync(editor, cancellationToken),
                                (string?)null,
                                diagnostic);

                            async Task CreateAndInitializeAsync(DocumentEditor editor, CancellationToken cancellationToken)
                            {
                                var disposable = await editor.AddFieldAsync(
                                    containingType,
                                    "disposable",
                                    Accessibility.Private,
                                    DeclarationModifiers.ReadOnly,
                                    CompositeDisposableType,
                                    cancellationToken).ConfigureAwait(false);

                                _ = editor.ReplaceNode(
                                    statement!,
                                    x => x.WithExpression(
                                        SyntaxFactory.AssignmentExpression(
                                            SyntaxKind.SimpleAssignmentExpression,
                                            disposable,
                                            SyntaxFactory.ObjectCreationExpression(
                                                CompositeDisposableType,
                                                null,
                                                CreateInitializer(x))))
                                          .WithoutTrailingTrivia()
                                          .WithAdditionalAnnotations(Formatter.Annotation));
                            }
                        }
                    }
                }
            }
        }

        private static InitializerExpressionSyntax CreateInitializer(ExpressionStatementSyntax x)
        {
            return SyntaxFactory.InitializerExpression(
                SyntaxKind.CollectionInitializerExpression,
                SyntaxFactory.SeparatedList(
                    new[] { x.Expression.WithAdditionalAnnotations(Formatter.Annotation) },
                    new[] { SyntaxFactory.Token(default, SyntaxKind.CommaToken, x.GetTrailingTrivia()) }));
        }

        private static bool TryGetField(SyntaxNode node, SemanticModel semanticModel, CancellationToken cancellationToken, [NotNullWhen(true)] out VariableDeclaratorSyntax? field)
        {
            if (node.TryFirstAncestor(out TypeDeclarationSyntax? containingType))
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
