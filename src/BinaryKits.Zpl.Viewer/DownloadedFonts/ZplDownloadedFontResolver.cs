using BinaryKits.Zpl.Label.Elements;

namespace BinaryKits.Zpl.Viewer.DownloadedFonts
{
    /// <summary>
    /// Finds the downloaded font a field names (^A@, ^CW) in printer storage. Names match exactly on drive, name and
    /// extension: no cross-drive search, no basename fallback, no extension probing.
    /// </summary>
    public static class ZplDownloadedFontResolver
    {
        /// <summary>
        /// Resolves <see cref="ZplFont.FontFileName"/>. <c>false</c> for resident fonts, unknown names and outline fonts
        /// Skia cannot load: the field then prints with <see cref="ZplFont.FontName"/>.
        /// </summary>
        public static bool TryResolve(ZplFont font, IPrinterStorage storage, out ZplDownloadedFont downloadedFont)
        {
            downloadedFont = null;

            if (font == null)
            {
                return false;
            }

            return TryResolve(font.FontFileName, storage, out downloadedFont);
        }

        /// <summary>
        /// Resolves a <c>D:NAME.EXT</c> font name (a field font or a ^FL link key).
        /// </summary>
        public static bool TryResolve(string fontFileName, IPrinterStorage storage, out ZplDownloadedFont downloadedFont)
        {
            downloadedFont = null;

            if (storage == null || !PrinterObjectName.TryParse(fontFileName, null, out PrinterObjectName name))
            {
                return false;
            }

            if (!storage.TryGetFont(name.Device, name.Name, out ZplDownloadedFont font))
            {
                return false;
            }

            if (font is ZplDownloadedOutlineFont outlineFont && !outlineFont.TryGetTypeface(out _))
            {
                return false;
            }

            downloadedFont = font;
            return true;
        }
    }
}
