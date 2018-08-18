namespace ValidCode.Inheritance
{
    class NopBaseImplOverride : NopBase
    {
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}