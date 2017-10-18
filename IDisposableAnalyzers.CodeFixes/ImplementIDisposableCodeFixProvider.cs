namespace IDisposableAnalyzers
{
    using System.Collections.Immutable;
    using System.Composition;
    using System.Globalization;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

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

                var classDeclaration = syntaxRoot.FindNode(diagnostic.Location.SourceSpan).FirstAncestorOrSelf<ClassDeclarationSyntax>();
                if (classDeclaration == null)
                {
                    continue;
                }

                var type = semanticModel.GetDeclaredSymbolSafe(classDeclaration, context.CancellationToken);

                if (diagnostic.Id == IDISP009IsIDisposable.DiagnosticId)
                {
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            "Add IDisposable interface",
                            cancellationToken =>
                                AddInterfaceAsync(
                                    context,
                                    cancellationToken,
                                    classDeclaration),
                            nameof(ImplementIDisposableCodeFixProvider) + "add interface"),
                        diagnostic);
                    continue;
                }

                if (Disposable.IsAssignableTo(type) &&
                    Disposable.BaseTypeHasVirtualDisposeMethod(type))
                {
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            "override Dispose(bool)",
                            cancellationToken =>
                                OverrideDisposeAsync(
                                    context,
                                    semanticModel,
                                    cancellationToken,
                                    classDeclaration),
                            nameof(ImplementIDisposableCodeFixProvider) + "override"),
                        diagnostic);
                    continue;
                }

                if (type.TryGetMethod("Dispose", out var disposeMethod) &&
                    !disposeMethod.IsStatic &&
                    disposeMethod.ReturnsVoid &&
                    disposeMethod.Parameters.Length == 0)
                {
                    continue;
                }

                if (type.TryGetField("disposed", out _) ||
                    type.TryGetField("_disposed", out _))
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
                                    semanticModel,
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
                                    semanticModel,
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
                                semanticModel,
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
                                semanticModel,
                                cancellationToken,
                                classDeclaration),
                        nameof(ImplementIDisposableCodeFixProvider) + "Virtual"),
                    diagnostic);
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

        private static async Task<Document> AddInterfaceAsync(CodeFixContext context, CancellationToken cancellationToken, ClassDeclarationSyntax classDeclaration)
        {
            var editor = await DocumentEditor.CreateAsync(context.Document, cancellationToken)
                                             .ConfigureAwait(false);
            editor.AddInterfaceType(classDeclaration, IDisposableInterface);
            return editor.GetChangedDocument();
        }

        private static async Task<Document> OverrideDisposeAsync(CodeFixContext context, SemanticModel semanticModel, CancellationToken cancellationToken, ClassDeclarationSyntax classDeclaration)
        {
            var type = (ITypeSymbol)semanticModel.GetDeclaredSymbol(classDeclaration, cancellationToken);
            var usesUnderscoreNames = classDeclaration.UsesUnderscore(semanticModel, cancellationToken);

            var editor = await DocumentEditor.CreateAsync(context.Document, cancellationToken)
                                             .ConfigureAwait(false);
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
                    @"protected override void Dispose(bool disposing)
                      {
                          if (this.disposed)
                          {
                              return;
                          }
              
                          this.disposed = true;
                          if (disposing)
                          {
                          }
              
                          base.Dispose(disposing);
                      }",
                    usesUnderscoreNames,
                    field));

            if (!type.TryGetMethod("ThrowIfDisposed", out _))
            {
                if (type.BaseType.TryGetMethod("ThrowIfDisposed", out var baseThrow) &&
                    baseThrow.Parameters.Length == 0)
                {
                    if (baseThrow.IsVirtual)
                    {
                        editor.AddMethod(
                            classDeclaration,
                            ParseMethod(
                                @"protected override void ThrowIfDisposed()
                                  {
                                      if (this.disposed)
                                      {
                                          throw new System.ObjectDisposedException(this.GetType().FullName);
                                      }
                                 
                                      base.ThrowIfDisposed();
                                  }",
                                usesUnderscoreNames,
                                field));
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

        private static async Task<Document> ImplementIDisposableVirtualAsync(CodeFixContext context, SemanticModel semanticModel, CancellationToken cancellationToken, ClassDeclarationSyntax classDeclaration)
        {
            var type = (ITypeSymbol)semanticModel.GetDeclaredSymbol(classDeclaration, cancellationToken);
            var usesUnderscoreNames = classDeclaration.UsesUnderscore(semanticModel, cancellationToken);
            var editor = await DocumentEditor.CreateAsync(context.Document, cancellationToken)
                                             .ConfigureAwait(false);

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

            if (!type.TryGetMethod("ThrowIfDisposed", out _))
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

            if (classDeclaration.BaseList?.Types.TryGetSingle(x => (x.Type as IdentifierNameSyntax)?.Identifier.ValueText.Contains("IDisposable") == true, out BaseTypeSyntax _) != true)
            {
                editor.AddInterfaceType(classDeclaration, IDisposableInterface);
            }

            return editor.GetChangedDocument();
        }

        private static async Task<Document> ImplementIDisposableSealedAsync(CodeFixContext context, SemanticModel semanticModel, CancellationToken cancellationToken, ClassDeclarationSyntax classDeclaration)
        {
            var type = (ITypeSymbol)semanticModel.GetDeclaredSymbol(classDeclaration, cancellationToken);
            var usesUnderscoreNames = classDeclaration.UsesUnderscore(semanticModel, cancellationToken);
            var editor = await DocumentEditor.CreateAsync(context.Document, cancellationToken)
                                             .ConfigureAwait(false);

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

            if (!type.TryGetMethod("ThrowIfDisposed", out IMethodSymbol _))
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

            if (classDeclaration.BaseList?.Types.TryGetFirst(x => (x.Type as IdentifierNameSyntax)?.Identifier.ValueText.Contains("IDisposable") == true, out BaseTypeSyntax _) != true)
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

        private static MethodDeclarationSyntax ParseMethod(string code, bool usesUnderscoreNames, FieldDeclarationSyntax field = null)
        {
            if (field != null &&
                field.Name() != "disposed")
            {
                code = code.Replace("disposed", field.Name());
            }

            if (usesUnderscoreNames)
            {
                code = code.Replace("this.", string.Empty);
            }

            return (MethodDeclarationSyntax)SyntaxFactory.ParseCompilationUnit(code)
                                                         .Members
                                                         .Single()
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
                if (node.Modifiers.TryGetSingle(x => x.IsKind(SyntaxKind.VirtualKeyword), out SyntaxToken modifier))
                {
                    node = node.WithModifiers(node.Modifiers.Remove(modifier));
                }

                if (node.Modifiers.TryGetSingle(x => x.IsKind(SyntaxKind.ProtectedKeyword), out modifier))
                {
                    node = node.WithModifiers(node.Modifiers.Replace(modifier, SyntaxFactory.Token(SyntaxKind.PrivateKeyword)));
                }

                return base.VisitFieldDeclaration(node);
            }

            public override SyntaxNode VisitEventDeclaration(EventDeclarationSyntax node)
            {
                if (node.Modifiers.TryGetSingle(x => x.IsKind(SyntaxKind.VirtualKeyword), out SyntaxToken modifier))
                {
                    node = node.WithModifiers(node.Modifiers.Remove(modifier));
                }

                if (node.Modifiers.TryGetSingle(x => x.IsKind(SyntaxKind.ProtectedKeyword), out modifier) &&
                    !node.Modifiers.Any(SyntaxKind.OverrideKeyword))
                {
                    node = node.WithModifiers(node.Modifiers.Replace(modifier, SyntaxFactory.Token(SyntaxKind.PrivateKeyword)));
                }

                return base.VisitEventDeclaration(node);
            }

            public override SyntaxNode VisitPropertyDeclaration(PropertyDeclarationSyntax node)
            {
                if (node.Modifiers.TryGetSingle(x => x.IsKind(SyntaxKind.VirtualKeyword), out SyntaxToken modifier))
                {
                    node = node.WithModifiers(node.Modifiers.Remove(modifier));
                }

                if (node.Modifiers.TryGetSingle(x => x.IsKind(SyntaxKind.ProtectedKeyword), out modifier) &&
                    !node.Modifiers.Any(SyntaxKind.OverrideKeyword))
                {
                    node = node.WithModifiers(node.Modifiers.Replace(modifier, SyntaxFactory.Token(SyntaxKind.PrivateKeyword)));
                }

                return base.VisitPropertyDeclaration(node);
            }

            public override SyntaxNode VisitAccessorDeclaration(AccessorDeclarationSyntax node)
            {
                if (node.Modifiers.TryGetSingle(x => x.IsKind(SyntaxKind.VirtualKeyword), out SyntaxToken modifier))
                {
                    node = node.WithModifiers(node.Modifiers.Remove(modifier));
                }

                var parentModifiers = node.FirstAncestor<BasePropertyDeclarationSyntax>()?.Modifiers;
                if (node.Modifiers.TryGetSingle(x => x.IsKind(SyntaxKind.ProtectedKeyword), out modifier) &&
                    parentModifiers?.Any(SyntaxKind.OverrideKeyword) == false)
                {
                    node = node.WithModifiers(node.Modifiers.Replace(modifier, SyntaxFactory.Token(SyntaxKind.PrivateKeyword)));
                }

                if (parentModifiers?.TryGetSingle(x => x.IsKind(SyntaxKind.PrivateKeyword), out modifier) == true)
                {
                    if (node.Modifiers.TryGetSingle(x => x.IsKind(SyntaxKind.PrivateKeyword), out modifier))
                    {
                        node = node.WithModifiers(node.Modifiers.Remove(modifier));
                    }
                }

                return base.VisitAccessorDeclaration(node);
            }

            public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
            {
                if (node.Modifiers.TryGetSingle(x => x.IsKind(SyntaxKind.VirtualKeyword), out SyntaxToken modifier))
                {
                    node = node.WithModifiers(node.Modifiers.Remove(modifier));
                }

                if (node.Modifiers.TryGetSingle(x => x.IsKind(SyntaxKind.ProtectedKeyword), out modifier) &&
                    !node.Modifiers.Any(SyntaxKind.OverrideKeyword))
                {
                    node = node.WithModifiers(node.Modifiers.Replace(modifier, SyntaxFactory.Token(SyntaxKind.PrivateKeyword)));
                }

                return base.VisitMethodDeclaration(node);
            }
        }
    }
}