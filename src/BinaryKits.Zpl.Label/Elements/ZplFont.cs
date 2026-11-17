using System.Collections.Generic;

namespace BinaryKits.Zpl.Label.Elements
{
    /// <summary>
    /// ^A – Scalable/Bitmapped Font
    /// </summary>
    public class ZplFont : ZplElementBase
    {
        /// <summary>
        /// Any font in the printer (downloaded, EPROM, stored fonts, fonts A through Z and 0 to 9). For downloaded
        /// fonts this is the fallback letter: the ^CF font at selection time.
        /// </summary>
        public string FontName { get; private set; }

        /// <summary>
        /// Normalized <c>D:NAME.EXT</c> of a downloaded font (^A@, or a ^CW alias), <c>null</c> for resident fonts.
        /// When it does not resolve, the field prints with <see cref="FontName"/>.
        /// </summary>
        public string FontFileName { get; private set; }
        public FieldOrientation FieldOrientation { get; private set; }
        public int FontWidth { get; private set; }
        public int FontHeight { get; private set; }

        public ZplFont(
            int fontWidth = 30,
            int fontHeight = 30,
            string fontName = "0",
            FieldOrientation fieldOrientation = FieldOrientation.Normal,
            string fontFileName = null)
        {
            FontName = fontName;
            FontFileName = fontFileName;
            FieldOrientation = fieldOrientation;
            FontWidth = fontWidth;
            FontHeight = fontHeight;
        }

        ///<inheritdoc/>
        public override IEnumerable<string> Render(ZplRenderOptions context)
        {
            if (FontFileName != null)
            {
                return new[] { $"^A@{RenderFieldOrientation(FieldOrientation)},{context.Scale(FontHeight)},{context.Scale(FontWidth)},{FontFileName}" };
            }

            return new[] { $"^A{FontName}{RenderFieldOrientation(FieldOrientation)},{context.Scale(FontHeight)},{context.Scale(FontWidth)}" };
        }
    }
}
