namespace ValidCode
{
    using System;
    using global::Moq;

    public class Moq
    {
        public Moq()
        {
            var mock1 = Mock.Of<IDisposable>();
            var mock2 = new Mock<IDisposable>();
        }
    }
}
