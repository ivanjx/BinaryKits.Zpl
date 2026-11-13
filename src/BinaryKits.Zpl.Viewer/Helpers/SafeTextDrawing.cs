using SkiaSharp;
using SkiaSharp.HarfBuzz;

using System;
using System.Runtime.CompilerServices;

namespace BinaryKits.Zpl.Viewer.Helpers
{
    /// <summary>
    /// Draws and measures text shaped by HarfBuzz with plain glyph advances: Labelary applies neither kerning nor
    /// ligatures to font 0 (Triumvirate's <c>AV</c>, <c>Ag</c>, <c>To</c> kern pairs and <c>fi</c>/<c>fl</c> ligatures
    /// do not appear). Measuring and drawing use the same glyph positions, so wrapping, justification and alignment
    /// agree with the drawn text. Typefaces without font data fall back to unshaped Skia text.
    /// </summary>
    public static class SafeTextDrawing
    {
        private static readonly HarfBuzzSharp.Feature[] shapingFeatures =
        [
            HarfBuzzSharp.Feature.Parse("kern=0"),
            HarfBuzzSharp.Feature.Parse("liga=0")
        ];

        private static readonly ConditionalWeakTable<SKTypeface, ShapingFont> shapingFonts = new();

        public static void DrawShapedTextSafe(
            this SKCanvas canvas,
            string text,
            float x,
            float y,
            SKTextAlign textAlign,
            SKFont font,
            SKPaint paint)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            if (!TryShapeText(text, font, out ushort[] glyphs, out SKPoint[] positions, out float width))
            {
                canvas.DrawText(text, x, y, textAlign, font, paint);
                return;
            }

            float alignOffset = textAlign switch
            {
                SKTextAlign.Center => width / 2,
                SKTextAlign.Right => width,
                _ => 0
            };

            using SKTextBlobBuilder builder = new();
            SKPositionedRunBuffer run = builder.AllocatePositionedRun(font, glyphs.Length);
            run.SetGlyphs(glyphs);
            run.SetPositions(positions);

            using SKTextBlob textBlob = builder.Build();
            canvas.DrawText(textBlob, x - alignOffset, y, paint);
        }

        /// <summary>
        /// Advance width of the shaped text, as drawn by <see cref="DrawShapedTextSafe"/>.
        /// </summary>
        public static float MeasureShapedText(this SKFont font, string text)
        {
            return font.MeasureShapedText(text, out _);
        }

        /// <summary>
        /// Advance width and ink bounds (relative to the origin and baseline) of the shaped text, as drawn by
        /// <see cref="DrawShapedTextSafe"/>.
        /// </summary>
        public static float MeasureShapedText(this SKFont font, string text, out SKRect bounds)
        {
            bounds = SKRect.Empty;

            if (string.IsNullOrEmpty(text))
            {
                return 0;
            }

            if (!TryShapeText(text, font, out ushort[] glyphs, out SKPoint[] positions, out float width))
            {
                return font.MeasureText(text, out bounds);
            }

            font.GetGlyphWidths(glyphs, out SKRect[] glyphBounds, null);

            for (int i = 0; i < glyphs.Length; i++)
            {
                SKRect glyphBound = glyphBounds[i];
                if (glyphBound.IsEmpty)
                {
                    continue;
                }

                glyphBound.Offset(positions[i]);
                bounds = bounds.IsEmpty ? glyphBound : SKRect.Union(bounds, glyphBound);
            }

            return width;
        }

        private static bool TryShapeText(
            string text,
            SKFont font,
            out ushort[] glyphs,
            out SKPoint[] positions,
            out float width)
        {
            glyphs = null;
            positions = null;
            width = 0;

            if (font.Typeface == null)
            {
                return false;
            }

            ShapingFont shapingFont = shapingFonts.GetValue(font.Typeface, CreateShapingFont);
            if (shapingFont.Font == null)
            {
                return false;
            }

            using HarfBuzzSharp.Buffer buffer = new();
            buffer.AddUtf16(text);
            buffer.GuessSegmentProperties();
            shapingFont.Font.Shape(buffer, shapingFeatures);

            ReadOnlySpan<HarfBuzzSharp.GlyphInfo> glyphInfos = buffer.GetGlyphInfoSpan();
            ReadOnlySpan<HarfBuzzSharp.GlyphPosition> glyphPositions = buffer.GetGlyphPositionSpan();

            // The HarfBuzz font is scaled to one unit per font unit
            float scaleY = font.Size / shapingFont.UnitsPerEm;
            float scaleX = scaleY * font.ScaleX;

            glyphs = new ushort[glyphInfos.Length];
            positions = new SKPoint[glyphInfos.Length];
            float penX = 0;
            float penY = 0;

            for (int i = 0; i < glyphInfos.Length; i++)
            {
                glyphs[i] = (ushort)glyphInfos[i].Codepoint;
                positions[i] = new SKPoint(
                    penX + glyphPositions[i].XOffset * scaleX,
                    penY - glyphPositions[i].YOffset * scaleY);

                penX += glyphPositions[i].XAdvance * scaleX;
                penY += glyphPositions[i].YAdvance * scaleY;
            }

            width = penX;

            return true;
        }

        private static ShapingFont CreateShapingFont(SKTypeface typeface)
        {
            SKStreamAsset stream = typeface.OpenStream(out int index);
            if (stream == null)
            {
                return new ShapingFont(null, 0);
            }

            int unitsPerEm = typeface.UnitsPerEm;

            using HarfBuzzSharp.Blob blob = stream.ToHarfBuzzBlob();
            using HarfBuzzSharp.Face face = new(blob, index)
            {
                UnitsPerEm = unitsPerEm
            };

            HarfBuzzSharp.Font font = new(face);
            font.SetScale(unitsPerEm, unitsPerEm);
            font.SetFunctionsOpenType();

            return new ShapingFont(font, unitsPerEm);
        }

        /// <summary>
        /// HarfBuzz font of a typeface, cached for the typeface's lifetime; <see cref="Font"/> is null for typefaces
        /// without font data. HarfBuzz fonts are safe to shape with from several threads.
        /// </summary>
        private sealed class ShapingFont
        {
            public HarfBuzzSharp.Font Font { get; }

            public int UnitsPerEm { get; }

            public ShapingFont(HarfBuzzSharp.Font font, int unitsPerEm)
            {
                this.Font = font;
                this.UnitsPerEm = unitsPerEm;
            }
        }
    }
}
