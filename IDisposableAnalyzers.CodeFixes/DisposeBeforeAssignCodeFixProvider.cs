namespace IDisposableAnalyzers
{
    using System.Collections.Immutable;
    using System.Composition;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DisposeBeforeAssignCodeFixProvider))]
    [Shared]
    internal class DisposeBeforeAssignCodeFixProvider : CodeFixProvider
    {
        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(IDISP003DisposeBeforeReassigning.DiagnosticId);

        /// <inheritdoc/>
        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        /// <inheritdoc/>
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                          .ConfigureAwait(false) as CompilationUnitSyntax;
            if (syntaxRoot == null)
            {
                return;
            }

            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken)
                                             .ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                var token = syntaxRoot.FindToken(diagnostic.Location.SourceSpan.Start);
                if (string.IsNullOrEmpty(token.ValueText) ||
                    token.IsMissing)
                {
                    continue;
                }

                if (syntaxRoot.FindNode(diagnostic.Location.SourceSpan) is AssignmentExpressionSyntax assignment)
                {
                    if (TryCreateDisposeStatement(assignment, semanticModel, context.CancellationToken, out StatementSyntax diposeStatement))
                    {
                        context.RegisterCodeFix(
                            CodeAction.Create(
                                "Dispose before assigning.",
                                cancellationToken =>
                                        ApplyDisposeBeforeAssignFixAsync(context, syntaxRoot, assignment, diposeStatement),
                                nameof(DisposeBeforeAssignCodeFixProvider)),
                            diagnostic);
                    }

                    continue;
                }

                var argument = syntaxRoot.FindNode(diagnostic.Location.SourceSpan).FirstAncestorOrSelf<ArgumentSyntax>();
                if (argument != null)
                {
                    if (TryCreateDisposeStatement(argument, semanticModel, context.CancellationToken, out StatementSyntax diposeStatement))
                    {
                        context.RegisterCodeFix(
                            CodeAction.Create(
                                "Dispose before assigning.",
                                cancellationToken =>
                                        ApplyDisposeBeforeAssignFixAsync(context, syntaxRoot, argument, diposeStatement),
                                nameof(DisposeBeforeAssignCodeFixProvider)),
                            diagnostic);
                    }
                }
            }
        }

        private static Task<Document> ApplyDisposeBeforeAssignFixAsync(CodeFixContext context, CompilationUnitSyntax syntaxRoot, SyntaxNode assignment, StatementSyntax disposeStatement)
        {
            var block = assignment.FirstAncestorOrSelf<BlockSyntax>();
            var statement = assignment.FirstAncestorOrSelf<StatementSyntax>();
            if (block == null ||
                statement == null)
            {
                return Task.FromResult(context.Document);
            }

            var newBlock = block.InsertNodesBefore(statement, new[] { disposeStatement });
            var syntaxNode = syntaxRoot.ReplaceNode(block, newBlock);
            if (disposeStatement.ToString().Contains("as IDisposable"))
            {
                syntaxNode = syntaxNode.WithUsingSystem();
            }

            return Task.FromResult(context.Document.WithSyntaxRoot(syntaxNode));
        }

        private static bool TryCreateDisposeStatement(AssignmentExpressionSyntax assignment, SemanticModel semanticModel, CancellationToken cancellationToken, out StatementSyntax result)
        {
            result = null;
            if (!(assignment.Parent is StatementSyntax && assignment.Parent.Parent is BlockSyntax))
            {
                return false;
            }

            if (Disposable.IsAssignedWithCreated(assignment.Left, semanticModel, cancellationToken, out ISymbol assignedSymbol)
                          .IsEither(Result.No, Result.Unknown))
            {
                return false;
            }

            var prefix = (assignedSymbol is IPropertySymbol || assignedSymbol is IFieldSymbol) &&
                         !assignment.UsesUnderscoreNames(semanticModel, cancellationToken)
                             ? "this."
                             : string.Empty;
            if (!Disposable.IsAssignableTo(MemberType(assignedSymbol)))
            {
                result = SyntaxFactory.ParseStatement($"({prefix}{assignment.Left} as IDisposable)?.Dispose();")
                                      .WithLeadingTrivia(SyntaxFactory.ElasticMarker)
                                      .WithTrailingTrivia(SyntaxFactory.ElasticMarker);
                return true;
            }

            if (IsAlwaysAssigned(assignedSymbol))
            {
                result = SyntaxFactory.ParseStatement($"{prefix}{assignedSymbol.Name}.Dispose();")
                                    .WithLeadingTrivia(SyntaxFactory.ElasticMarker)
                                    .WithTrailingTrivia(SyntaxFactory.ElasticMarker);
                return true;
            }

            result = SyntaxFactory.ParseStatement($"{prefix}{assignedSymbol.Name}?.Dispose();")
                                .WithLeadingTrivia(SyntaxFactory.ElasticMarker)
                                .WithTrailingTrivia(SyntaxFactory.ElasticMarker);
            return true;
        }

        private static bool TryCreateDisposeStatement(ArgumentSyntax argument, SemanticModel semanticModel, CancellationToken cancellationToken, out StatementSyntax result)
        {
            var symbol = semanticModel.GetSymbolSafe(argument.Expression, cancellationToken);
            if (symbol == null)
            {
                result = null;
                return false;
            }

            if (!Disposable.IsAssignableTo(MemberType(symbol)))
            {
                result = SyntaxFactory.ParseStatement($"({argument.Expression} as IDisposable)?.Dispose();")
                                    .WithLeadingTrivia(SyntaxFactory.ElasticMarker)
                                    .WithTrailingTrivia(SyntaxFactory.ElasticMarker);
                return true;
            }

            if (IsAlwaysAssigned(symbol))
            {
                result = SyntaxFactory.ParseStatement($"{argument.Expression}.Dispose();")
                                    .WithLeadingTrivia(SyntaxFactory.ElasticMarker)
                                    .WithTrailingTrivia(SyntaxFactory.ElasticMarker);
                return true;
            }

            result = SyntaxFactory.ParseStatement($"{argument.Expression}?.Dispose();")
                                .WithLeadingTrivia(SyntaxFactory.ElasticMarker)
                                .WithTrailingTrivia(SyntaxFactory.ElasticMarker);
            return true;
        }

        // ReSharper disable once UnusedParameter.Local
        private static bool IsAlwaysAssigned(ISymbol member)
        {
            var symbol = member as ILocalSymbol;
            if (symbol == null)
            {
                return false;
            }

            return false;
        }

        private static ITypeSymbol MemberType(ISymbol member) =>
            (member as IFieldSymbol)?.Type ??
            (member as IPropertySymbol)?.Type ??
            (member as ILocalSymbol)?.Type ??
            (member as IParameterSymbol)?.Type;
    }
}