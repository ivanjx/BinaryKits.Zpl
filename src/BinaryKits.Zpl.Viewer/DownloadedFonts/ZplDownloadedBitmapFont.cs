using System;
using System.Collections.Generic;

namespace BinaryKits.Zpl.Viewer.DownloadedFonts
{
    /// <summary>
    /// ~DB bitmap font (.FNT): <c>~DBd:o.x,a,h,w,base,space,#char,©,data</c>.
    /// </summary>
    public sealed class ZplDownloadedBitmapFont : ZplDownloadedFont
    {
        private readonly Dictionary<int, ZplDownloadedBitmapGlyph> glyphs;

        public ZplDownloadedBitmapFont(
            int cellHeight,
            int cellWidth,
            int baseline,
            int spaceWidth,
            string copyright,
            IEnumerable<ZplDownloadedBitmapGlyph> glyphs)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cellHeight);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cellWidth);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(baseline);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(baseline, cellHeight);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(spaceWidth);
            ArgumentNullException.ThrowIfNull(glyphs);

            this.CellHeight = cellHeight;
            this.CellWidth = cellWidth;
            this.Baseline = baseline;
            this.SpaceWidth = spaceWidth;
            this.Copyright = copyright ?? string.Empty;
            this.glyphs = [];

            foreach (ZplDownloadedBitmapGlyph glyph in glyphs)
            {
                if (!this.glyphs.TryAdd(glyph.CodePoint, glyph))
                {
                    throw new ArgumentException($"Duplicate glyph code {glyph.CodePoint:X4}.", nameof(glyphs));
                }
            }
        }

        public int CellHeight { get; }

        public int CellWidth { get; }

        /// <summary>
        /// Distance from the cell top to the baseline, in dots.
        /// </summary>
        public int Baseline { get; }

        /// <summary>
        /// Advance of the space character and of code points without a glyph.
        /// </summary>
        public int SpaceWidth { get; }

        public string Copyright { get; }

        public IReadOnlyDictionary<int, ZplDownloadedBitmapGlyph> Glyphs => this.glyphs;

        public bool TryGetGlyph(int codePoint, out ZplDownloadedBitmapGlyph glyph)
        {
            return this.glyphs.TryGetValue(codePoint, out glyph);
        }
    }
}
