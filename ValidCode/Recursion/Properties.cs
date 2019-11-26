// ReSharper disable All
#pragma warning disable 1717
namespace ValidCode.Recursion
{
    using System;

    public sealed class Properties : IDisposable
    {
        private IDisposable withBackingField1;
        private IDisposable withBackingField2;

        public Properties()
        {
            var value = this.StatementBody;
            value = this.ExpressionBody;
            value = this.ExpressionBodyGetter;
            value = this.StatementBodyRecursiveAccessors;
            value = this.ExpressionBodyRecursiveAccessors;
            value = this.StatementBodyCycle1;
            value = this.StatementBodyCycle2;
            value = this.StatementBodyCycle3;
            value = this.ExpressionBodyCycle1;
            value = this.ExpressionBodyCycle2;
            value = this.ExpressionBodyCycle3;
            value = this.WithBackingField1;
            value = this.WithBackingField2;
        }

        public IDisposable StatementBody
        {
            get
            {
                return this.StatementBody;
            }
        }

        public IDisposable ExpressionBody => this.ExpressionBody;

        public IDisposable ExpressionBodyGetter
        {
            get => this.ExpressionBodyGetter;
        }

        public IDisposable StatementBodyRecursiveAccessors
        {
            get
            {
                return this.StatementBodyRecursiveAccessors;
            }

            set
            {
                if (value == this.StatementBodyRecursiveAccessors)
                {
                    return;
                }

                this.StatementBodyRecursiveAccessors = value;
            }
        }

        public IDisposable ExpressionBodyRecursiveAccessors
        {
            get
            {
                return this.ExpressionBodyRecursiveAccessors;
            }

            set
            {
                if (value == this.ExpressionBodyRecursiveAccessors)
                {
                    return;
                }

                this.ExpressionBodyRecursiveAccessors = value;
            }
        }

        public IDisposable StatementBodyCycle1
        {
            get
            {
                return this.StatementBodyCycle2;
            }

            set
            {
                if (value == this.StatementBodyCycle2)
                {
                    return;
                }

                this.StatementBodyCycle2 = value;
            }
        }

        public IDisposable StatementBodyCycle2
        {
            get => this.StatementBodyCycle3;
            set
            {
                if (value == this.StatementBodyCycle3)
                {
                    return;
                }

                this.StatementBodyCycle3 = value;
            }
        }

        public IDisposable StatementBodyCycle3
        {
            get => this.StatementBodyCycle1;
            set
            {
                if (value == this.StatementBodyCycle1)
                {
                    return;
                }

                this.StatementBodyCycle1 = value;
            }
        }

        public IDisposable ExpressionBodyCycle1 => this.ExpressionBodyCycle2;

        public IDisposable ExpressionBodyCycle2 => this.ExpressionBodyCycle3;

        public IDisposable ExpressionBodyCycle3 => this.ExpressionBodyCycle1;

        public IDisposable WithBackingField1
        {
            get
            {
                return this.withBackingField1;
            }

            set
            {
                if (Equals(value, this.withBackingField1))
                {
                    return;
                }

                if (value != null && this.withBackingField2 != null)
                {
                    this.WithBackingField2 = null;
                }

                this.withBackingField1 = value;
            }
        }

        public IDisposable WithBackingField2
        {
            get
            {
                return this.withBackingField2;
            }

            set
            {
                if (Equals(value, this.withBackingField2))
                {
                    return;
                }

                if (value != null && this.withBackingField1 != null)
                {
                    this.WithBackingField1 = null;
                }

                this.withBackingField2 = value;
            }
        }

        public void Dispose()
        {
#pragma warning disable IDISP007 // Don't dispose injected.
            this.withBackingField1.Dispose();
            this.withBackingField2.Dispose();
            this.StatementBody.Dispose();
            this.ExpressionBody.Dispose();
            this.ExpressionBodyGetter.Dispose();
            this.StatementBodyRecursiveAccessors.Dispose();
            this.ExpressionBodyRecursiveAccessors.Dispose();
            this.StatementBodyCycle1.Dispose();
            this.StatementBodyCycle2.Dispose();
            this.StatementBodyCycle3.Dispose();
            this.ExpressionBodyCycle1.Dispose();
            this.ExpressionBodyCycle2.Dispose();
            this.ExpressionBodyCycle3.Dispose();
            this.WithBackingField1.Dispose();
            this.WithBackingField2.Dispose();
#pragma warning restore IDISP007 // Don't dispose injected.
        }

        public void SameAsInCtor()
        {
            var value = this.StatementBody;
            value = this.ExpressionBody;
            value = this.ExpressionBodyGetter;
            value = this.StatementBodyRecursiveAccessors;
            value = this.ExpressionBodyRecursiveAccessors;
            value = this.StatementBodyCycle1;
            value = this.StatementBodyCycle2;
            value = this.StatementBodyCycle3;
            value = this.ExpressionBodyCycle1;
            value = this.ExpressionBodyCycle2;
            value = this.ExpressionBodyCycle3;
            value = this.WithBackingField1;
            value = this.WithBackingField2;
        }

        public void UsingRecursive()
        {
            using (var item = this.StatementBody)
            {
            }

            using (this.StatementBody)
            {
            }

            using (var item = this.ExpressionBody)
            {
            }

            using (this.ExpressionBody)
            {
            }

            using (var item = this.ExpressionBodyGetter)
            {
            }

            using (this.ExpressionBodyGetter)
            {
            }

#pragma warning disable IDISP007 // Don't dispose injected.
            using (var item = this.StatementBodyRecursiveAccessors)
            {
            }

            using (this.StatementBodyRecursiveAccessors)
            {
            }

            using (var item = this.ExpressionBodyRecursiveAccessors)
            {
            }

            using (this.ExpressionBodyRecursiveAccessors)
            {
            }

            using (var item = this.StatementBodyCycle1)
            {
            }

            using (this.StatementBodyCycle1)
            {
            }

            using (var item = this.ExpressionBodyCycle1)
            {
            }

            using (this.ExpressionBodyCycle1)
            {
            }

            using (var item = this.WithBackingField1)
            {
            }

            using (this.WithBackingField1)
            {
            }

            using (var item = this.withBackingField1)
            {
            }

            using (this.withBackingField1)
            {
            }

            using (var item = this.WithBackingField2)
            {
            }

            using (this.WithBackingField2)
            {
            }

            using (var item = this.withBackingField2)
            {
            }

            using (this.withBackingField2)
            {
            }
#pragma warning restore IDISP007 // Don't dispose injected.
        }
    }
}
