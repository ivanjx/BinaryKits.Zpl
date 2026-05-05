using BinaryKits.Zpl.Label;

namespace BinaryKits.Zpl.Viewer.BitmapFonts
{
    public sealed class EmptyZplBitmapFontProvider : IZplBitmapFontProvider
    {
        public static EmptyZplBitmapFontProvider Instance { get; } = new EmptyZplBitmapFontProvider();

        private EmptyZplBitmapFontProvider()
        {
        }

        public bool TryGet(
            string fontName,
            int printDensityDpmm,
            InternationalFont internationalFont,
            out ZplBitmapFontData font)
        {
            font = null;
            return false;
        }
    }
}

