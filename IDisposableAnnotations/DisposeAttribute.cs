namespace IDisposableAnnotations
{
    using System;

    /// <summary>
    /// The return value should be disposed by caller.
    /// </summary>
    [AttributeUsage(AttributeTargets.ReturnValue, AllowMultiple = false, Inherited = true)]
    public class DisposeAttribute : Attribute
    {
    }
}
