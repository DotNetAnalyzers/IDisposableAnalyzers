// ReSharper disable All
namespace ValidCode.NetCore
{
    using System.IO;
    using System.Xml.Serialization;

    public class Issue254
    {
        private static readonly XmlSerializer Serializer = new XmlSerializer(typeof(Issue254));

        public static void Save(string fileName, Issue254 item)
        {
            using var stream = new FileStream(fileName, FileMode.Create);
            Serializer.Serialize(stream, item);
        }
    }
}
