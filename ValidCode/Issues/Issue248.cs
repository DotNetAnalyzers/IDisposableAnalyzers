// ReSharper disable All
namespace ValidCode
{
    using System;
    using System.Drawing;

    public class Issue248
    {
        public static Bitmap Diff(Bitmap expected, Bitmap actual)
        {
            var diff = new Bitmap(Math.Min(expected.Width, actual.Width), Math.Min(expected.Height, actual.Height));
            for (var x = 0; x < diff.Size.Width; x++)
            {
                for (var y = 0; y < diff.Size.Height; y++)
                {
                    var ep = expected.GetPixel(x, y);
                    var ap = actual.GetPixel(x, y);
                    var color = ep.A != ap.A
                        ? System.Drawing.Color.HotPink
                        : System.Drawing.Color.FromArgb(
                            Diff(x => x.R),
                            Diff(x => x.G),
                            Diff(x => x.B));
                    diff.SetPixel(x, y, color);

                    int Diff(Func<Color, byte> func)
                    {
                        return Math.Abs(func(ep) - func(ap));
                    }
                }
            }

            return diff;
        }
    }
}
