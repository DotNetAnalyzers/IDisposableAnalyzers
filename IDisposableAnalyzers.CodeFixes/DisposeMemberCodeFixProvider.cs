namespace IDisposableAnalyzers
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DisposeMemberCodeFixProvider))]
    [Shared]
    internal class DisposeMemberCodeFixProvider : CodeFixProvider
    {
        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(IDISP002DisposeMember.DiagnosticId);

        /// <inheritdoc/>
        public override FixAllProvider GetFixAllProvider() => BacthFixer.Default;

        /// <inheritdoc/>
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                          .ConfigureAwait(false);

            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken)
                                             .ConfigureAwait(false);

            var usesUnderscoreNames = syntaxRoot.UsesUnderscore(semanticModel, context.CancellationToken);

            foreach (var diagnostic in context.Diagnostics)
            {
                var fix = CreateFix(diagnostic, syntaxRoot, semanticModel, context.CancellationToken, usesUnderscoreNames);
                if (fix.DisposeStatement != null)
                {
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            "Dispose member.",
                            _ => Task.FromResult(context.Document.WithSyntaxRoot(ApplyFix(syntaxRoot, fix))),
                            nameof(DisposeMemberCodeFixProvider)),
                        diagnostic);
                }
            }
        }

        private static Fix CreateFix(Diagnostic diagnostic, SyntaxNode syntaxRoot, SemanticModel semanticModel, CancellationToken cancellationToken, bool usesUnderscoreNames)
        {
            var token = syntaxRoot.FindToken(diagnostic.Location.SourceSpan.Start);
            if (string.IsNullOrEmpty(token.ValueText) ||
                token.IsMissing)
            {
                return default(Fix);
            }

            var member = (MemberDeclarationSyntax)syntaxRoot.FindNode(diagnostic.Location.SourceSpan);
            if (member is MethodDeclarationSyntax methodDeclaration)
            {
                var method = semanticModel.GetDeclaredSymbolSafe(methodDeclaration, cancellationToken);
                if (method.Parameters.Length != 1)
                {
                    return default(Fix);
                }

                var overridden = method.OverriddenMethod;
                var baseCall = SyntaxFactory.ParseStatement($"base.{overridden.Name}({method.Parameters[0].Name});")
                                                      .WithLeadingTrivia(SyntaxFactory.ElasticMarker)
                                                      .WithTrailingTrivia(SyntaxFactory.ElasticMarker);
                return new Fix(baseCall, methodDeclaration);
            }

            if (!TryGetMemberSymbol(member, semanticModel, cancellationToken, out ISymbol memberSymbol))
            {
                return default(Fix);
            }

            if (Disposable.TryGetDisposeMethod(memberSymbol.ContainingType, Search.TopLevel, out IMethodSymbol disposeMethodSymbol))
            {
                if (disposeMethodSymbol.DeclaredAccessibility == Accessibility.Public &&
                    disposeMethodSymbol.Parameters.Length == 0 &&
                    disposeMethodSymbol.TryGetSingleDeclaration(cancellationToken, out MethodDeclarationSyntax disposeMethodDeclaration))
                {
                    var disposeStatement = CreateDisposeStatement(memberSymbol, semanticModel, cancellationToken, usesUnderscoreNames);
                    return new Fix(disposeStatement, disposeMethodDeclaration);
                }

                if (disposeMethodSymbol.Parameters.Length == 1 &&
                    disposeMethodSymbol.TryGetSingleDeclaration(cancellationToken, out disposeMethodDeclaration))
                {
                    var parameterType = semanticModel.GetTypeInfoSafe(disposeMethodDeclaration.ParameterList.Parameters[0]?.Type, cancellationToken).Type;
                    if (parameterType == KnownSymbol.Boolean)
                    {
                        var disposeStatement = CreateDisposeStatement(memberSymbol, semanticModel, cancellationToken, usesUnderscoreNames);
                        return new Fix(disposeStatement, disposeMethodDeclaration);
                    }
                }
            }

            return default(Fix);
        }

        private static SyntaxNode ApplyFix(SyntaxNode syntaxRoot, Fix fix)
        {
            var disposeMethod = syntaxRoot.GetCurrentNode(fix.DisposeMethod) ?? fix.DisposeMethod;
            if (disposeMethod == null)
            {
                return syntaxRoot;
            }

            if (disposeMethod.Modifiers.Any(SyntaxKind.PublicKeyword) && disposeMethod.ParameterList.Parameters.Count == 0)
            {
                var statements = CreateStatements(disposeMethod, fix.DisposeStatement);
                if (disposeMethod.Body != null)
                {
                    var updatedBody = disposeMethod.Body.WithStatements(statements);
                    return syntaxRoot.ReplaceNode(disposeMethod.Body, updatedBody);
                }

                if (disposeMethod.ExpressionBody != null)
                {
                    var newMethod = disposeMethod.WithBody(SyntaxFactory.Block(statements))
                                                 .WithExpressionBody(null)
                                                 .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.None));
                    return syntaxRoot.ReplaceNode(disposeMethod, newMethod);
                }

                return syntaxRoot;
            }

            if (disposeMethod.ParameterList.Parameters.Count == 1 && disposeMethod.Body != null)
            {
                if (fix.DisposeStatement is ExpressionStatementSyntax expressionStatement &&
                    expressionStatement.Expression is InvocationExpressionSyntax invocation &&
                    invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                    memberAccess.Expression is BaseExpressionSyntax)
                {
                    var statements = disposeMethod.Body.Statements.Add(fix.DisposeStatement);
                    var newBlock = disposeMethod.Body.WithStatements(statements);
                    return syntaxRoot.ReplaceNode(disposeMethod.Body, newBlock);
                }

                foreach (var statement in disposeMethod.Body.Statements)
                {
                    var ifStatement = statement as IfStatementSyntax;
                    if (ifStatement == null)
                    {
                        continue;
                    }

                    if ((ifStatement.Condition as IdentifierNameSyntax)?.Identifier.ValueText == "disposing")
                    {
                        if (ifStatement.Statement is BlockSyntax block)
                        {
                            var statements = block.Statements.Add(fix.DisposeStatement);
                            var newBlock = block.WithStatements(statements);
                            return syntaxRoot.ReplaceNode(block, newBlock);
                        }
                    }
                }
            }

            return syntaxRoot;
        }

        private static StatementSyntax CreateDisposeStatement(ISymbol member, SemanticModel semanticModel, CancellationToken cancellationToken, bool usesUnderScoreNames)
        {
            var prefix = usesUnderScoreNames ? string.Empty : "this.";
            if (!Disposable.IsAssignableTo(MemberType(member)))
            {
                return SyntaxFactory.ParseStatement($"({prefix}{member.Name} as System.IDisposable)?.Dispose();")
                             .WithLeadingTrivia(SyntaxFactory.ElasticMarker)
                             .WithTrailingTrivia(SyntaxFactory.ElasticMarker)
                             .WithSimplifiedNames();
            }

            if (IsReadOnly(member) &&
                IsNeverNull(member, semanticModel, cancellationToken))
            {
                return SyntaxFactory.ParseStatement($"{prefix}{member.Name}.Dispose();")
                                    .WithLeadingTrivia(SyntaxFactory.ElasticMarker)
                                    .WithTrailingTrivia(SyntaxFactory.ElasticMarker);
            }

            return SyntaxFactory.ParseStatement($"{prefix}{member.Name}?.Dispose();")
                                .WithLeadingTrivia(SyntaxFactory.ElasticMarker)
                                .WithTrailingTrivia(SyntaxFactory.ElasticMarker);
        }

        private static SyntaxList<StatementSyntax> CreateStatements(MethodDeclarationSyntax method, StatementSyntax newStatement)
        {
            if (method.ExpressionBody != null)
            {
                return SyntaxFactory.List(new[] { SyntaxFactory.ExpressionStatement(method.ExpressionBody.Expression), newStatement });
            }

            return method.Body.Statements.Add(newStatement);
        }

        private static bool IsReadOnly(ISymbol member)
        {
            var isReadOnly = (member as IFieldSymbol)?.IsReadOnly ?? (member as IPropertySymbol)?.IsReadOnly;
            if (isReadOnly == null)
            {
                throw new InvalidOperationException($"Could not figure out if member: {member} is readonly.");
            }

            return isReadOnly.Value;
        }

        private static bool IsNeverNull(ISymbol member, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (!(member is IFieldSymbol || member is IPropertySymbol))
            {
                return false;
            }

            using (var sources = AssignedValueWalker.Create(member, semanticModel, cancellationToken))
            {
                foreach (var value in sources.Item)
                {
                    if (value is ObjectCreationExpressionSyntax)
                    {
                        continue;
                    }

                    return false;
                }
            }

            return true;
        }

        private static ITypeSymbol MemberType(ISymbol member) => (member as IFieldSymbol)?.Type ?? (member as IPropertySymbol)?.Type;

        private static bool TryGetMemberSymbol(MemberDeclarationSyntax member, SemanticModel semanticModel, CancellationToken cancellationToken, out ISymbol symbol)
        {
            if (member is FieldDeclarationSyntax field &&
                field.Declaration.Variables.TryGetSingle(out VariableDeclaratorSyntax declarator))
            {
                symbol = semanticModel.GetDeclaredSymbolSafe(declarator, cancellationToken);
                return symbol != null;
            }

            if (member is PropertyDeclarationSyntax property)
            {
                symbol = semanticModel.GetDeclaredSymbolSafe(property, cancellationToken);
                return symbol != null;
            }

            symbol = null;
            return false;
        }

        private struct Fix
        {
            internal readonly StatementSyntax DisposeStatement;
            internal readonly MethodDeclarationSyntax DisposeMethod;

            public Fix(StatementSyntax disposeStatement, MethodDeclarationSyntax disposeMethod)
            {
                this.DisposeStatement = disposeStatement;
                this.DisposeMethod = disposeMethod;
            }
        }

        private class BacthFixer : FixAllProvider
        {
            public static readonly BacthFixer Default = new BacthFixer();
            private static readonly ImmutableArray<FixAllScope> SupportedFixAllScopes = ImmutableArray.Create(FixAllScope.Document);

            private BacthFixer()
            {
            }

            public override IEnumerable<FixAllScope> GetSupportedFixAllScopes()
            {
                return SupportedFixAllScopes;
            }

            [SuppressMessage("ReSharper", "RedundantCaseLabel", Justification = "Mute R#")]
            public override Task<CodeAction> GetFixAsync(FixAllContext fixAllContext)
            {
                switch (fixAllContext.Scope)
                {
                    case FixAllScope.Document:
                        return Task.FromResult(CodeAction.Create(
                            "Dispose member.",
                            _ => FixDocumentAsync(fixAllContext),
                            this.GetType().Name));
                    case FixAllScope.Project:
                    case FixAllScope.Solution:
                    case FixAllScope.Custom:
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            private static async Task<Document> FixDocumentAsync(FixAllContext context)
            {
                var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                              .ConfigureAwait(false);
                var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken)
                                                 .ConfigureAwait(false);
                var usesUnderscoreNames = syntaxRoot.UsesUnderscore(semanticModel, context.CancellationToken);

                var diagnostics = await context.GetDocumentDiagnosticsAsync(context.Document).ConfigureAwait(false);
                var fixes = new List<Fix>();
                foreach (var diagnostic in diagnostics)
                {
                    var fix = CreateFix(diagnostic, syntaxRoot, semanticModel, context.CancellationToken, usesUnderscoreNames);
                    if (fix.DisposeStatement != null)
                    {
                        fixes.Add(fix);
                    }
                }

                if (fixes.Count == 0)
                {
                    return context.Document;
                }

                if (fixes.Count == 1)
                {
                    return context.Document.WithSyntaxRoot(ApplyFix(syntaxRoot, fixes[0]));
                }

                var tracking = syntaxRoot.TrackNodes(fixes.Select(x => x.DisposeMethod));
                foreach (var fix in fixes)
                {
                    tracking = ApplyFix(tracking, fix);
                }

                return context.Document.WithSyntaxRoot(tracking);
            }
        }
    }
}