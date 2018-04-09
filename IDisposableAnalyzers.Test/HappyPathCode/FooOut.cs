// ReSharper disable UnusedMember.Global Used in HappyPathWithAll.PropertyChangedAnalyzersSln
// ReSharper disable NotAccessedField.Local
// ReSharper disable UnusedVariable
// ReSharper disable UnusedParameter.Local
// ReSharper disable RedundantAssignment
// ReSharper disable ArrangeAccessorOwnerBody
// ReSharper disable FunctionRecursiveOnAllPaths
// ReSharper disable UnusedParameter.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ArrangeThisQualifier
// ReSharper disable PossibleUnintendedReferenceComparison
// ReSharper disable RedundantCheckBeforeAssignment
// ReSharper disable UnusedMethodReturnValue.Global
// ReSharper disable NotAccessedVariable
// ReSharper disable InlineOutVariableDeclaration
#pragma warning disable 1717
#pragma warning disable SA1101 // Prefix local calls with this
#pragma warning disable GU0011 // Don't ignore the return value.
#pragma warning disable GU0010 // Assigning same value.
#pragma warning disable IDE0009 // Member access should be qualified.
namespace IDisposableAnalyzers.Test.HappyPathCode
{
    using System;
    using System.IO;

    public class FooOut
    {
        public static bool TryGetStream(out Stream stream)
        {
            return TryGetStreamCore(out stream);
        }

        public void Bar()
        {
            IDisposable disposable;
            using (disposable = new Disposable())
            {
            }
        }

        public void Baz()
        {
            Stream disposable;
            if (TryGetStreamCore(out disposable))
            {
                using (disposable)
                {
                }
            }
        }

        private static bool TryGetStreamCore(out Stream stream)
        {
            stream = File.OpenRead(string.Empty);
            return true;
        }
    }
}
