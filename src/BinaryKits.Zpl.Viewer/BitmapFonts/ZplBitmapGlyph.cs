using System;

namespace BinaryKits.Zpl.Viewer.BitmapFonts
{
    public sealed class ZplBitmapGlyph
    {
        private readonly bool[] pixels;

        public ZplBitmapGlyph(int codePoint, int width, int height, bool[] pixels)
        {
            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width), width, "Glyph width must be positive.");
            }

            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height), height, "Glyph height must be positive.");
            }

            ArgumentNullException.ThrowIfNull(pixels);

            if (pixels.Length != width * height)
            {
                throw new ArgumentException("Pixel data length must equal width multiplied by height.", nameof(pixels));
            }

            this.CodePoint = codePoint;
            this.Width = width;
            this.Height = height;
            this.pixels = (bool[])pixels.Clone();
        }

        public int CodePoint { get; }

        public int Width { get; }

        public int Height { get; }

        public bool IsSet(int x, int y)
        {
            if (x < 0 || x >= this.Width)
            {
                throw new ArgumentOutOfRangeException(nameof(x), x, "X coordinate is outside the glyph.");
            }

            if (y < 0 || y >= this.Height)
            {
                throw new ArgumentOutOfRangeException(nameof(y), y, "Y coordinate is outside the glyph.");
            }

            return this.pixels[(y * this.Width) + x];
        }

        public bool[] GetPixels()
        {
            return (bool[])this.pixels.Clone();
        }
    }
}

