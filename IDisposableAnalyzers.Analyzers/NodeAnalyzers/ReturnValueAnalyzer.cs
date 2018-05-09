namespace IDisposableAnalyzers
{
    using System.Collections.Immutable;
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class ReturnValueAnalyzer : DiagnosticAnalyzer
    {
#pragma warning disable GU0001 // Name the arguments.
        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
            IDISP005ReturntypeShouldIndicateIDisposable.Descriptor,
            IDISP011DontReturnDisposed.Descriptor,
            IDISP012PropertyShouldNotReturnCreated.Descriptor,
            IDISP013AwaitInUsing.Descriptor);
#pragma warning restore GU0001 // Name the arguments.

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(HandleReturnValue, SyntaxKind.ReturnStatement);
            context.RegisterSyntaxNodeAction(HandleArrow, SyntaxKind.ArrowExpressionClause);
            context.RegisterSyntaxNodeAction(HandleLambda, SyntaxKind.ParenthesizedLambdaExpression);
            context.RegisterSyntaxNodeAction(HandleLambda, SyntaxKind.SimpleLambdaExpression);
        }

        private static void HandleReturnValue(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            if (IsIgnored(context.ContainingSymbol))
            {
                return;
            }

            if (context.Node is ReturnStatementSyntax returnStatement &&
                returnStatement.Expression is ExpressionSyntax expression)
            {
                HandleReturnValue(context, expression);
            }
        }

        private static void HandleArrow(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            if (IsIgnored(context.ContainingSymbol))
            {
                return;
            }

            if (context.Node is ArrowExpressionClauseSyntax arrowExpressionClause &&
                arrowExpressionClause.Expression is ExpressionSyntax expression)
            {
                HandleReturnValue(context, expression);
            }
        }

        private static void HandleLambda(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            if (IsIgnored(context.ContainingSymbol))
            {
                return;
            }

            if (context.Node is LambdaExpressionSyntax lambda &&
                lambda.Body is ExpressionSyntax expression)
            {
                HandleReturnValue(context, expression);
            }
        }

        private static void HandleReturnValue(SyntaxNodeAnalysisContext context, ExpressionSyntax returnValue)
        {
            if (Disposable.IsCreation(returnValue, context.SemanticModel, context.CancellationToken) == Result.Yes &&
                context.SemanticModel.TryGetSymbol(returnValue, context.CancellationToken, out ISymbol returnedSymbol))
            {
                if (IsInUsing(returnedSymbol, context.CancellationToken) ||
                    Disposable.IsDisposedBefore(returnedSymbol, returnValue, context.SemanticModel, context.CancellationToken))
                {
                    context.ReportDiagnostic(Diagnostic.Create(IDISP011DontReturnDisposed.Descriptor, returnValue.GetLocation()));
                }
                else
                {
                    if (returnValue.FirstAncestor<AccessorDeclarationSyntax>() is AccessorDeclarationSyntax accessor &&
                        accessor.IsKind(SyntaxKind.GetAccessorDeclaration))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(IDISP012PropertyShouldNotReturnCreated.Descriptor, returnValue.GetLocation()));
                    }

                    if (returnValue.FirstAncestor<ArrowExpressionClauseSyntax>() is ArrowExpressionClauseSyntax arrow &&
                        arrow.Parent is PropertyDeclarationSyntax)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(IDISP012PropertyShouldNotReturnCreated.Descriptor, returnValue.GetLocation()));
                    }

                    if (!IsDisposableReturnTypeOrIgnored(ReturnType(context), context.Compilation))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(IDISP005ReturntypeShouldIndicateIDisposable.Descriptor, returnValue.GetLocation()));
                    }
                }
            }
            else if (returnValue is InvocationExpressionSyntax invocation &&
                     invocation.ArgumentList is ArgumentListSyntax argumentList)
            {
                foreach (var argument in argumentList.Arguments)
                {
                    if (Disposable.IsCreation(argument.Expression, context.SemanticModel, context.CancellationToken).IsEither(Result.Yes, Result.AssumeYes) &&
                        context.SemanticModel.TryGetSymbol(argument.Expression, context.CancellationToken, out ISymbol argumentSymbol))
                    {
                        if (IsInUsing(argumentSymbol, context.CancellationToken) ||
                            Disposable.IsDisposedBefore(argumentSymbol, argument.Expression, context.SemanticModel, context.CancellationToken))
                        {
                            if (IsLazyEnumerable(invocation, context.SemanticModel, context.CancellationToken))
                            {
                                context.ReportDiagnostic(Diagnostic.Create(IDISP011DontReturnDisposed.Descriptor, argument.GetLocation()));
                            }
                        }
                    }
                }
            }

            if (ReturnType(context).IsAssignableTo(KnownSymbol.Task, context.SemanticModel.Compilation) &&
                returnValue.TryFirstAncestor<UsingStatementSyntax>(out var usingStatement) &&
                usingStatement.Statement.Contains(returnValue) &&
                !returnValue.TryFirstAncestorOrSelf<AwaitExpressionSyntax>(out _) &&
                returnValue.IsAssignableTo(KnownSymbol.Task, context.SemanticModel) &&
                ShouldAwait(context, returnValue))
            {
                context.ReportDiagnostic(Diagnostic.Create(IDISP013AwaitInUsing.Descriptor, returnValue.GetLocation()));
            }
        }

        private static bool ShouldAwait(SyntaxNodeAnalysisContext context, ExpressionSyntax returnValue)
        {
            switch (returnValue)
            {
                case InvocationExpressionSyntax invocation when invocation.TryGetMethodName(out var name) &&
                                                                name == KnownSymbol.Task.FromResult.Name:
                    return context.SemanticModel.GetSymbolSafe(returnValue, context.CancellationToken) != KnownSymbol.Task.FromResult;
                case MemberAccessExpressionSyntax memberAccess when memberAccess.Name.Identifier.ValueText == "CompletedTask":
                    return context.SemanticModel.GetSymbolSafe(returnValue, context.CancellationToken) != KnownSymbol.Task.CompletedTask;
            }

            return true;
        }

        private static bool IsInUsing(ISymbol symbol, CancellationToken cancellationToken)
        {
            return symbol.TrySingleDeclaration<SyntaxNode>(cancellationToken, out var declaration) &&
                   declaration.Parent?.Parent is UsingStatementSyntax;
        }

        private static bool IsLazyEnumerable(InvocationExpressionSyntax invocation, SemanticModel semanticModel, CancellationToken cancellationToken, PooledSet<SyntaxNode> visited = null)
        {
            if (semanticModel.GetSymbolSafe(invocation, cancellationToken) is IMethodSymbol method &&
                method.ReturnType.IsAssignableTo(KnownSymbol.IEnumerable, semanticModel.Compilation) &&
                method.TrySingleDeclaration(cancellationToken, out MethodDeclarationSyntax methodDeclaration))
            {
                if (YieldStatementWalker.Any(methodDeclaration))
                {
                    return true;
                }

                using (var walker = ReturnValueWalker.Borrow(methodDeclaration, ReturnValueSearch.TopLevel, semanticModel, cancellationToken))
                {
                    using (visited = visited.IncrementUsage())
                    {
                        foreach (var returnValue in walker)
                        {
                            if (returnValue is InvocationExpressionSyntax nestedInvocation &&
                                visited.Add(returnValue) &&
                                IsLazyEnumerable(nestedInvocation, semanticModel, cancellationToken, visited))
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        private static bool IsDisposableReturnTypeOrIgnored(ITypeSymbol type, Compilation compilation)
        {
            if (type == null ||
                type == KnownSymbol.Void)
            {
                return true;
            }

            if (Disposable.IsAssignableFrom(type, compilation))
            {
                return true;
            }

            if (type == KnownSymbol.IEnumerator)
            {
                return true;
            }

            if (type == KnownSymbol.Task)
            {
                var namedType = type as INamedTypeSymbol;
                return namedType?.IsGenericType == true &&
                       Disposable.IsAssignableFrom(namedType.TypeArguments[0], compilation);
            }

            if (type == KnownSymbol.Func)
            {
                var namedType = type as INamedTypeSymbol;
                return namedType?.IsGenericType == true &&
                       Disposable.IsAssignableFrom(namedType.TypeArguments[namedType.TypeArguments.Length - 1], compilation);
            }

            return false;
        }

        private static bool IsIgnored(ISymbol symbol)
        {
            if (symbol is IMethodSymbol method)
            {
                return method == KnownSymbol.IEnumerable.GetEnumerator;
            }

            return false;
        }

        private static ITypeSymbol ReturnType(SyntaxNodeAnalysisContext context)
        {
            var anonymousFunction = context.Node.FirstAncestorOrSelf<AnonymousFunctionExpressionSyntax>();
            if (anonymousFunction != null)
            {
                var method = context.SemanticModel.GetSymbolSafe(anonymousFunction, context.CancellationToken) as IMethodSymbol;
                return method?.ReturnType;
            }

            return (context.ContainingSymbol as IMethodSymbol)?.ReturnType ??
                   (context.ContainingSymbol as IFieldSymbol)?.Type ??
                   (context.ContainingSymbol as IPropertySymbol)?.Type;
        }
    }
}
