namespace IDisposableAnalyzers
{
    using System;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Simplification;

    internal static class MethodFactory
    {
        internal static readonly TypeSyntax SystemObjectDisposedException = SyntaxFactory.QualifiedName(
            SyntaxFactory.IdentifierName("System"), SyntaxFactory.IdentifierName("ObjectDisposedException")).WithAdditionalAnnotations(Simplifier.Annotation);

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

        private static readonly MethodDeclarationSyntax DefaultVirtualDispose = SyntaxFactory.MethodDeclaration(
            attributeLists: default,
            modifiers: SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.VirtualKeyword)),
            returnType: SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
            explicitInterfaceSpecifier: default,
            identifier: SyntaxFactory.Identifier("Dispose"),
            typeParameterList: default,
            parameterList: SyntaxFactory.ParameterList(),
            constraintClauses: default,
            body: SyntaxFactory.Block(),
            expressionBody: default,
            semicolonToken: default);

        private static readonly MethodDeclarationSyntax DefaultPublicOverrideDispose = SyntaxFactory.MethodDeclaration(
            attributeLists: default,
            modifiers: SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.OverrideKeyword)),
            returnType: SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
            explicitInterfaceSpecifier: default,
            identifier: SyntaxFactory.Identifier("Dispose"),
            typeParameterList: default,
            parameterList: SyntaxFactory.ParameterList(),
            constraintClauses: default,
            body: SyntaxFactory.Block(
                SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.BaseExpression(),
                            SyntaxFactory.IdentifierName("Dispose"))))),
            expressionBody: default,
            semicolonToken: default);

        private static readonly MethodDeclarationSyntax DefaultProtectedVirtualDispose = SyntaxFactory.MethodDeclaration(
            attributeLists: default,
            modifiers: SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword), SyntaxFactory.Token(SyntaxKind.VirtualKeyword)),
            returnType: SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
            explicitInterfaceSpecifier: default,
            identifier: SyntaxFactory.Identifier("Dispose"),
            typeParameterList: default,
            parameterList: SingleBoolParameter("disposing"),
            constraintClauses: default,
            body: SyntaxFactory.Block(
                SyntaxFactory.IfStatement(
                    SyntaxFactory.IdentifierName("disposing"),
                    SyntaxFactory.Block())),
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

        internal static MethodDeclarationSyntax VirtualDispose(ExpressionSyntax disposedField)
        {
            if (disposedField == null)
            {
                return DefaultVirtualDispose;
            }

            return DefaultVirtualDispose.InsertBodyStatements(
                0,
                SyntaxFactory.IfStatement(
                    disposedField,
                    SyntaxFactory.Block(SyntaxFactory.ReturnStatement())),
                SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        disposedField,
                        SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression))));
        }

        internal static MethodDeclarationSyntax OverrideDispose(ExpressionSyntax disposedField, IMethodSymbol toOverride)
        {
            switch (toOverride)
            {
                case { DeclaredAccessibility: Accessibility.Public, IsVirtual: true, Parameters: { Length: 0 } }:
                    return DefaultPublicOverrideDispose.InsertBodyStatements(
                        0,
                        IfDisposedReturn(),
                        DisposedTrue());
                case { DeclaredAccessibility: Accessibility.Protected, IsVirtual: true, Parameters: { Length: 1 } parameters }
                    when parameters[0] is { Type: { SpecialType: SpecialType.System_Boolean } } parameter:
                    return SyntaxFactory.MethodDeclaration(
                        attributeLists: default,
                        modifiers: SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword), SyntaxFactory.Token(SyntaxKind.OverrideKeyword)),
                        returnType: SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                        explicitInterfaceSpecifier: default,
                        identifier: SyntaxFactory.Identifier(toOverride.Name),
                        typeParameterList: default,
                        parameterList: SingleBoolParameter(parameter.Name),
                        constraintClauses: default,
                        body: SyntaxFactory.Block(
                            IfDisposedReturn(),
                            DisposedTrue(),
                            IfDisposing(parameter),
                            BaseDispose(parameter)),
                        expressionBody: default,
                        semicolonToken: default);
                case { DeclaredAccessibility: Accessibility.Public, IsVirtual: true, Parameters: { Length: 1 } parameters }
                    when parameters[0] is { Type: { SpecialType: SpecialType.System_Boolean } } parameter:
                    return SyntaxFactory.MethodDeclaration(
                        attributeLists: default,
                        modifiers: SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.OverrideKeyword)),
                        returnType: SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                        explicitInterfaceSpecifier: default,
                        identifier: SyntaxFactory.Identifier(toOverride.Name),
                        typeParameterList: default,
                        parameterList: SingleBoolParameter(parameter.Name),
                        constraintClauses: default,
                        body: SyntaxFactory.Block(
                            IfDisposedReturn(),
                            DisposedTrue(),
                            IfDisposing(parameter),
                            BaseDispose(parameter)),
                        expressionBody: default,
                        semicolonToken: default);
                default:
                    throw new NotSupportedException($"Could not generate code for overriding {toOverride}");
            }

            IfStatementSyntax IfDisposedReturn()
            {
                return SyntaxFactory.IfStatement(
                    disposedField,
                    SyntaxFactory.Block(SyntaxFactory.ReturnStatement()));
            }

            ExpressionStatementSyntax DisposedTrue()
            {
                return SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        disposedField,
                        SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression)));
            }

            static IfStatementSyntax IfDisposing(IParameterSymbol parameter)
            {
                return SyntaxFactory.IfStatement(
                    SyntaxFactory.IdentifierName(parameter.Name),
                    SyntaxFactory.Block());
            }

            static ExpressionStatementSyntax BaseDispose(IParameterSymbol parameter)
            {
                return SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.BaseExpression(),
                            SyntaxFactory.IdentifierName(parameter.ContainingSymbol.Name)),
                        IDisposableFactory.Arguments(SyntaxFactory.IdentifierName(parameter.Name))));
            }
        }

        internal static MethodDeclarationSyntax ProtectedVirtualDispose(ExpressionSyntax disposedField)
        {
            if (disposedField == null)
            {
                return DefaultProtectedVirtualDispose;
            }

            return DefaultProtectedVirtualDispose.InsertBodyStatements(
                0,
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
            if (statements is null || statements.Length == 0)
            {
                return EmptyDispose;
            }

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

        private static MethodDeclarationSyntax InsertBodyStatements(this MethodDeclarationSyntax method, int index, params StatementSyntax[] items)
        {
            return method.WithBody(method.Body.WithStatements(method.Body.Statements.InsertRange(index, items)));
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

        private static ParameterListSyntax SingleBoolParameter(string name)
        {
            return SyntaxFactory.ParameterList(
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.Parameter(
                        default,
                        default,
                        SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.BoolKeyword)),
                        SyntaxFactory.Identifier(name),
                        default)));
        }
    }
}
