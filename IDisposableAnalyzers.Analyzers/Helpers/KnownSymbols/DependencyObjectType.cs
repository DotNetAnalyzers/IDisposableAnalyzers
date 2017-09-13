namespace IDisposableAnalyzers
{
    internal class DependencyObjectType : QualifiedType
    {
        internal readonly QualifiedMethod GetValue;
        internal readonly QualifiedMethod SetValue;
        internal readonly QualifiedMethod SetCurrentValue;

        internal DependencyObjectType()
            : base("System.Windows.DependencyObject")
        {
            this.GetValue = new QualifiedMethod(this, nameof(this.GetValue));
            this.SetValue = new QualifiedMethod(this, nameof(this.SetValue));
            this.SetCurrentValue = new QualifiedMethod(this, nameof(this.SetCurrentValue));
        }
    }
}