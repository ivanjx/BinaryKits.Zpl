using BinaryKits.Zpl.Label;

namespace BinaryKits.Zpl.Viewer.Models
{
    public class FontInfo
    {
        public string FontName { get; private set; }
        public FieldOrientation FieldOrientation { get; private set; }
        public int FontWidth { get; private set; }
        public int FontHeight { get; private set; }

        /// <summary>
        /// Normalized <c>D:NAME.EXT</c> of a downloaded font (^A@, or a ^CW alias), <c>null</c> for resident fonts
        /// </summary>
        public string FontFileName { get; private set; }

        public FontInfo(string fontName, FieldOrientation fieldOrientation, int fontWidth, int fontHeight, string fontFileName = null)
        {
            this.FontName = fontName;
            this.FieldOrientation = fieldOrientation;
            this.FontWidth = fontWidth;
            this.FontHeight = fontHeight;
            this.FontFileName = fontFileName;
        }
    }
}
