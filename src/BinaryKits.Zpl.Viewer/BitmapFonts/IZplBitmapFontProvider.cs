using BinaryKits.Zpl.Label;

namespace BinaryKits.Zpl.Viewer.BitmapFonts
{
    public interface IZplBitmapFontProvider
    {
        bool TryGet(
            string fontName,
            int printDensityDpmm,
            InternationalFont internationalFont,
            out ZplBitmapFontData font);
    }
}

