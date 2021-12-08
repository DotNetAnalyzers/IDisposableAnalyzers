namespace ValidCode.Web.InstanceMapRepro
{
    using System;

    /// <summary>
    /// Similar to <see cref="Nullable{T}"/> but T can be a reference type.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    public interface IMaybe<out T>
    {
        /// <summary>
        /// Tells you if this instance has a value.
        /// Note that the value can be null.
        /// </summary>
        bool HasValue { get; }

        /// <summary>
        /// Check HasValue before getting the value.
        /// Note that null is a valid value for reference types.
        /// </summary>
        T Value { get; }
    }
}