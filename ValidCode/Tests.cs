namespace ValidCode
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;

    public static class Tests
    {
        [Test]
        public static void Throws()
        {
            Assert.Throws<InvalidOperationException>(() => Create(true));
            Assert.AreEqual(
                "Expected",
                Assert.Throws<InvalidOperationException>(() => Create(true))?.Message);

            Assert.ThrowsAsync<InvalidOperationException>(() => CreateAsync(true));
            Assert.AreEqual(
                "Expected",
                Assert.ThrowsAsync<InvalidOperationException>(() => CreateAsync(true))?.Message);
        }

        private static Disposable Create(bool b)
        {
            if (b)
            {
                throw new InvalidOperationException("Expected");
            }

            return new Disposable();
        }

        private static async Task<Disposable> CreateAsync(bool b)
        {
            if (b)
            {
                throw new InvalidOperationException("Expected");
            }

            await Task.Delay(1);
            return new Disposable();
        }
    }
}
