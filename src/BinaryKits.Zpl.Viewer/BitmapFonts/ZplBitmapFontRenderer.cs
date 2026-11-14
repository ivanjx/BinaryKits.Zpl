using SkiaSharp;

using System;
using System.Text;

namespace BinaryKits.Zpl.Viewer.BitmapFonts
{
    public sealed class ZplBitmapFontRenderer
    {
        private readonly SKCanvas canvas;

        public ZplBitmapFontRenderer(SKCanvas canvas)
        {
            this.canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
        }

        /// <summary>
        /// Width of the text: every character advances by the font advance plus <paramref name="characterGap"/> (^FP).
        /// </summary>
        public static int MeasureTextWidth(string text, ZplBitmapFontMetrics metrics, int characterGap = 0)
        {
            ArgumentNullException.ThrowIfNull(metrics);

            if (string.IsNullOrEmpty(text))
            {
                return 0;
            }

            int width = 0;
            foreach (Rune _ in text.EnumerateRunes())
            {
                width += metrics.Advance + characterGap;
            }

            return width;
        }

        public static SKPaint CreatePaint(bool reversePrint)
        {
            return new SKPaint
            {
                IsAntialias = false,
                Style = SKPaintStyle.Fill,
                BlendMode = reversePrint ?
                    SKBlendMode.Xor :
                    SKBlendMode.SrcOver
            };
        }

        /// <summary>
        /// Draws the text with its top-left at (<paramref name="x"/>, <paramref name="y"/>); every character advances
        /// by the font advance plus <paramref name="characterGap"/> (^FP). Returns the advanced width.
        /// </summary>
        public int DrawText(
            string text,
            ZplBitmapFontData fontData,
            ZplBitmapFontMetrics metrics,
            float x,
            float y,
            SKPaint paint,
            int characterGap = 0)
        {
            ArgumentNullException.ThrowIfNull(fontData);
            ArgumentNullException.ThrowIfNull(metrics);
            ArgumentNullException.ThrowIfNull(paint);

            if (string.IsNullOrEmpty(text))
            {
                return 0;
            }

            float currentX = Snap(x);
            float top = Snap(y);
            int renderedWidth = 0;

            foreach (Rune rune in text.EnumerateRunes())
            {
                if (fontData.TryGetGlyph(rune.Value, out ZplBitmapGlyph glyph))
                {
                    this.DrawGlyph(glyph, metrics, currentX, top, paint);
                }

                currentX += metrics.Advance + characterGap;
                renderedWidth += metrics.Advance + characterGap;
            }

            return renderedWidth;
        }

        public void DrawGlyph(
            ZplBitmapGlyph glyph,
            ZplBitmapFontMetrics metrics,
            float x,
            float y,
            SKPaint paint)
        {
            ArgumentNullException.ThrowIfNull(glyph);
            ArgumentNullException.ThrowIfNull(metrics);
            ArgumentNullException.ThrowIfNull(paint);

            float left = Snap(x);
            float top = Snap(y);
            float pixelWidth = SnapDimension(metrics.ExpansionX);
            float pixelHeight = SnapDimension(metrics.ExpansionY);
            int drawWidth = Math.Min(glyph.Width, metrics.MatrixWidth);
            int drawHeight = Math.Min(glyph.Height, metrics.MatrixHeight);

            for (int row = 0; row < drawHeight; row++)
            {
                for (int column = 0; column < drawWidth; column++)
                {
                    if (!glyph.IsSet(column, row))
                    {
                        continue;
                    }

                    this.canvas.DrawRect(
                        left + column * pixelWidth,
                        top + row * pixelHeight,
                        pixelWidth,
                        pixelHeight,
                        paint);
                }
            }
        }

        private static float Snap(float value)
        {
            return MathF.Round(value);
        }

        private static float SnapDimension(float value)
        {
            return Math.Max(1, MathF.Round(value));
        }
    }
}
