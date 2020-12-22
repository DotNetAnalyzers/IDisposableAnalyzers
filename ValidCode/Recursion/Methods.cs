// ReSharper disable All
namespace ValidCode.Recursion
{
    using System;
    using System.Collections.Generic;

    public class Methods
    {
        private readonly IDisposable bar1;
        private readonly IDisposable bar2;

        public Methods()
        {
            var value = this.StatementBody();
            value = this.StatementBody(1);
            value = StatementBodyWithOptionalParameter(value);
            value = StatementBodyWithOptionalParameter(value, null);
            value = StatementBodyWithOptionalParameter(value, new[] { value });
            value = this.ExpressionBody();
            value = this.ExpressionBody(1);
            value = this.Cycle1();
            value = this.Cycle2();
            value = value;
        }

        public IDisposable StatementBody()
        {
            return this.StatementBody();
        }

        public IDisposable StatementBody(int value)
        {
            return this.StatementBody(value);
        }

        private static IDisposable StatementBodyWithOptionalParameter(IDisposable value, IEnumerable<IDisposable> values = null)
        {
            if (values == null)
            {
                return StatementBodyWithOptionalParameter(value, new[] { value });
            }

            return value;
        }

        public Disposable ExpressionBody() => this.ExpressionBody();

        public IDisposable ExpressionBody(int value) => this.ExpressionBody(value);

        public IDisposable Cycle1() => this.Cycle2();

        public IDisposable Cycle2() => this.Cycle1();

        public void NotUsingRecursive()
        {
            var value1 = this.StatementBody();
            var value2 = this.StatementBody(1);
            var value3 = StatementBodyWithOptionalParameter(value1);
            var value4 = StatementBodyWithOptionalParameter(value1, null);
            var value5 = StatementBodyWithOptionalParameter(value1, new[] { value1 });
            var value6 = this.ExpressionBody();
            var value7 = this.ExpressionBody(1);
            var value8 = this.Cycle1();
            var value9 = this.Cycle2();
        }

        public void UsingRecursive()
        {
            using (var item = this.ExpressionBody())
            {
            }

            using (this.ExpressionBody())
            {
            }

            using (var item = this.Cycle1())
            {
            }

            using (this.Cycle1())
            {
            }
        }

        public void SameAsInCtor()
        {
            var value = this.StatementBody();
            value = this.StatementBody(1);
            value = StatementBodyWithOptionalParameter(value);
            value = StatementBodyWithOptionalParameter(value, null);
            value = StatementBodyWithOptionalParameter(value, new[] { value });
            value = this.ExpressionBody();
            value = this.ExpressionBody(1);
            value = this.Cycle1();
            value = this.Cycle2();
            value = value;
        }

        public IDisposable Return(IDisposable p)
        {
            if (true)
            {
                var temp = p;
                return p;
            }

            p = p;
            return p;
        }
    }
}
