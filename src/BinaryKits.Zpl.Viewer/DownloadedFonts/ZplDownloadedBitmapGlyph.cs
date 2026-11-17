using System;

namespace BinaryKits.Zpl.Viewer.DownloadedFonts
{
    /// <summary>
    /// One ~DB glyph record: <c>#code.height.width.xOffset.yOffset.advance.data</c>.
    /// </summary>
    public sealed class ZplDownloadedBitmapGlyph
    {
        private readonly byte[] rows;

        /// <param name="codePoint">Unicode code point (the <c>#xxxx</c> code).</param>
        /// <param name="width">Glyph bitmap width in dots, &gt; 0.</param>
        /// <param name="height">Glyph bitmap height in dots, &gt; 0.</param>
        /// <param name="xOffset">Ink left relative to the pen.</param>
        /// <param name="yOffset">Glyph ascent above the baseline.</param>
        /// <param name="advance">Pen advance including the intercharacter gap, &gt;= 0.</param>
        /// <param name="rows">Top-down rows of <c>ceil(width/8)</c> bytes, MSB first.</param>
        public ZplDownloadedBitmapGlyph(int codePoint, int width, int height, int xOffset, int yOffset, int advance, byte[] rows)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
            ArgumentOutOfRangeException.ThrowIfNegative(advance);
            ArgumentNullException.ThrowIfNull(rows);

            int bytesPerRow = (width + 7) / 8;
            if (rows.Length != bytesPerRow * height)
            {
                throw new ArgumentException($"Glyph data must be {bytesPerRow * height} bytes.", nameof(rows));
            }

            this.CodePoint = codePoint;
            this.Width = width;
            this.Height = height;
            this.XOffset = xOffset;
            this.YOffset = yOffset;
            this.Advance = advance;
            this.BytesPerRow = bytesPerRow;
            this.rows = rows;
        }

        public int CodePoint { get; }

        public int Width { get; }

        public int Height { get; }

        public int XOffset { get; }

        public int YOffset { get; }

        public int Advance { get; }

        public int BytesPerRow { get; }

        public ReadOnlySpan<byte> Rows => this.rows;

        public bool IsSet(int x, int y)
        {
            return (this.rows[y * this.BytesPerRow + (x >> 3)] & (0x80 >> (x & 7))) != 0;
        }
    }
}
