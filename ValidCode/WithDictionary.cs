// ReSharper disable All
namespace ValidCode
{
    using System;
    using System.Collections.Generic;

    public sealed class WithDictionary : IDisposable
    {
        private readonly Dictionary<string, Disposable> map = new Dictionary<string, Disposable>();

        public void M(string key)
        {
            if (this.map.ContainsKey(key))
            {
                return;
            }

            var disposable = new Disposable();
            this.map.Add(key, disposable);
        }

        public void Dispose()
        {
            foreach (var disposable in this.map.Values)
            {
                disposable.Dispose();
            }

            this.map.Clear();
        }
    }
}
