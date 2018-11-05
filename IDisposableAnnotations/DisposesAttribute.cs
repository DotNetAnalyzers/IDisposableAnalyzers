namespace IDisposableAnnotations
{
    using System;

    /// <summary>
    /// The containing method owns the instance and is responsible for disposing it.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
    public class DisposesAttribute : Attribute
    {
    }
}
