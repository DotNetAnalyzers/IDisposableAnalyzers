namespace IDisposableAnalyzers
{
    using System;
    using Microsoft.CodeAnalysis;

    internal static class AccessibilityExt
    {
        internal static string ToCodeString(this Accessibility accessibility)
        {
            switch (accessibility)
            {
                case Accessibility.NotApplicable:
                    break;
                case Accessibility.Private:
                    return "private";
                case Accessibility.ProtectedAndInternal:
                    return "private protected";
                case Accessibility.Protected:
                    return "protected";
                case Accessibility.Internal:
                    return "internal";
                case Accessibility.ProtectedOrInternal:
                    return "internal protected";
                case Accessibility.Public:
                    return "public";
                default:
                    throw new ArgumentOutOfRangeException(nameof(accessibility), accessibility, null);
            }

            return string.Empty;
        }
    }
}