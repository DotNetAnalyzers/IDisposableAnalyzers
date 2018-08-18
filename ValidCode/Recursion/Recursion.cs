// ReSharper disable All
#pragma warning disable 169
#pragma warning disable 1717
namespace ValidCode.Recursion
{
    using System;
    using System.Collections.Generic;

    public class Recursion
    {
        private IDisposable bar1;
        private IDisposable bar2;

        public Recursion()
        //            : this() compiler checks this so we test it separately.
        {
            var value = this.RecursiveExpressionBodyMethod();
            value = this.RecursiveExpressionBodyMethod(1);
            value = this.RecursiveStatementBodyMethod();
            value = this.RecursiveStatementBodyMethod(1);
            value = RecursiveStatementBodyMethodWithOptionalParameter(value);
            value = value;
        }

        public IDisposable CallingMethod => this.CallingMethod;


        public Disposable RecursiveMethod() => this.RecursiveMethod();

        public IDisposable CallingProperty() => this.CallingMethod;

        public void NotUsingRecursive()
        {
            var item1 = this.CallingMethod;
            var item2 = this.RecursiveMethod();
        }

        public void UsingRecursive()
        {
            using (var item = new Disposable())
            {
            }

            using (var item = this.CallingMethod)
            {
            }

            using (this.CallingMethod)
            {
            }

            using (var item = this.RecursiveMethod())
            {
            }

            using (this.RecursiveMethod())
            {
            }
        }

        public IDisposable RecursiveStatementBodyMethod()
        {
            return this.RecursiveStatementBodyMethod();
        }

        public IDisposable RecursiveExpressionBodyMethod() => this.RecursiveExpressionBodyMethod();

        public IDisposable RecursiveExpressionBodyMethod(int value) => this.RecursiveExpressionBodyMethod(value);

        public IDisposable RecursiveStatementBodyMethod(int value)
        {
            return this.RecursiveStatementBodyMethod(value);
        }

        public void SameStuffAsInCtor()
        {
            var value = this.RecursiveExpressionBodyMethod();
            value = this.RecursiveExpressionBodyMethod(1);
            value = this.RecursiveStatementBodyMethod();
            value = this.RecursiveStatementBodyMethod(1);
            value = RecursiveStatementBodyMethodWithOptionalParameter(value);
            value = value;
        }

        private static IDisposable RecursiveStatementBodyMethodWithOptionalParameter(IDisposable value, IEnumerable<IDisposable> values = null)
        {
            if (values == null)
            {
                return RecursiveStatementBodyMethodWithOptionalParameter(value, new[] { value });
            }

            return value;
        }
    }
}
