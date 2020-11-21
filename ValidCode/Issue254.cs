// ReSharper disable All
namespace ValidCode
{
    using System.IO;

    public class Issue254
    {
        public static void Save(string fileName)
        {
            using var stream = new FileStream(fileName, FileMode.Create);
        }
    }
}
