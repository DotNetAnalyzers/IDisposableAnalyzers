namespace IDisposableAnalyzers
{
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class TypeSyntaxExt
    {
        internal static bool IsVoid(this TypeSyntax type)
        {
            var predefinedType = type as PredefinedTypeSyntax;
            if (predefinedType == null)
            {
                return false;
            }

            return predefinedType.Keyword.ValueText == "void";
        }
    }
}