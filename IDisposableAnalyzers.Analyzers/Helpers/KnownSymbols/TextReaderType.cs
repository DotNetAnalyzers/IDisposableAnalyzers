namespace IDisposableAnalyzers
{
    using Gu.Roslyn.AnalyzerExtensions;

    internal class TextReaderType : QualifiedType
    {
        internal TextReaderType()
            : base("System.IO.TextReader")
        {
        }
    }
}
