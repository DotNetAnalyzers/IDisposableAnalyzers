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
            var value = this.RecursiveExpressionBodyProperty;
            value = this.RecursiveExpressionBodyGetter;
            value = this.RecursiveStatementBodyProperty;
            value = this.RecursiveExpressionBodyMethod();
            value = this.RecursiveExpressionBodyMethod(1);
            value = this.RecursiveStatementBodyMethod();
            value = this.RecursiveStatementBodyMethod(1);
            value = RecursiveStatementBodyMethodWithOptionalParameter(value);
            value = value;
        }

        public IDisposable RecursiveStatementBodyProperty
        {
            get
            {
                return this.RecursiveStatementBodyProperty;
            }
        }

        public IDisposable RecursiveExpressionBodyGetter
        {
            get => this.RecursiveExpressionBodyGetter;
        }

        public IDisposable RecursiveExpressionBodyProperty => this.RecursiveExpressionBodyProperty;

        public IDisposable CallingMethod => this.CallingMethod;

        public static bool RecursiveOut(out IDisposable value)
        {
            return RecursiveOut(out value);
        }

        public static bool RecursiveOut(int foo, out IDisposable value)
        {
            return RecursiveOut(2, out value);
        }

        public static bool RecursiveOut(double foo, out IDisposable value)
        {
            value = null;
            return RecursiveOut(3.0, out value);
        }

        public static bool RecursiveOut(string foo, out IDisposable value)
        {
            if (foo == null)
            {
                return RecursiveOut(3.0, out value);
            }

            value = null;
            return true;
        }

        public static bool RecursiveRef(ref IDisposable value)
        {
            return RecursiveRef(ref value);
        }

        public Disposable RecursiveMethod() => this.RecursiveMethod();

        public IDisposable CallingProperty() => this.CallingMethod;

        public void NotUsingRecursive()
        {
            var item1 = this.RecursiveExpressionBodyGetter;
            var item2 = this.RecursiveMethod();
        }

        public void UsingRecursive()
        {
            using (var item = new Disposable())
            {
            }

            using (var item = this.RecursiveExpressionBodyGetter)
            {
            }

            using (this.RecursiveExpressionBodyGetter)
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
            var value = this.RecursiveExpressionBodyProperty;
            value = this.RecursiveExpressionBodyGetter;
            value = this.RecursiveStatementBodyProperty;
            value = this.RecursiveExpressionBodyMethod();
            value = this.RecursiveExpressionBodyMethod(1);
            value = this.RecursiveStatementBodyMethod();
            value = this.RecursiveStatementBodyMethod(1);
            value = RecursiveStatementBodyMethodWithOptionalParameter(value);
            RecursiveOut(out value);
            RecursiveOut(1, out value);
            RecursiveOut(1.0, out value);
            RecursiveOut(string.Empty, out value);
            RecursiveRef(ref value);
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
