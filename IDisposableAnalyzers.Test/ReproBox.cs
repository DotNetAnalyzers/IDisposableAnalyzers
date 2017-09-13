namespace IDisposableAnalyzers.Test
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.CodeAnalysis.Diagnostics;

    using NUnit.Framework;

    [Explicit]
    public class ReproBox
    {
        // ReSharper disable once UnusedMember.Local
        private static readonly IReadOnlyList<DiagnosticAnalyzer> AllAnalyzers =
            typeof(KnownSymbol).Assembly.GetTypes()
                               .Where(typeof(DiagnosticAnalyzer).IsAssignableFrom)
                               .Select(t => (DiagnosticAnalyzer)Activator.CreateInstance(t))
                               .ToArray();

        [TestCaseSource(nameof(AllAnalyzers))]
        public async Task Repro(DiagnosticAnalyzer analyzer)
        {
            var testCode = @"
namespace IDisposableAnalyzers
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static partial class Disposable
    {
        internal static Result IsMemberDisposed(ISymbol member, TypeDeclarationSyntax context, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            return IsMemberDisposed(member, semanticModel.GetDeclaredSymbolSafe(context, cancellationToken), semanticModel, cancellationToken);
        }

        internal static Result IsMemberDisposed(ISymbol member, ITypeSymbol context, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (!(member is IFieldSymbol ||
                  member is IPropertySymbol) ||
                  context == null)
            {
                return Result.Unknown;
            }

            using (var pooled = DisposeWalker.Create(context, semanticModel, cancellationToken))
            {
                return pooled.Item.IsMemberDisposed(member);
            }
        }

        internal static bool IsMemberDisposed(ISymbol member, IMethodSymbol disposeMethod, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (member == null ||
                disposeMethod == null)
            {
                return false;
            }

            foreach (var reference in disposeMethod.DeclaringSyntaxReferences)
            {
                var node = reference.GetSyntax(cancellationToken) as MethodDeclarationSyntax;
                using (var pooled = DisposeWalker.Create(disposeMethod, semanticModel, cancellationToken))
                {
                    foreach (var invocation in pooled.Item)
                    {
                        var method = semanticModel.GetSymbolSafe(invocation, cancellationToken) as IMethodSymbol;
                        if (method == null ||
                            method.Parameters.Length != 0 ||
                            method != KnownSymbol.IDisposable.Dispose)
                        {
                            continue;
                        }

                        if (TryGetDisposedRootMember(invocation, semanticModel, cancellationToken, out ExpressionSyntax disposed))
                        {
                            if (SymbolComparer.Equals(member, semanticModel.GetSymbolSafe(disposed, cancellationToken)))
                            {
                                return true;
                            }
                        }
                    }
                }

                using (var pooled = IdentifierNameWalker.Create(node))
                {
                    foreach (var identifier in pooled.Item.IdentifierNames)
                    {
                        var memberAccess = identifier.Parent as MemberAccessExpressionSyntax;
                        if (memberAccess?.Expression is BaseExpressionSyntax)
                        {
                            var baseMethod = semanticModel.GetSymbolSafe(identifier, cancellationToken) as IMethodSymbol;
                            if (baseMethod?.Name == ""Dispose"")
                            {
                                if (IsMemberDisposed(member, baseMethod, semanticModel, cancellationToken))
                                {
                                    return true;
                                }
                            }
                        }

                        if (identifier.Identifier.ValueText != member.Name)
                        {
                            continue;
                        }

                        var symbol = semanticModel.GetSymbolSafe(identifier, cancellationToken);
                        if (member.Equals(symbol) || (member as IPropertySymbol)?.OverriddenProperty?.Equals(symbol) == true)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        internal static bool TryGetDisposedRootMember(InvocationExpressionSyntax disposeCall, SemanticModel semanticModel, CancellationToken cancellationToken, out ExpressionSyntax disposedMember)
        {
            if (MemberPath.TryFindRootMember(disposeCall, out disposedMember))
            {
                var property = semanticModel.GetSymbolSafe(disposedMember, cancellationToken) as IPropertySymbol;
                if (property == null ||
                    property.IsAutoProperty(cancellationToken))
                {
                    return true;
                }

                if (property.GetMethod == null)
                {
                    return false;
                }

                foreach (var reference in property.GetMethod.DeclaringSyntaxReferences)
                {
                    var node = reference.GetSyntax(cancellationToken);
                    using (var pooled = ReturnValueWalker.Create(node, false, semanticModel, cancellationToken))
                    {
                        if (pooled.Item.Count == 0)
                        {
                            return false;
                        }

                        return MemberPath.TryFindRootMember(pooled.Item[0], out disposedMember);
                    }
                }
            }

            return false;
        }

        internal sealed class DisposeWalker : ExecutionWalker, IReadOnlyList<InvocationExpressionSyntax>
        {
            private static readonly Pool<DisposeWalker> Pool = new Pool<DisposeWalker>(
                () => new DisposeWalker(),
                x =>
                    {
                        x.invocations.Clear();
                        x.identifiers.Clear();
                        x.Clear();
                    });

            private readonly List<InvocationExpressionSyntax> invocations = new List<InvocationExpressionSyntax>();
            private readonly List<IdentifierNameSyntax> identifiers = new List<IdentifierNameSyntax>();

            private DisposeWalker()
            {
                this.IsRecursive = true;
            }

            public int Count => this.invocations.Count;

            public InvocationExpressionSyntax this[int index] => this.invocations[index];

            public IEnumerator<InvocationExpressionSyntax> GetEnumerator() => this.invocations.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)this.invocations).GetEnumerator();

            public override void VisitInvocationExpression(InvocationExpressionSyntax node)
            {
                base.VisitInvocationExpression(node);
                var symbol = this.SemanticModel.GetSymbolSafe(node, this.CancellationToken) as IMethodSymbol;
                if (symbol == KnownSymbol.IDisposable.Dispose &&
                    symbol?.Parameters.Length == 0)
                {
                    this.invocations.Add(node);
                }
            }

            public override void VisitIdentifierName(IdentifierNameSyntax node)
            {
                this.identifiers.Add(node);
                base.VisitIdentifierName(node);
            }

            internal static Pool<DisposeWalker>.Pooled Create(ITypeSymbol type, SemanticModel semanticModel, CancellationToken cancellationToken)
            {
                if (!IsAssignableTo(type))
                {
                    return Create(semanticModel, cancellationToken);
                }

                if (TryGetDisposeMethod(type, true, out IMethodSymbol disposeMethod))
                {
                    return Create(disposeMethod, semanticModel, cancellationToken);
                }

                return Create(semanticModel, cancellationToken);
            }

            internal static Pool<DisposeWalker>.Pooled Create(IMethodSymbol disposeMethod, SemanticModel semanticModel, CancellationToken cancellationToken)
            {
                if (disposeMethod != KnownSymbol.IDisposable.Dispose)
                {
                    return Create(semanticModel, cancellationToken);
                }

                var pooled = Create(semanticModel, cancellationToken);
                foreach (var reference in disposeMethod.DeclaringSyntaxReferences)
                {
                    pooled.Item.Visit(reference.GetSyntax(cancellationToken));
                }

                return pooled;
            }

            internal Result IsMemberDisposed(ISymbol member)
            {
                foreach (var invocation in this.invocations)
                {
                    if (TryGetDisposedRootMember(invocation, this.SemanticModel, this.CancellationToken, out ExpressionSyntax disposed) &&
                        SymbolComparer.Equals(member, this.SemanticModel.GetSymbolSafe(disposed, this.CancellationToken)))
                    {
                        return Result.Yes;
                    }
                }

                foreach (var name in this.identifiers)
                {
                    if (SymbolComparer.Equals(member, this.SemanticModel.GetSymbolSafe(name, this.CancellationToken)))
                    {
                        return Result.Maybe;
                    }
                }

                return Result.No;
            }

            private static Pool<DisposeWalker>.Pooled Create(SemanticModel semanticModel, CancellationToken cancellationToken)
            {
                var pooled = Pool.GetOrCreate();
                pooled.Item.SemanticModel = semanticModel;
                pooled.Item.CancellationToken = cancellationToken;
                return pooled;
            }
        }
    }

     using System.Threading;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class SemanticModelExt
    {
        internal static SymbolInfo GetSymbolInfoSafe(this SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
        {
            return semanticModel.SemanticModelFor(node)
                                ?.GetSymbolInfo(node, cancellationToken) ??
                                default(SymbolInfo);
        }

        internal static bool IsEither<T1, T2>(this SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
                where T1 : ISymbol
                where T2 : ISymbol
        {
            return semanticModel.GetSymbolSafe(node, cancellationToken).IsEither<T1, T2>() ||
                   semanticModel.GetDeclaredSymbolSafe(node, cancellationToken).IsEither<T1, T2>() ||
                   semanticModel.GetTypeInfoSafe(node, cancellationToken).Type.IsEither<T1, T2>();
        }

        internal static ISymbol GetSymbolSafe(this SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
        {
            var awaitExpression = node as AwaitExpressionSyntax;
            if (awaitExpression != null)
            {
                return GetSymbolSafe(semanticModel, awaitExpression, cancellationToken);
            }

            return semanticModel.SemanticModelFor(node)
                                ?.GetSymbolInfo(node, cancellationToken)
                                 .Symbol;
        }

        internal static ISymbol GetSymbolSafe(this SemanticModel semanticModel, AwaitExpressionSyntax node, CancellationToken cancellationToken)
        {
            return semanticModel.GetSymbolSafe(node.Expression, cancellationToken);
        }

        internal static IMethodSymbol GetSymbolSafe(this SemanticModel semanticModel, MethodDeclarationSyntax node, CancellationToken cancellationToken)
        {
            return (IMethodSymbol)semanticModel.GetSymbolSafe((SyntaxNode)node, cancellationToken);
        }

        internal static IMethodSymbol GetSymbolSafe(this SemanticModel semanticModel, ConstructorInitializerSyntax node, CancellationToken cancellationToken)
        {
            return (IMethodSymbol)semanticModel.GetSymbolSafe((SyntaxNode)node, cancellationToken);
        }

        internal static IFieldSymbol GetDeclaredSymbolSafe(this SemanticModel semanticModel, FieldDeclarationSyntax node, CancellationToken cancellationToken)
        {
            return (IFieldSymbol)semanticModel.SemanticModelFor(node)
                                              ?.GetDeclaredSymbol(node, cancellationToken);
        }

        internal static IMethodSymbol GetDeclaredSymbolSafe(this SemanticModel semanticModel, ConstructorDeclarationSyntax node, CancellationToken cancellationToken)
        {
            return (IMethodSymbol)semanticModel.SemanticModelFor(node)
                                               ?.GetDeclaredSymbol(node, cancellationToken);
        }

        internal static ISymbol GetDeclaredSymbolSafe(this SemanticModel semanticModel, BasePropertyDeclarationSyntax node, CancellationToken cancellationToken)
        {
            return semanticModel.SemanticModelFor(node)
                                ?.GetDeclaredSymbol(node, cancellationToken);
        }

        internal static IPropertySymbol GetDeclaredSymbolSafe(this SemanticModel semanticModel, PropertyDeclarationSyntax node, CancellationToken cancellationToken)
        {
            return (IPropertySymbol)semanticModel.SemanticModelFor(node)
                                                 ?.GetDeclaredSymbol(node, cancellationToken);
        }

        internal static IPropertySymbol GetDeclaredSymbolSafe(this SemanticModel semanticModel, IndexerDeclarationSyntax node, CancellationToken cancellationToken)
        {
            return (IPropertySymbol)semanticModel.SemanticModelFor(node)
                                                 ?.GetDeclaredSymbol(node, cancellationToken);
        }

        internal static IMethodSymbol GetDeclaredSymbolSafe(this SemanticModel semanticModel, MethodDeclarationSyntax node, CancellationToken cancellationToken)
        {
            return (IMethodSymbol)semanticModel.SemanticModelFor(node)
                                               .GetDeclaredSymbol(node, cancellationToken);
        }

        internal static ITypeSymbol GetDeclaredSymbolSafe(this SemanticModel semanticModel, TypeDeclarationSyntax node, CancellationToken cancellationToken)
        {
            return (ITypeSymbol)semanticModel.SemanticModelFor(node)
                                             ?.GetDeclaredSymbol(node, cancellationToken);
        }

        internal static ISymbol GetDeclaredSymbolSafe(this SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
        {
            return semanticModel.SemanticModelFor(node)
                                ?.GetDeclaredSymbol(node, cancellationToken);
        }

        internal static Optional<object> GetConstantValueSafe(this SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
        {
            return semanticModel.SemanticModelFor(node)
                                ?.GetConstantValue(node, cancellationToken) ?? default(Optional<object>);
        }

        internal static TypeInfo GetTypeInfoSafe(this SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
        {
            return semanticModel.SemanticModelFor(node)
                                ?.GetTypeInfo(node, cancellationToken) ?? default(TypeInfo);
        }

        /// <summary>
        /// Gets the semantic model for <paramref name=""expression""/>
        /// This can be needed for partial classes.
        /// </summary>
        /// <param name=""semanticModel"">The semantic model.</param>
        /// <param name=""expression"">The expression.</param>
        /// <returns>The semantic model that corresponds to <paramref name=""expression""/></returns>
        internal static SemanticModel SemanticModelFor(this SemanticModel semanticModel, SyntaxNode expression)
        {
            if (semanticModel == null ||
                expression == null ||
                expression.IsMissing)
            {
                return null;
            }

            if (ReferenceEquals(semanticModel.SyntaxTree, expression.SyntaxTree))
            {
                return semanticModel;
            }

            if (semanticModel.Compilation.ContainsSyntaxTree(expression.SyntaxTree))
            {
                return semanticModel.Compilation.GetSemanticModel(expression.SyntaxTree);
            }

            foreach (var metadataReference in semanticModel.Compilation.References)
            {
                var compilationReference = metadataReference as CompilationReference;
                if (compilationReference != null)
                {
                    if (compilationReference.Compilation.ContainsSyntaxTree(expression.SyntaxTree))
                    {
                        return compilationReference.Compilation.GetSemanticModel(expression.SyntaxTree);
                    }
                }
            }

            return null;
        }
    }
    }";

            Console.WriteLine(analyzer);
            var analyzers = ImmutableArray.Create(analyzer);
            await DiagnosticVerifier.GetSortedDiagnosticsFromDocumentsAsync(
                          analyzers,
                          CodeFactory.GetDocuments(
                              new[] { testCode },
                              analyzers,
                              Enumerable.Empty<string>()),
                          CancellationToken.None)
                      .ConfigureAwait(false);
        }
    }
}