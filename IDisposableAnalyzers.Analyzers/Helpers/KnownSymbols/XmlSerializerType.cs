namespace IDisposableAnalyzers
{
    using Gu.Roslyn.AnalyzerExtensions;

    // ReSharper disable once InconsistentNaming
    internal class XmlSerializerType : QualifiedType
    {
        public XmlSerializerType()
            : base("System.Xml.Serialization.XmlSerializer")
        {
        }
    }
}
