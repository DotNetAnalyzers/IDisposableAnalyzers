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
#pragma warning disable 1717
#pragma warning disable IDE0009 // Member access should be qualified.
namespace ValidCode
{
    using System.Collections.Concurrent;
    using System.IO;

    internal class FooCached
    {
        private static readonly ConcurrentDictionary<int, Stream> Cache = new ConcurrentDictionary<int, Stream>();
        private readonly ConcurrentDictionary<int, Stream> cache = new ConcurrentDictionary<int, Stream>();

        public static long Bar()
        {
            var stream = Cache.GetOrAdd(1, _ => File.OpenRead(string.Empty));
            return stream.Length;
        }

        public long Bar1()
        {
            var stream = this.cache.GetOrAdd(1, _ => File.OpenRead(string.Empty));
            return stream.Length;
        }
    }
}
