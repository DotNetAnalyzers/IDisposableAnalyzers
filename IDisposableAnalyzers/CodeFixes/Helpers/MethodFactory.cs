namespace IDisposableAnalyzers
{
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class MethodFactory
    {
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
    }
}
