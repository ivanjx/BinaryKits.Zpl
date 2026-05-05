using System;

namespace BinaryKits.Zpl.Viewer.BitmapFonts
{
    public sealed class ZplBitmapFontMetrics
    {
        public ZplBitmapFontMetrics(
            string name,
            int matrixWidth,
            int matrixHeight,
            int intercharacterGap,
            int baseline,
            int expansionX = 1,
            int expansionY = 1,
            int lineSpacing = 0)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Font name is required.", nameof(name));
            }

            if (matrixWidth <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(matrixWidth), matrixWidth, "Matrix width must be positive.");
            }

            if (matrixHeight <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(matrixHeight), matrixHeight, "Matrix height must be positive.");
            }

            if (intercharacterGap < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(intercharacterGap), intercharacterGap, "Intercharacter gap cannot be negative.");
            }

            if (baseline < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(baseline), baseline, "Baseline cannot be negative.");
            }

            if (expansionX <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(expansionX), expansionX, "Horizontal expansion must be positive.");
            }

            if (expansionY <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(expansionY), expansionY, "Vertical expansion must be positive.");
            }

            this.Name = name;
            this.MatrixWidth = matrixWidth;
            this.MatrixHeight = matrixHeight;
            this.IntercharacterGap = intercharacterGap;
            this.Baseline = baseline;
            this.ExpansionX = expansionX;
            this.ExpansionY = expansionY;
            this.LineSpacing = lineSpacing;
        }

        public string Name { get; }

        public int MatrixWidth { get; }

        public int MatrixHeight { get; }

        public int IntercharacterGap { get; }

        public int Baseline { get; }

        public int ExpansionX { get; }

        public int ExpansionY { get; }

        public int LineSpacing { get; }

        public int Advance => (this.MatrixWidth + this.IntercharacterGap) * this.ExpansionX;

        public int RenderedGlyphWidth => this.MatrixWidth * this.ExpansionX;

        public int RenderedGlyphHeight => this.MatrixHeight * this.ExpansionY;

        public int LineHeight => this.RenderedGlyphHeight + this.LineSpacing;

        public ZplBitmapFontMetrics WithExpansion(int expansionX, int expansionY, int lineSpacing = 0)
        {
            return new ZplBitmapFontMetrics(
                this.Name,
                this.MatrixWidth,
                this.MatrixHeight,
                this.IntercharacterGap,
                this.Baseline,
                expansionX,
                expansionY,
                lineSpacing);
        }
    }
}

