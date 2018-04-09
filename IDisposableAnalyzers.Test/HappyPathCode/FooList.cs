// ReSharper disable UnusedMember.Global Used in HappyPathWithAll.PropertyChangedAnalyzersSln
// ReSharper disable NotAccessedField.Local
// ReSharper disable UnusedVariable
// ReSharper disable UnusedParameter.Local
namespace IDisposableAnalyzers.Test.HappyPathCode
{
    using System.Collections;
    using System.Collections.Generic;


    internal class FooList<T> : IReadOnlyList<T>
    {
        private readonly List<T> inner = new List<T>();

        public int Count => this.inner.Count;

        public T this[int index] => this.inner[index];

        public IEnumerator<T> GetEnumerator()
        {
            return this.inner.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }
}
