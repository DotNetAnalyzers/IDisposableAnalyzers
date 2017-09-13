namespace IDisposableAnalyzers
{
    internal class SerialDisposableType : QualifiedType
    {
        internal readonly QualifiedProperty Disposable;

        internal SerialDisposableType()
            : base("System.Reactive.Disposables.SerialDisposable")
        {
            this.Disposable = new QualifiedProperty(this, nameof(this.Disposable));
        }
    }
}