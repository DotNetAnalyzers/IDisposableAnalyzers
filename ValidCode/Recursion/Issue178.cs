// ReSharper disable All
namespace ValidCode.Recursion
{
    using System.IO;

    class Issue178
    {
        void M() => M(new MemoryStream());

        void M(System.IDisposable p) => M(p);
    }
}
