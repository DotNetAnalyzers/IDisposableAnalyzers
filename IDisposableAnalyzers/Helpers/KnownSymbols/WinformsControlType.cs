namespace IDisposableAnalyzers;

using Gu.Roslyn.AnalyzerExtensions;

internal class WinformsControlType : QualifiedType
{
    internal readonly QualifiedMethod Show;

    internal WinformsControlType()
        : base("System.Windows.Forms.Control")
    {
        this.Show = new QualifiedMethod(this, nameof(this.Show));
    }
}
