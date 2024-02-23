namespace IDisposableAnalyzers;

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
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
        Descriptors.IDISP005ReturnTypeShouldBeIDisposable,
        Descriptors.IDISP011DontReturnDisposed,
        Descriptors.IDISP012PropertyShouldNotReturnCreated,
        Descriptors.IDISP013AwaitInUsing);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(c => HandleReturnValue(c), SyntaxKind.ReturnStatement);
        context.RegisterSyntaxNodeAction(c => HandleArrow(c), SyntaxKind.ArrowExpressionClause);
        context.RegisterSyntaxNodeAction(c => HandleLambda(c), SyntaxKind.ParenthesizedLambdaExpression);
        context.RegisterSyntaxNodeAction(c => HandleLambda(c), SyntaxKind.SimpleLambdaExpression);
    }

    private static void HandleReturnValue(SyntaxNodeAnalysisContext context)
    {
        if (!context.IsExcludedFromAnalysis() &&
            context.ContainingSymbol is { } &&
            !IsIgnored(context.ContainingSymbol) &&
            context.Node is ReturnStatementSyntax { Expression: { } expression })
        {
            HandleReturnValue(context, expression);
        }
    }

    private static void HandleArrow(SyntaxNodeAnalysisContext context)
    {
        if (!context.IsExcludedFromAnalysis() &&
            context.ContainingSymbol is { } &&
            !IsIgnored(context.ContainingSymbol) &&
            context.Node is ArrowExpressionClauseSyntax { Expression: { } expression })
        {
            HandleReturnValue(context, expression);
        }
    }

    private static void HandleLambda(SyntaxNodeAnalysisContext context)
    {
        if (!context.IsExcludedFromAnalysis() &&
            context.ContainingSymbol is { } &&
            !IsIgnored(context.ContainingSymbol) &&
            context.Node is LambdaExpressionSyntax { Body: ExpressionSyntax expression } lambda &&
            ShouldHandle())
        {
            HandleReturnValue(context, expression);
        }

        bool ShouldHandle()
        {
            return lambda switch
            {
                { Parent: ArgumentSyntax } => Disposable.Ignores(lambda, context.SemanticModel, context.CancellationToken),
                _ => true,
            };
        }
    }

    private static void HandleReturnValue(SyntaxNodeAnalysisContext context, ExpressionSyntax returnValue)
    {
        if (Disposable.IsCreation(returnValue, context.SemanticModel, context.CancellationToken) &&
            context.SemanticModel.TryGetSymbol(returnValue, context.CancellationToken, out var returnedSymbol))
        {
            if (IsUsing(returnedSymbol, context.CancellationToken) ||
                Disposable.IsDisposedBefore(returnedSymbol, returnValue, context.SemanticModel, context.CancellationToken))
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptors.IDISP011DontReturnDisposed, returnValue.GetLocation()));
            }
            else
            {
                if (returnValue.FirstAncestor<AccessorDeclarationSyntax>() is { } accessor &&
                    accessor.IsKind(SyntaxKind.GetAccessorDeclaration))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Descriptors.IDISP012PropertyShouldNotReturnCreated, returnValue.GetLocation()));
                }

                if (returnValue.FirstAncestor<ArrowExpressionClauseSyntax>() is { Parent: PropertyDeclarationSyntax _ })
                {
                    context.ReportDiagnostic(Diagnostic.Create(Descriptors.IDISP012PropertyShouldNotReturnCreated, returnValue.GetLocation()));
                }

                if (!IsDisposableReturnTypeOrIgnored(ReturnType(context), context.Compilation))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Descriptors.IDISP005ReturnTypeShouldBeIDisposable, returnValue.GetLocation()));
                }
            }
        }
        else if (returnValue is InvocationExpressionSyntax { ArgumentList.Arguments: { } arguments } invocation &&
                 context.ContainingSymbol is { ContainingType: { } containingType })
        {
            foreach (var argument in arguments)
            {
                if (argument is { Expression: { } expression } &&
                    Disposable.IsCreation(expression, context.SemanticModel, context.CancellationToken) &&
                    context.SemanticModel.TryGetSymbol(expression, context.CancellationToken, out var argumentSymbol))
                {
                    if (IsUsing(argumentSymbol, context.CancellationToken) ||
                        Disposable.IsDisposedBefore(argumentSymbol, expression, context.SemanticModel, context.CancellationToken))
                    {
                        if (IsLazyEnumerable(invocation, containingType, context.SemanticModel, context.CancellationToken))
                        {
                            context.ReportDiagnostic(Diagnostic.Create(Descriptors.IDISP011DontReturnDisposed, argument.GetLocation()));
                        }
                    }
                }
            }
        }

        if (ReturnType(context)?.IsAwaitable() == true &&
            IsInUsing(returnValue) &&
            !returnValue.TryFirstAncestorOrSelf<AwaitExpressionSyntax>(out _) &&
            ShouldAwait(context, returnValue))
        {
            context.ReportDiagnostic(Diagnostic.Create(Descriptors.IDISP013AwaitInUsing, returnValue.GetLocation()));
        }

        static bool ShouldAwait(SyntaxNodeAnalysisContext context, ExpressionSyntax returnValue)
        {
            if (context.SemanticModel.GetType(returnValue, context.CancellationToken)?.IsAwaitable() == true)
            {
                if (returnValue.TryFirstAncestor(out InvocationExpressionSyntax? ancestor) &&
                    ancestor.TryGetMethodName(out var ancestorName) &&
                    ancestorName is "ThrowsAsync" or "Setup" or "Verify")
                {
                    return false;
                }

                return returnValue switch
                {
                    InvocationExpressionSyntax invocation
                        => !(invocation.IsSymbol(KnownSymbols.Task.FromResult,         context.SemanticModel, context.CancellationToken)
                             || invocation.IsSymbol(KnownSymbols.ValueTask.FromResult, context.SemanticModel, context.CancellationToken)),
                    MemberAccessExpressionSyntax { Name.Identifier.ValueText: "CompletedTask" } memberAccess
                        => !(memberAccess.IsSymbol(KnownSymbols.Task.CompletedTask,         context.SemanticModel, context.CancellationToken)
                             || memberAccess.IsSymbol(KnownSymbols.ValueTask.CompletedTask, context.SemanticModel, context.CancellationToken)),
                    DefaultExpressionSyntax => false,
                    LiteralExpressionSyntax => false,
                    BaseObjectCreationExpressionSyntax => false,
                    _ => true,
                };
            }

            return false;
        }
    }

    private static bool IsInUsing(SyntaxNode node)
    {
        if (node.TryFirstAncestor<UsingStatementSyntax>(out var usingStatement))
        {
            return usingStatement.Statement.Contains(node);
        }

        if (node.TryFirstAncestor<BlockSyntax>(out var block))
        {
            foreach (var statement in block.Statements)
            {
                if (statement.SpanStart >= node.SpanStart)
                {
                    break;
                }

                if (statement is LocalDeclarationStatementSyntax { UsingKeyword.ValueText: "using" })
                {
                    return true;
                }
            }

            return node.Parent != null && IsInUsing(node.Parent);
        }

        return false;
    }

    private static bool IsUsing(ISymbol symbol, CancellationToken cancellationToken)
    {
        if (symbol.TrySingleDeclaration<SyntaxNode>(cancellationToken, out var declaration))
        {
            return declaration switch
            {
                { Parent.Parent: UsingStatementSyntax } => true,
                { Parent.Parent: LocalDeclarationStatementSyntax { UsingKeyword.ValueText: "using" } } => true,
                _ => false,
            };
        }

        return false;
    }

    private static bool IsLazyEnumerable(InvocationExpressionSyntax invocation, INamedTypeSymbol containingType, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        using var recursion = Recursion.Borrow(containingType, semanticModel, cancellationToken);
        return IsLazyEnumerable(invocation, recursion);
    }

    private static bool IsLazyEnumerable(InvocationExpressionSyntax invocation, Recursion recursion)
    {
        if (recursion.Target(invocation) is { Symbol: IMethodSymbol method, Declaration: { } declaration } &&
            method.ReturnType.IsAssignableTo(KnownSymbols.IEnumerable, recursion.SemanticModel.Compilation))
        {
            using var yieldWalker = YieldStatementWalker.Borrow(declaration);
            if (yieldWalker.YieldStatements.Count > 0)
            {
                return true;
            }

            using var walker = ReturnValueWalker.Borrow(declaration, ReturnValueSearch.Member, recursion.SemanticModel, recursion.CancellationToken);
            foreach (var returnValue in walker.Values)
            {
                if (returnValue is InvocationExpressionSyntax nestedInvocation)
                {
                    if (IsLazyEnumerable(nestedInvocation, recursion))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private static bool IsDisposableReturnTypeOrIgnored(ITypeSymbol? type, Compilation compilation)
    {
        if (type is null ||
            type == KnownSymbols.Void)
        {
            return true;
        }

        if (Disposable.IsAssignableFrom(type, compilation))
        {
            return true;
        }

        if (type == KnownSymbols.IAsyncDisposable)
        {
            return true;
        }

        if (type == KnownSymbols.IEnumerator)
        {
            return true;
        }

        if (type == KnownSymbols.Task)
        {
            return type is INamedTypeSymbol { IsGenericType: true } namedType &&
                   Disposable.IsAssignableFrom(namedType.TypeArguments[0], compilation);
        }

        if (type == KnownSymbols.ValueTaskOfT)
        {
            return type is INamedTypeSymbol { IsGenericType: true } namedType &&
                   Disposable.IsAssignableFrom(namedType.TypeArguments[0], compilation);
        }

        if (type == KnownSymbols.Func)
        {
            return type is INamedTypeSymbol { IsGenericType: true } namedType &&
                   Disposable.IsAssignableFrom(namedType.TypeArguments[namedType.TypeArguments.Length - 1], compilation);
        }

        return false;
    }

    private static bool IsIgnored(ISymbol symbol)
    {
        if (symbol is IMethodSymbol method)
        {
            return method == KnownSymbols.IEnumerable.GetEnumerator;
        }

        return false;
    }

    private static ITypeSymbol? ReturnType(SyntaxNodeAnalysisContext context)
    {
        if (context.Node.FirstAncestorOrSelf<AnonymousFunctionExpressionSyntax>() is { } lambda)
        {
            var method = context.SemanticModel.GetSymbolSafe(lambda, context.CancellationToken) as IMethodSymbol;
            return method?.ReturnType;
        }

        if (context.Node.TryFirstAncestor(out LocalFunctionStatementSyntax? local))
        {
            var method = context.SemanticModel.GetDeclaredSymbol(local, context.CancellationToken) as IMethodSymbol;
            return method?.ReturnType;
        }

        return context switch
        {
            { ContainingSymbol: IFieldSymbol field } => field.Type,
            { ContainingSymbol: IPropertySymbol property } => property.Type,
            { ContainingSymbol: IMethodSymbol method } => method.ReturnType,
            _ => null,
        };
    }
}
