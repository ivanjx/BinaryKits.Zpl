using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace BinaryKits.Zpl.Viewer.BitmapFonts
{
    public sealed class ZplBitmapFontData
    {
        private readonly IReadOnlyDictionary<int, ZplBitmapGlyph> glyphs;

        public ZplBitmapFontData(
            ZplBitmapFontMetrics metrics,
            IReadOnlyDictionary<int, ZplBitmapGlyph> glyphs)
        {
            this.Metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
            this.glyphs = new ReadOnlyDictionary<int, ZplBitmapGlyph>(
                new Dictionary<int, ZplBitmapGlyph>(glyphs ?? throw new ArgumentNullException(nameof(glyphs))));
        }

        public ZplBitmapFontMetrics Metrics { get; }

        public IReadOnlyDictionary<int, ZplBitmapGlyph> Glyphs => this.glyphs;

        public bool TryGetGlyph(int codePoint, out ZplBitmapGlyph glyph)
        {
            return this.glyphs.TryGetValue(codePoint, out glyph);
        }
    }
}
