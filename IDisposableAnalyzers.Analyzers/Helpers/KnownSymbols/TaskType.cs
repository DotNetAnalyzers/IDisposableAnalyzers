namespace IDisposableAnalyzers
{
    internal class TaskType : QualifiedType
    {
        internal readonly QualifiedMethod FromResult;
        internal readonly QualifiedMethod Run;
        internal readonly QualifiedMethod RunOfT;
        internal readonly QualifiedMethod ConfigureAwait;

        internal TaskType()
            : base("System.Threading.Tasks.Task")
        {
            this.FromResult = new QualifiedMethod(this, nameof(this.FromResult));
            this.Run = new QualifiedMethod(this, nameof(this.Run));
            this.RunOfT = new QualifiedMethod(this, $"{nameof(this.Run)}`1");
            this.ConfigureAwait = new QualifiedMethod(this, nameof(this.ConfigureAwait));
        }
    }
}