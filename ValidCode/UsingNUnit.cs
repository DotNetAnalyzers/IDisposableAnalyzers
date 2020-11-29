// ReSharper disable All
namespace ValidCode
{
    using System;
    using NUnit.Framework;

    public class UsingNUnit
    {
        private IDisposable _container;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _container = System.Reactive.Disposables.Disposable.Create(() => { });
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _container.Dispose();
        }

        [Test]
        public void M()
        {
            Assert.AreSame(_container, _container);
        }
    }
}
