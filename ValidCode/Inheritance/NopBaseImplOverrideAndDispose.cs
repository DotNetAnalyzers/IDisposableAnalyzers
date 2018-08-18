namespace ValidCode.Inheritance
{
    using System.IO;

    class NopBaseImplOverrideAndDispose : NopBase
    {
        private readonly Stream stream = File.OpenRead(string.Empty);

        protected override void Dispose(bool disposing)
        {
            this.stream.Dispose();
            base.Dispose(disposing);
        }
    }
}