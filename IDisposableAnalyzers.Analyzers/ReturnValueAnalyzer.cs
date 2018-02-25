namespace IDisposableAnalyzers
{
    using System.Collections.Immutable;
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class ReturnValueAnalyzer : DiagnosticAnalyzer
    {
        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
            IDISP005ReturntypeShouldIndicateIDisposable.Descriptor,
            IDISP011DontReturnDisposed.Descriptor,
            IDISP012PropertyShouldNotReturnCreated.Descriptor,
            IDISP013AwaitInUsing.Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(HandleReturnValue, SyntaxKind.ReturnStatement);
            context.RegisterSyntaxNodeAction(HandleArrow, SyntaxKind.ArrowExpressionClause);
            context.RegisterSyntaxNodeAction(HandleLamdba, SyntaxKind.ParenthesizedLambdaExpression);
            context.RegisterSyntaxNodeAction(HandleLamdba, SyntaxKind.SimpleLambdaExpression);
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

        private static void HandleLamdba(SyntaxNodeAnalysisContext context)
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
            if (Disposable.IsCreation(returnValue, context.SemanticModel, context.CancellationToken)
                          .IsEither(Result.Yes, Result.AssumeYes) &&
                context.SemanticModel.GetSymbolSafe(returnValue, context.CancellationToken) is ISymbol returnedSymbol)
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

                    if (!IsDisposableReturnTypeOrIgnored(ReturnType(context)))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(IDISP005ReturntypeShouldIndicateIDisposable.Descriptor, returnValue.GetLocation()));
                    }
                }
            }
            else if (returnValue is InvocationExpressionSyntax invocation)
            {
                if (invocation.ArgumentList != null)
                {
                    foreach (var argument in invocation.ArgumentList.Arguments)
                    {
                        if (Disposable.IsCreation(argument.Expression, context.SemanticModel, context.CancellationToken)
                                      .IsEither(Result.Yes, Result.AssumeYes) &&
                            context.SemanticModel.GetSymbolSafe(argument.Expression, context.CancellationToken) is ISymbol argumentSymbol)
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

                if (invocation.FirstAncestor<UsingStatementSyntax>() != null &&
                    invocation.Expression is MemberAccessExpressionSyntax memberAccess)
                {
                    var symbol = context.SemanticModel.GetSymbolSafe(memberAccess.Expression, context.CancellationToken);
                    if (IsInUsing(symbol, context.CancellationToken))
                    {
                        if (context.SemanticModel.GetTypeInfoSafe(returnValue, context.CancellationToken).Type.Is(KnownSymbol.Task))
                        {
                            context.ReportDiagnostic(Diagnostic.Create(IDISP013AwaitInUsing.Descriptor, returnValue.GetLocation()));
                        }
                    }
                }
            }
        }

        private static bool IsInUsing(ISymbol symbol, CancellationToken cancellationToken)
        {
            return symbol.TryGetSingleDeclaration<SyntaxNode>(cancellationToken, out var declaration) &&
                   declaration.Parent?.Parent is UsingStatementSyntax;
        }

        private static bool IsLazyEnumerable(InvocationExpressionSyntax invocation, SemanticModel semanticModel, CancellationToken cancellationToken, PooledHashSet<SyntaxNode> set = null)
        {
            if (semanticModel.GetSymbolSafe(invocation, cancellationToken) is IMethodSymbol method &&
                method.ReturnType.Is(KnownSymbol.IEnumerable) &&
                method.TryGetSingleDeclaration(cancellationToken, out MethodDeclarationSyntax methodDeclaration))
            {
                if (YieldStatementWalker.Any(methodDeclaration))
                {
                    return true;
                }

                using (var walker = ReturnValueWalker.Borrow(methodDeclaration, Search.TopLevel, semanticModel, cancellationToken))
                {
                    set = PooledHashSet<SyntaxNode>.BorrowOrIncrementUsage(set);
                    foreach (var returnValue in walker)
                    {
                        if (returnValue is InvocationExpressionSyntax nestedInvocation &&
                            set.Add(returnValue) &&
                            IsLazyEnumerable(nestedInvocation, semanticModel, cancellationToken))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static bool IsDisposableReturnTypeOrIgnored(ITypeSymbol type)
        {
            if (type == null ||
                type == KnownSymbol.Void)
            {
                return true;
            }

            if (Disposable.IsAssignableTo(type))
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
                return namedType?.IsGenericType == true && Disposable.IsAssignableTo(namedType.TypeArguments[0]);
            }

            if (type == KnownSymbol.Func)
            {
                var namedType = type as INamedTypeSymbol;
                return namedType?.IsGenericType == true && Disposable.IsAssignableTo(namedType.TypeArguments[namedType.TypeArguments.Length - 1]);
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
