namespace IDisposableAnalyzers
{
    using System.Collections.Immutable;
    using System.Composition;
    using System.Globalization;
    using System.Threading;
    using System.Threading.Tasks;
    using Gu.Roslyn.AnalyzerExtensions;
    using Gu.Roslyn.CodeFixExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Editing;
    using Microsoft.CodeAnalysis.Formatting;
    using Microsoft.CodeAnalysis.Simplification;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ImplementIDisposableCodeFixProvider))]
    [Shared]
    internal class ImplementIDisposableCodeFixProvider : CodeFixProvider
    {
        // ReSharper disable once InconsistentNaming
        private static readonly TypeSyntax IDisposableInterface = SyntaxFactory.ParseTypeName("System.IDisposable")
                                                                               .WithTrailingTrivia(SyntaxFactory.ElasticMarker)
                                                                               .WithAdditionalAnnotations(Simplifier.Annotation, SyntaxAnnotation.ElasticAnnotation);

        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(
            IDISP006ImplementIDisposable.DiagnosticId,
            IDISP009IsIDisposable.DiagnosticId,
            "CS0535");

        public override FixAllProvider GetFixAllProvider() => null;

        /// <inheritdoc/>
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                          .ConfigureAwait(false);
            if (syntaxRoot == null)
            {
                return;
            }

            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken)
                                             .ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                if (!IsSupportedDiagnostic(diagnostic))
                {
                    continue;
                }

                var token = syntaxRoot.FindToken(diagnostic.Location.SourceSpan.Start);
                if (string.IsNullOrEmpty(token.ValueText) ||
                    token.IsMissing)
                {
                    continue;
                }

                var typeDeclaration = syntaxRoot.FindNode(diagnostic.Location.SourceSpan)
                                                .FirstAncestorOrSelf<TypeDeclarationSyntax>();
                if (diagnostic.Id == IDISP009IsIDisposable.DiagnosticId)
                {
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            "Add IDisposable interface",
                            cancellationToken =>
                                AddInterfaceAsync(
                                    context,
                                    cancellationToken,
                                    typeDeclaration),
                            nameof(ImplementIDisposableCodeFixProvider) + "add interface"),
                        diagnostic);
                    continue;
                }

                if (typeDeclaration is StructDeclarationSyntax structDeclaration)
                {
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            "Implement IDisposable.",
                            cancellationToken =>
                                ImplementIDisposableStructAsync(
                                    context,
                                    cancellationToken,
                                    structDeclaration),
                            nameof(ImplementIDisposableCodeFixProvider) + "Struct"),
                        diagnostic);
                }
                else if (typeDeclaration is ClassDeclarationSyntax classDeclaration)
                {
                    var type = semanticModel.GetDeclaredSymbolSafe(typeDeclaration, context.CancellationToken);
                    if (Disposable.IsAssignableFrom(type, semanticModel.Compilation) &&
                        DisposeMethod.TryFindBaseVirtual(type, out var baseDispose))
                    {
                        context.RegisterCodeFix(
                            CodeAction.Create(
                                "override Dispose(bool)",
                                cancellationToken =>
                                    OverrideDisposeAsync(
                                        context,
                                        classDeclaration,
                                        baseDispose,
                                        cancellationToken),
                                nameof(ImplementIDisposableCodeFixProvider) + "override"),
                            diagnostic);
                        continue;
                    }

                    if (type.TryFindSingleMethodRecursive("Dispose", out var disposeMethod) &&
                        !disposeMethod.IsStatic &&
                        disposeMethod.ReturnsVoid &&
                        disposeMethod.Parameters.Length == 0)
                    {
                        continue;
                    }

                    if (type.TryFindFieldRecursive("disposed", out _) ||
                        type.TryFindFieldRecursive("_disposed", out _))
                    {
                        return;
                    }

                    if (type.IsSealed)
                    {
                        context.RegisterCodeFix(
                            CodeAction.Create(
                                "Implement IDisposable.",
                                cancellationToken =>
                                    ImplementIDisposableSealedAsync(
                                        context,
                                        cancellationToken,
                                        classDeclaration),
                                nameof(ImplementIDisposableCodeFixProvider) + "Sealed"),
                            diagnostic);
                        continue;
                    }

                    if (type.IsAbstract)
                    {
                        context.RegisterCodeFix(
                            CodeAction.Create(
                                "Implement IDisposable with virtual dispose method.",
                                cancellationToken =>
                                    ImplementIDisposableVirtualAsync(
                                        context,
                                        cancellationToken,
                                        classDeclaration),
                                nameof(ImplementIDisposableCodeFixProvider) + "Virtual"),
                            diagnostic);
                        continue;
                    }

                    context.RegisterCodeFix(
                        CodeAction.Create(
                            "Implement IDisposable and make class sealed.",
                            cancellationToken =>
                                ImplementIDisposableSealedAsync(
                                    context,
                                    cancellationToken,
                                    classDeclaration),
                            nameof(ImplementIDisposableCodeFixProvider) + "Sealed"),
                        diagnostic);

                    context.RegisterCodeFix(
                        CodeAction.Create(
                            "Implement IDisposable with virtual dispose method.",
                            cancellationToken =>
                                ImplementIDisposableVirtualAsync(
                                    context,
                                    cancellationToken,
                                    classDeclaration),
                            nameof(ImplementIDisposableCodeFixProvider) + "Virtual"),
                        diagnostic);
                }
            }
        }

        private static bool IsSupportedDiagnostic(Diagnostic diagnostic)
        {
            if (diagnostic.Id == IDISP006ImplementIDisposable.DiagnosticId ||
                diagnostic.Id == IDISP009IsIDisposable.DiagnosticId)
            {
                return true;
            }

            if (diagnostic.Id == "CS0535")
            {
                return diagnostic.GetMessage(CultureInfo.InvariantCulture)
                                 .EndsWith("does not implement interface member 'IDisposable.Dispose()'");
            }

            return false;
        }

        private static async Task<Document> AddInterfaceAsync(CodeFixContext context, CancellationToken cancellationToken, TypeDeclarationSyntax typeDeclaration)
        {
            var editor = await DocumentEditor.CreateAsync(context.Document, cancellationToken)
                                             .ConfigureAwait(false);
            editor.AddInterfaceType(typeDeclaration, IDisposableInterface);
            return editor.GetChangedDocument();
        }

        private static async Task<Document> OverrideDisposeAsync(CodeFixContext context, ClassDeclarationSyntax classDeclaration, IMethodSymbol baseDispose, CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(context.Document, cancellationToken)
                                             .ConfigureAwait(false);
            var type = (ITypeSymbol)editor.SemanticModel.GetDeclaredSymbol(classDeclaration, cancellationToken);
            var usesUnderscoreNames = editor.SemanticModel.UnderscoreFields();
            var field = editor.AddField(
                classDeclaration,
                usesUnderscoreNames
                    ? "_disposed"
                    : "disposed",
                Accessibility.Private,
                DeclarationModifiers.None,
                SyntaxFactory.ParseTypeName("bool"),
                cancellationToken);

            var code = StringBuilderPool.Borrow()
                                        .AppendLine($"{baseDispose.DeclaredAccessibility.ToCodeString()} override void Dispose(bool disposing)")
                                        .AppendLine("{")
                                        .AppendLine("    if (this.disposed)")
                                        .AppendLine("    {")
                                        .AppendLine("        return;")
                                        .AppendLine("    }")
                                        .AppendLine()
                                        .AppendLine("     this.disposed = true;")
                                        .AppendLine("     if (disposing)")
                                        .AppendLine("     {")
                                        .AppendLine("     }")
                                        .AppendLine()
                                        .AppendLine("     base.Dispose(disposing);")
                                        .AppendLine("}")
                                        .Return();
            editor.AddMethod(
                classDeclaration,
                ParseMethod(code, usesUnderscoreNames, field));

            if (!type.GetMembers().TryFirst(x => x.Name == "ThrowIfDisposed", out _))
            {
                if (type.BaseType.TryFindSingleMethodRecursive("ThrowIfDisposed", out var baseThrow) &&
                    baseThrow.Parameters.Length == 0)
                {
                    if (baseThrow.IsVirtual)
                    {
                        code = StringBuilderPool.Borrow()
                                                .AppendLine($"{baseThrow.DeclaredAccessibility.ToCodeString()} override void ThrowIfDisposed()")
                                                .AppendLine("{")
                                                .AppendLine("    if (this.disposed)")
                                                .AppendLine("    {")
                                                .AppendLine("        throw new System.ObjectDisposedException(this.GetType().FullName);")
                                                .AppendLine("    }")
                                                .AppendLine()
                                                .AppendLine("     base.ThrowIfDisposed();")
                                                .AppendLine("}")
                                                .Return();
                        editor.AddMethod(
                            classDeclaration,
                            ParseMethod(code, usesUnderscoreNames, field));
                    }
                }
                else
                {
                    editor.AddMethod(
                        classDeclaration,
                        ParseMethod(
                            @"protected virtual void ThrowIfDisposed()
                            {
                                if (this.disposed)
                                {
                                    throw new System.ObjectDisposedException(this.GetType().FullName);
                                }
                            }",
                            usesUnderscoreNames,
                            field));
                }
            }

            return editor.GetChangedDocument();
        }

        private static async Task<Document> ImplementIDisposableVirtualAsync(CodeFixContext context, CancellationToken cancellationToken, ClassDeclarationSyntax classDeclaration)
        {
            var editor = await DocumentEditor.CreateAsync(context.Document, cancellationToken)
                                             .ConfigureAwait(false);
            var type = (ITypeSymbol)editor.SemanticModel.GetDeclaredSymbol(classDeclaration, cancellationToken);
            var usesUnderscoreNames = editor.SemanticModel.UnderscoreFields();
            var field = editor.AddField(
                classDeclaration,
                usesUnderscoreNames
                    ? "_disposed"
                    : "disposed",
                Accessibility.Private,
                DeclarationModifiers.None,
                SyntaxFactory.ParseTypeName("bool"),
                cancellationToken);

            editor.AddMethod(
                classDeclaration,
                ParseMethod(
                    @"public void Dispose()
                          {
                              this.Dispose(true);
                          }",
                    usesUnderscoreNames));

            editor.AddMethod(
                classDeclaration,
                ParseMethod(
                    @"protected virtual void Dispose(bool disposing)
                      {
                          if (this.disposed)
                          {
                              return;
                          }
                     
                          this.disposed = true;
                          if (disposing)
                          {
                          }
                      }",
                    usesUnderscoreNames,
                    field));

            if (!type.TryFindSingleMethodRecursive("ThrowIfDisposed", out _))
            {
                editor.AddMethod(
                    classDeclaration,
                    ParseMethod(
                        @"protected virtual void ThrowIfDisposed()
                          {
                              if (this.disposed)
                              {
                                  throw new System.ObjectDisposedException(this.GetType().FullName);
                              }
                          }",
                        usesUnderscoreNames,
                        field));
            }

            if (classDeclaration.BaseList?.Types.TrySingle(x => (x.Type as IdentifierNameSyntax)?.Identifier.ValueText.Contains("IDisposable") == true, out BaseTypeSyntax _) != true)
            {
                editor.AddInterfaceType(classDeclaration, IDisposableInterface);
            }

            return editor.GetChangedDocument();
        }

        private static async Task<Document> ImplementIDisposableSealedAsync(CodeFixContext context, CancellationToken cancellationToken, ClassDeclarationSyntax classDeclaration)
        {
            var editor = await DocumentEditor.CreateAsync(context.Document, cancellationToken)
                                             .ConfigureAwait(false);
            var type = (ITypeSymbol)editor.SemanticModel.GetDeclaredSymbol(classDeclaration, cancellationToken);
            var usesUnderscoreNames = editor.SemanticModel.UnderscoreFields();
            var field = editor.AddField(
                classDeclaration,
                usesUnderscoreNames
                    ? "_disposed"
                    : "disposed",
                Accessibility.Private,
                DeclarationModifiers.None,
                SyntaxFactory.ParseTypeName("bool"),
                cancellationToken);

            editor.AddMethod(
                classDeclaration,
                ParseMethod(
                    @"public void Dispose()
                          {
                              if (this.disposed)
                              {
                                  return;
                              }

                              this.disposed = true;
                          }",
                    usesUnderscoreNames,
                    field));

            if (!type.TryFindSingleMethodRecursive("ThrowIfDisposed", out IMethodSymbol _))
            {
                editor.AddMethod(
                    classDeclaration,
                    ParseMethod(
                        @"private void ThrowIfDisposed()
                          {
                              if (this.disposed)
                              {
                                  throw new System.ObjectDisposedException(this.GetType().FullName);
                              }
                          }",
                        usesUnderscoreNames,
                        field));
            }

            if (classDeclaration.BaseList?.Types.TryFirst(x => (x.Type as IdentifierNameSyntax)?.Identifier.ValueText.Contains("IDisposable") == true, out BaseTypeSyntax _) != true)
            {
                editor.AddInterfaceType(classDeclaration, IDisposableInterface);
            }

            var updated = editor.GetChangedDocument();
            if (type.IsSealed)
            {
                return updated;
            }

            editor.TrackNode(classDeclaration);
            var updatedRoot = await updated.GetSyntaxRootAsync(context.CancellationToken)
                                           .ConfigureAwait(false);
            var updatedClassDeclaration = updatedRoot.GetCurrentNode(classDeclaration);
            return updated.WithSyntaxRoot(MakeSealedRewriter.Default.Visit(updatedRoot, updatedClassDeclaration));
        }

        private static async Task<Document> ImplementIDisposableStructAsync(CodeFixContext context, CancellationToken cancellationToken, StructDeclarationSyntax structDeclaration)
        {
            var editor = await DocumentEditor.CreateAsync(context.Document, cancellationToken)
                                             .ConfigureAwait(false);

            editor.AddMethod(
                structDeclaration,
                Parse.MethodDeclaration(@"public void Dispose()
                          {
                          }")
                     .WithSimplifiedNames()
                     .WithLeadingTrivia(SyntaxFactory.ElasticMarker)
                     .WithTrailingTrivia(SyntaxFactory.ElasticMarker)
                     .WithAdditionalAnnotations(Formatter.Annotation));

            return editor.GetChangedDocument();
        }

        private static MethodDeclarationSyntax ParseMethod(string code, bool usesUnderscoreNames, FieldDeclarationSyntax field = null)
        {
            if (field.TryGetName(out var name) &&
                name != "disposed")
            {
                code = code.Replace("disposed", name);
            }

            if (usesUnderscoreNames)
            {
                code = code.Replace("this.", string.Empty);
            }

            return Parse.MethodDeclaration(code)
                        .WithSimplifiedNames()
                        .WithLeadingTrivia(SyntaxFactory.ElasticMarker)
                        .WithTrailingTrivia(SyntaxFactory.ElasticMarker)
                        .WithAdditionalAnnotations(Formatter.Annotation);
        }

        private class MakeSealedRewriter : CSharpSyntaxRewriter
        {
            public static readonly MakeSealedRewriter Default = new MakeSealedRewriter();

            private static readonly ThreadLocal<ClassDeclarationSyntax> CurrentClass = new ThreadLocal<ClassDeclarationSyntax>();

            public SyntaxNode Visit(SyntaxNode node, ClassDeclarationSyntax classDeclaration)
            {
                CurrentClass.Value = classDeclaration;
                var updated = this.Visit(node);
                CurrentClass.Value = null;
                return updated;
            }

            public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
            {
                // We only want to make the top level class sealed.
                if (ReferenceEquals(CurrentClass.Value, node))
                {
                    var updated = (ClassDeclarationSyntax)base.VisitClassDeclaration(node);
                    return updated.WithModifiers(updated.Modifiers.Add(SyntaxFactory.Token(SyntaxKind.SealedKeyword)));
                }

                return node;
            }

            public override SyntaxNode VisitFieldDeclaration(FieldDeclarationSyntax node)
            {
                if (node.Modifiers.TrySingle(x => x.IsKind(SyntaxKind.VirtualKeyword), out SyntaxToken modifier))
                {
                    node = node.WithModifiers(node.Modifiers.Remove(modifier));
                }

                if (node.Modifiers.TrySingle(x => x.IsKind(SyntaxKind.ProtectedKeyword), out modifier))
                {
                    node = node.WithModifiers(node.Modifiers.Replace(modifier, SyntaxFactory.Token(SyntaxKind.PrivateKeyword)));
                }

                return base.VisitFieldDeclaration(node);
            }

            public override SyntaxNode VisitEventDeclaration(EventDeclarationSyntax node)
            {
                if (node.Modifiers.TrySingle(x => x.IsKind(SyntaxKind.VirtualKeyword), out SyntaxToken modifier))
                {
                    node = node.WithModifiers(node.Modifiers.Remove(modifier));
                }

                if (node.Modifiers.TrySingle(x => x.IsKind(SyntaxKind.ProtectedKeyword), out modifier) &&
                    !node.Modifiers.Any(SyntaxKind.OverrideKeyword))
                {
                    node = node.WithModifiers(node.Modifiers.Replace(modifier, SyntaxFactory.Token(SyntaxKind.PrivateKeyword)));
                }

                return base.VisitEventDeclaration(node);
            }

            public override SyntaxNode VisitPropertyDeclaration(PropertyDeclarationSyntax node)
            {
                if (node.Modifiers.TrySingle(x => x.IsKind(SyntaxKind.VirtualKeyword), out SyntaxToken modifier))
                {
                    node = node.WithModifiers(node.Modifiers.Remove(modifier));
                }

                if (node.Modifiers.TrySingle(x => x.IsKind(SyntaxKind.ProtectedKeyword), out modifier) &&
                    !node.Modifiers.Any(SyntaxKind.OverrideKeyword))
                {
                    node = node.WithModifiers(node.Modifiers.Replace(modifier, SyntaxFactory.Token(SyntaxKind.PrivateKeyword)));
                }

                return base.VisitPropertyDeclaration(node);
            }

            public override SyntaxNode VisitAccessorDeclaration(AccessorDeclarationSyntax node)
            {
                if (node.Modifiers.TrySingle(x => x.IsKind(SyntaxKind.VirtualKeyword), out SyntaxToken modifier))
                {
                    node = node.WithModifiers(node.Modifiers.Remove(modifier));
                }

                var parentModifiers = node.FirstAncestor<BasePropertyDeclarationSyntax>()?.Modifiers;
                if (node.Modifiers.TrySingle(x => x.IsKind(SyntaxKind.ProtectedKeyword), out modifier) &&
                    parentModifiers?.Any(SyntaxKind.OverrideKeyword) == false)
                {
                    node = node.WithModifiers(node.Modifiers.Replace(modifier, SyntaxFactory.Token(SyntaxKind.PrivateKeyword)));
                }

                if (parentModifiers?.TrySingle(x => x.IsKind(SyntaxKind.PrivateKeyword), out modifier) == true)
                {
                    if (node.Modifiers.TrySingle(x => x.IsKind(SyntaxKind.PrivateKeyword), out modifier))
                    {
                        node = node.WithModifiers(node.Modifiers.Remove(modifier));
                    }
                }

                return base.VisitAccessorDeclaration(node);
            }

            public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
            {
                if (node.Modifiers.TrySingle(x => x.IsKind(SyntaxKind.VirtualKeyword), out SyntaxToken modifier))
                {
                    node = node.WithModifiers(node.Modifiers.Remove(modifier));
                }

                if (node.Modifiers.TrySingle(x => x.IsKind(SyntaxKind.ProtectedKeyword), out modifier) &&
                    !node.Modifiers.Any(SyntaxKind.OverrideKeyword))
                {
                    node = node.WithModifiers(node.Modifiers.Replace(modifier, SyntaxFactory.Token(SyntaxKind.PrivateKeyword)));
                }

                return base.VisitMethodDeclaration(node);
            }
        }
    }
}
