namespace ValidCode
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class Linq
    {
        public Linq(IEnumerable<IDisposable> disposables)
        {
            var first = disposables.First();
            first = disposables.Single();
        }
    }
}
