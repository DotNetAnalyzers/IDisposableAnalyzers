namespace IDisposableAnalyzers.Test.Helpers
{
    using Microsoft.CodeAnalysis.CSharp;
    using NUnit.Framework;

    public class RecursionLoopTests
    {
        [Test]
        public void OneItem()
        {
            var a = SyntaxFactory.IdentifierName("a");
            var b = SyntaxFactory.IdentifierName("b");
            var loop = new RecursionLoop();
            Assert.AreEqual(true,  loop.Add(a));
            Assert.AreEqual(false, loop.Add(a));
            Assert.AreEqual(false, loop.Add(a));
            Assert.AreEqual(true,  loop.Add(b));
            Assert.AreEqual(false, loop.Add(b));
            Assert.AreEqual(false, loop.Add(b));
        }

        [Test]
        public void TwoItems()
        {
            var a = SyntaxFactory.IdentifierName("a");
            var b = SyntaxFactory.IdentifierName("b");
            var loop = new RecursionLoop();
            Assert.AreEqual(true,  loop.Add(a));
            Assert.AreEqual(true,  loop.Add(b));
            Assert.AreEqual(true,  loop.Add(a));
            Assert.AreEqual(false, loop.Add(b));
            Assert.AreEqual(false, loop.Add(a));
            Assert.AreEqual(false, loop.Add(a));
            Assert.AreEqual(true,  loop.Add(b));
            Assert.AreEqual(false, loop.Add(b));
        }

        [Test]
        public void TwoItemsPrefixed()
        {
            var a = SyntaxFactory.IdentifierName("a");
            var b = SyntaxFactory.IdentifierName("b");
            var c = SyntaxFactory.IdentifierName("c");
            var loop = new RecursionLoop();
            Assert.AreEqual(true, loop.Add(a));
            Assert.AreEqual(true, loop.Add(b));
            Assert.AreEqual(true, loop.Add(c));
            Assert.AreEqual(true, loop.Add(b));
            Assert.AreEqual(false, loop.Add(c));
        }

        [Test]
        public void ThreeItems()
        {
            var a = SyntaxFactory.IdentifierName("a");
            var b = SyntaxFactory.IdentifierName("b");
            var c = SyntaxFactory.IdentifierName("c");
            var loop = new RecursionLoop();
            Assert.AreEqual(true,  loop.Add(a));
            Assert.AreEqual(false, loop.Add(a));
            Assert.AreEqual(true,  loop.Add(b));
            Assert.AreEqual(true,  loop.Add(c));
            Assert.AreEqual(true,  loop.Add(a));
            Assert.AreEqual(true,  loop.Add(b));
            Assert.AreEqual(false, loop.Add(c));
        }
    }
}
