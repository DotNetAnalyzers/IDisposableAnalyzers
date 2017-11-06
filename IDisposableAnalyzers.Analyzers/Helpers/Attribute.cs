namespace IDisposableAnalyzers
{
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class Attribute
    {
        internal static bool TryGetAttribute(MethodDeclarationSyntax methodDeclaration, QualifiedType attributeType, SemanticModel semanticModel, CancellationToken cancellationToken, out AttributeSyntax result)
        {
            result = null;
            if (methodDeclaration == null)
            {
                return false;
            }

            foreach (var attributeList in methodDeclaration.AttributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    if (IsType(attribute, attributeType, semanticModel, cancellationToken))
                    {
                        result = attribute;
                        return true;
                    }
                }
            }

            return false;
        }

        internal static bool IsType(AttributeSyntax attribute, QualifiedType qualifiedType, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            bool IsMatch(SimpleNameSyntax sn, QualifiedType qt)
            {
                return sn.Identifier.ValueText == qt.Type ||
                       qt.Type.IsParts(sn.Identifier.ValueText, "Attribute");
            }

            if (attribute == null)
            {
                return false;
            }

            if (attribute.Name is SimpleNameSyntax simpleName)
            {
                if (!IsMatch(simpleName, qualifiedType) &&
                    !AliasWalker.Contains(attribute.SyntaxTree, simpleName.Identifier.ValueText))
                {
                    return false;
                }
            }
            else if (attribute.Name is QualifiedNameSyntax qualifiedName &&
                     qualifiedName.Right is SimpleNameSyntax typeName)
            {
                if (!IsMatch(typeName, qualifiedType) &&
                    !AliasWalker.Contains(attribute.SyntaxTree, typeName.Identifier.ValueText))
                {
                    return false;
                }
            }

            var attributeType = semanticModel.GetTypeInfoSafe(attribute, cancellationToken).Type;
            return attributeType == qualifiedType;
        }
    }
}