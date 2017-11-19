namespace IDisposableAnalyzers
{
    using System;
    using System.Collections.Immutable;
    using System.Composition;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Editing;
    using Microsoft.CodeAnalysis.Formatting;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DisposeInTearDownCodeFixProvider))]
    [Shared]
    internal class DisposeInTearDownCodeFixProvider : CodeFixProvider
    {
        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(
            IDISP002DisposeMember.DiagnosticId,
            IDISP003DisposeBeforeReassigning.DiagnosticId);

        /// <inheritdoc/>
        public override FixAllProvider GetFixAllProvider() => DocumentEditorFixAllProvider.Default;

        /// <inheritdoc/>
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                          .ConfigureAwait(false);

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

                var member = (MemberDeclarationSyntax)syntaxRoot.FindNode(diagnostic.Location.SourceSpan);
                if (TryGetMemberSymbol(member, semanticModel, context.CancellationToken, out var memberSymbol))
                {
                    if (TestFixture.IsAssignedInSetUp(memberSymbol, member.FirstAncestor<ClassDeclarationSyntax>(), semanticModel, context.CancellationToken, out var setupAttribute))
                    {
                        if (TestFixture.TryGetTearDownMethod(setupAttribute, semanticModel, context.CancellationToken, out var tearDownMethodDeclaration))
                        {
                            context.RegisterDocumentEditorFix(
                                $"Dispose member in {tearDownMethodDeclaration.Identifier.ValueText}.",
                                (editor, cancellationToken) => DisposeInTearDownMethod(editor, memberSymbol, tearDownMethodDeclaration, cancellationToken),
                                diagnostic);
                        }
                        else if (setupAttribute.FirstAncestor<MethodDeclarationSyntax>() is MethodDeclarationSyntax setupMethod)
                        {
                            var tearDownType = semanticModel.GetTypeInfoSafe(setupAttribute, context.CancellationToken).Type == KnownSymbol.NUnitSetUpAttribute
                                ? KnownSymbol.NUnitTearDownAttribute
                                : KnownSymbol.NUnitOneTimeTearDownAttribute;

                            context.RegisterDocumentEditorFix(
                                $"Create {tearDownType.Type} method and dispose member.",
                                (editor, cancellationToken) => CreateTearDownMethod(editor, memberSymbol, setupMethod, tearDownType, cancellationToken),
                                diagnostic);
                        }
                    }
                }
            }
        }

        private static void DisposeInTearDownMethod(DocumentEditor editor, ISymbol memberSymbol, MethodDeclarationSyntax disposeMethod, CancellationToken cancellationToken)
        {
            var usesUnderscoreNames = editor.OriginalRoot.UsesUnderscore(editor.SemanticModel, cancellationToken);
            var disposeStatement = CreateDisposeStatement(memberSymbol, editor.SemanticModel, cancellationToken, usesUnderscoreNames);
            var statements = CreateStatements(disposeMethod, disposeStatement);
            if (disposeMethod.Body != null)
            {
                var updatedBody = disposeMethod.Body.WithStatements(statements);
                editor.ReplaceNode(disposeMethod.Body, updatedBody);
            }
            else if (disposeMethod.ExpressionBody != null)
            {
                var newMethod = disposeMethod.WithBody(SyntaxFactory.Block(statements))
                                             .WithExpressionBody(null)
                                             .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.None));
                editor.ReplaceNode(disposeMethod, newMethod);
            }
        }

        private static void CreateTearDownMethod(DocumentEditor editor, ISymbol memberSymbol, MethodDeclarationSyntax setupMethod, QualifiedType tearDownType, CancellationToken cancellationToken)
        {
            var usesUnderscoreNames = editor.OriginalRoot.UsesUnderscore(editor.SemanticModel, cancellationToken);
            var code = StringBuilderPool.Borrow()
                                        .AppendLine($"[{tearDownType.FullName}]")
                                        .AppendLine($"public void {tearDownType.Type.Replace("Attribute", string.Empty)}()")
                                        .AppendLine("{")
                                        .AppendLine($"    {(usesUnderscoreNames ? string.Empty : "this.")}{memberSymbol.Name}.Dispose();")
                                        .AppendLine("}")
                                        .Return();
            var tearDownMethod = (MethodDeclarationSyntax)SyntaxFactory.ParseCompilationUnit(code)
                                                                       .Members
                                                                       .Single()
                                                                       .WithSimplifiedNames()
                                                                       .WithLeadingTrivia(SyntaxFactory.ElasticMarker)
                                                                       .WithTrailingTrivia(SyntaxFactory.ElasticMarker)
                                                                       .WithAdditionalAnnotations(Formatter.Annotation);
            editor.InsertAfter(setupMethod, tearDownMethod);
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

            using (var assignedValues = AssignedValueWalker.Borrow(member, semanticModel, cancellationToken))
            {
                foreach (var value in assignedValues)
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

        private static bool TryGetIfDisposing(MethodDeclarationSyntax disposeMethod, out IfStatementSyntax result)
        {
            foreach (var statement in disposeMethod.Body.Statements)
            {
                var ifStatement = statement as IfStatementSyntax;
                if (ifStatement == null)
                {
                    continue;
                }

                if ((ifStatement.Condition as IdentifierNameSyntax)?.Identifier.ValueText == "disposing")
                {
                    result = ifStatement;
                    return true;
                }
            }

            result = null;
            return false;
        }

        private static bool TryGetMemberSymbol(MemberDeclarationSyntax member, SemanticModel semanticModel, CancellationToken cancellationToken, out ISymbol symbol)
        {
            if (member is FieldDeclarationSyntax field &&
                field.Declaration.Variables.TryGetSingle(out var declarator))
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
    }
}