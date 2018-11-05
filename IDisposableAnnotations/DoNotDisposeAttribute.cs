namespace IDisposableAnnotations
{
    using System;

    /// <summary>
    /// The return value must not be disposed by the caller.
    /// </summary>
    [AttributeUsage(AttributeTargets.ReturnValue | AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
    public class DoNotDisposeAttribute : Attribute
    {
    }
}
