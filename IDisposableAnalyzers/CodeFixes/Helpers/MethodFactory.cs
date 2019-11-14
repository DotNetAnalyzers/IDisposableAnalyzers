namespace IDisposableAnalyzers
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Simplification;

    internal static class MethodFactory
    {
        internal static readonly TypeSyntax SystemObjectDisposedException = SyntaxFactory.QualifiedName(SyntaxFactory.IdentifierName("System"), SyntaxFactory.IdentifierName("ObjectDisposedException"))
                                                                             .WithAdditionalAnnotations(Simplifier.Annotation);

        private static readonly MethodDeclarationSyntax EmptyDispose = SyntaxFactory.MethodDeclaration(
            attributeLists: default,
            modifiers: SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)),
            returnType: SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
            explicitInterfaceSpecifier: default,
            identifier: SyntaxFactory.Identifier("Dispose"),
            typeParameterList: default,
            parameterList: SyntaxFactory.ParameterList(),
            constraintClauses: default,
            body: SyntaxFactory.Block(),
            expressionBody: default,
            semicolonToken: default);

        private static readonly MethodDeclarationSyntax EmptyProtectedThrowIfDisposed = SyntaxFactory.MethodDeclaration(
            attributeLists: default,
            modifiers: SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword), SyntaxFactory.Token(SyntaxKind.VirtualKeyword)),
            returnType: SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
            explicitInterfaceSpecifier: default,
            identifier: SyntaxFactory.Identifier("ThrowIfDisposed"),
            typeParameterList: default,
            parameterList: SyntaxFactory.ParameterList(),
            constraintClauses: default,
            body: SyntaxFactory.Block(),
            expressionBody: default,
            semicolonToken: default);

        private static readonly MethodDeclarationSyntax EmptyPrivateThrowIfDisposed = SyntaxFactory.MethodDeclaration(
            attributeLists: default,
            modifiers: SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)),
            returnType: SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
            explicitInterfaceSpecifier: default,
            identifier: SyntaxFactory.Identifier("ThrowIfDisposed"),
            typeParameterList: default,
            parameterList: SyntaxFactory.ParameterList(),
            constraintClauses: default,
            body: SyntaxFactory.Block(),
            expressionBody: default,
            semicolonToken: default);

        internal static MethodDeclarationSyntax Dispose(ExpressionSyntax disposedField)
        {
            if (disposedField == null)
            {
                return EmptyDispose;
            }

            return EmptyDispose.AddBodyStatements(
                SyntaxFactory.IfStatement(
                    disposedField,
                    SyntaxFactory.Block(SyntaxFactory.ReturnStatement())),
                SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        disposedField,
                        SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression))));
        }

        internal static MethodDeclarationSyntax Dispose(params StatementSyntax[] statements)
        {
            return EmptyDispose.AddBodyStatements(statements);
        }

        internal static MethodDeclarationSyntax PrivateThrowIfDisposed(ExpressionSyntax disposedField)
        {
            return EmptyPrivateThrowIfDisposed.AddBodyStatements(IfDisposedThrow(disposedField));
        }

        internal static MethodDeclarationSyntax ProtectedThrowIfDisposed(ExpressionSyntax disposedField)
        {
            return EmptyProtectedThrowIfDisposed.AddBodyStatements(IfDisposedThrow(disposedField));
        }

        private static IfStatementSyntax IfDisposedThrow(ExpressionSyntax disposedField)
        {
            return SyntaxFactory.IfStatement(
                disposedField,
                SyntaxFactory.Block(
                    SyntaxFactory.ThrowStatement(
                        SyntaxFactory.ObjectCreationExpression(
                            SystemObjectDisposedException,
                            GetTypeFullName(),
                            null))));

            ArgumentListSyntax GetTypeFullName()
            {
                return SyntaxFactory.ArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Argument(
                            SyntaxFactory.MemberAccessExpression(
                                kind: SyntaxKind.SimpleMemberAccessExpression,
                                expression: SyntaxFactory.InvocationExpression(
                                    expression: GetType()),
                                name: SyntaxFactory.IdentifierName("FullName")))));

                ExpressionSyntax GetType()
                {
                    if (disposedField.IsKind(SyntaxKind.SimpleMemberAccessExpression))
                    {
                        return SyntaxFactory.MemberAccessExpression(
                            kind: SyntaxKind.SimpleMemberAccessExpression,
                            expression: SyntaxFactory.ThisExpression(),
                            name: SyntaxFactory.IdentifierName("GetType"));
                    }

                    return SyntaxFactory.IdentifierName("GetType");
                }
            }
        }
    }
}
