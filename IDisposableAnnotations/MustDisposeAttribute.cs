namespace IDisposableAnnotations
{
    using System;

    /// <summary>
    /// The return value must be disposed by the caller.
    /// </summary>
    [AttributeUsage(AttributeTargets.ReturnValue | AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
    public class MustDisposeAttribute : Attribute
    {
    }
}
