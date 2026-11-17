using BinaryKits.Zpl.Label.Elements;
using BinaryKits.Zpl.Viewer.Helpers;

using SkiaSharp;

using System;
using System.Collections.Generic;
using System.Text;

namespace BinaryKits.Zpl.Viewer.DownloadedFonts
{
    /// <summary>
    /// Measures and draws one field's text with a resolved downloaded font at the field's effective size, including
    /// ^FL link fallback for missing glyphs. Text is the decoded Unicode string (after ^FH/^CI); one rune = one glyph,
    /// no shaping, no kerning.
    /// </summary>
    /// <remarks>
    /// A missing glyph is looked up in the base font, then in the ^FL extension fonts in link order, and otherwise
    /// prints no ink and advances like a space of the base font. Linked fonts are sized from the field's h/w by their
    /// own rules (outline: em = h; .FNT: integer magnification of their own cell) and share the base font's baseline.
    /// A space the base font has no glyph for always takes the base font's space advance.
    /// </remarks>
    public sealed class ZplDownloadedFontText : IDisposable
    {
        private const int Space = ' ';

        private readonly SizedFont baseFont;
        private readonly List<SizedFont> linkedFonts;

        private ZplDownloadedFontText(ZplDownloadedFont font, SizedFont baseFont, List<SizedFont> linkedFonts)
        {
            this.Font = font;
            this.baseFont = baseFont;
            this.linkedFonts = linkedFonts;
            this.CellHeight = baseFont.CellHeight;
            this.CellWidth = baseFont.CellWidth;
            this.Baseline = baseFont.Baseline;
        }

        /// <summary>
        /// Resolves <see cref="ZplFont.FontFileName"/> in <paramref name="storage"/> and computes the effective size
        /// (decisions 4–5). <c>false</c> when the font is not downloaded or not usable: the caller then draws with
        /// <see cref="ZplFont.FontName"/> and the original h/w.
        /// </summary>
        public static bool TryCreate(ZplFont font, IPrinterStorage storage, int printDensityDpmm, out ZplDownloadedFontText text)
        {
            text = null;

            if (!ZplDownloadedFontResolver.TryResolve(font, storage, out ZplDownloadedFont downloadedFont))
            {
                return false;
            }

            SizedFont baseFont = SizedFont.Create(downloadedFont, font.FontHeight, font.FontWidth, printDensityDpmm);
            List<SizedFont> linkedFonts = [];

            PrinterObjectName baseName = PrinterObjectName.Parse(font.FontFileName, null);
            foreach (string linkKey in storage.GetFontLinks(baseName.Key))
            {
                if (ZplDownloadedFontResolver.TryResolve(linkKey, storage, out ZplDownloadedFont linkedFont) &&
                    !ReferenceEquals(linkedFont, downloadedFont))
                {
                    linkedFonts.Add(SizedFont.Create(linkedFont, font.FontHeight, font.FontWidth, printDensityDpmm));
                }
            }

            text = new ZplDownloadedFontText(downloadedFont, baseFont, linkedFonts);
            return true;
        }

        /// <summary>
        /// The resolved base font.
        /// </summary>
        public ZplDownloadedFont Font { get; private set; }

        /// <summary>
        /// Height of one text line without ^FB/^TB spacing: <c>cellHeight·magY</c> (.FNT) or <c>h</c> (outline).
        /// </summary>
        public float CellHeight { get; private set; }

        /// <summary>
        /// Width of one character cell: <c>cellWidth·magX</c> (.FNT) or the advance of <c>W</c> (outline, <c>w</c>
        /// without a <c>W</c> glyph). R fields anchor on it like the resident fonts.
        /// </summary>
        public float CellWidth { get; private set; }

        /// <summary>
        /// Distance from the cell top to the baseline: <c>baseline·magY</c> (.FNT) or <c>floor(3h/4)</c> (outline).
        /// ^FO places the cell top on y; ^FT places the baseline on y.
        /// </summary>
        public float Baseline { get; private set; }

        /// <summary>
        /// Width of <paramref name="text"/>: the sum of glyph advances (missing glyphs advance like a space) plus
        /// <paramref name="characterGap"/> after every rune, the same convention as
        /// <see cref="BitmapFonts.ZplBitmapFontRenderer.MeasureTextWidth"/>.
        /// </summary>
        public float MeasureText(string text, float characterGap = 0)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0;
            }

            float width = 0;

            foreach (Rune rune in text.EnumerateRunes())
            {
                this.FindGlyph(rune.Value, out float advance);
                width += advance + characterGap;
            }

            return width;
        }

        /// <summary>
        /// Draws <paramref name="text"/> with the cell top at <paramref name="top"/> and the pen starting at
        /// <paramref name="x"/>, 1-bit. Returns the advanced width (equal to <see cref="MeasureText"/>).
        /// </summary>
        public float DrawText(SKCanvas canvas, string text, float x, float top, SKPaint paint, float characterGap = 0)
        {
            ArgumentNullException.ThrowIfNull(canvas);
            ArgumentNullException.ThrowIfNull(paint);

            if (string.IsNullOrEmpty(text))
            {
                return 0;
            }

            float baselineY = top + this.Baseline;
            float pen = 0;

            foreach (Rune rune in text.EnumerateRunes())
            {
                SizedFont font = this.FindGlyph(rune.Value, out float advance);
                font?.DrawGlyph(canvas, rune.Value, x + pen, baselineY, paint);

                pen += advance + characterGap;
            }

            return pen;
        }

        /// <summary>
        /// 1-bit fill paint; XOR blend for ^FR.
        /// </summary>
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

        public void Dispose()
        {
            this.baseFont.Dispose();

            foreach (SizedFont linkedFont in this.linkedFonts)
            {
                linkedFont.Dispose();
            }

            this.linkedFonts.Clear();
        }

        /// <summary>
        /// Font that draws <paramref name="codePoint"/> (base, then links), or <c>null</c> when none has a glyph and the
        /// rune advances like a space of the base font.
        /// </summary>
        private SizedFont FindGlyph(int codePoint, out float advance)
        {
            if (this.baseFont.TryGetAdvance(codePoint, out advance))
            {
                return this.baseFont;
            }

            if (codePoint != Space)
            {
                foreach (SizedFont linkedFont in this.linkedFonts)
                {
                    if (linkedFont.TryGetAdvance(codePoint, out advance))
                    {
                        return linkedFont;
                    }
                }
            }

            advance = this.baseFont.SpaceAdvance;
            return null;
        }

        /// <summary>
        /// Snaps a glyph origin to whole device dots (half up), like Skia places font 0 glyphs, so rotated fields and
        /// fractional pens ink the same dots as unrotated ones.
        /// </summary>
        private static SKPoint SnapToDevice(SKCanvas canvas, float x, float y)
        {
            SKMatrix matrix = canvas.TotalMatrix;

            if (matrix.IsIdentity)
            {
                return new SKPoint(Round(x), Round(y));
            }

            if (!matrix.TryInvert(out SKMatrix inverse))
            {
                return new SKPoint(x, y);
            }

            SKPoint device = matrix.MapPoint(x, y);
            return inverse.MapPoint(Round(device.X), Round(device.Y));
        }

        private static float Round(float value)
        {
            return MathF.Floor(value + 0.5f);
        }

        /// <summary>
        /// A downloaded font at the field's effective size.
        /// </summary>
        private abstract class SizedFont : IDisposable
        {
            public float CellHeight { get; protected set; }

            public float CellWidth { get; protected set; }

            public float Baseline { get; protected set; }

            public float SpaceAdvance { get; protected set; }

            public static SizedFont Create(ZplDownloadedFont font, int fontHeight, int fontWidth, int printDensityDpmm)
            {
                return font switch
                {
                    ZplDownloadedBitmapFont bitmapFont => new SizedBitmapFont(bitmapFont, fontHeight, fontWidth),
                    ZplDownloadedOutlineFont outlineFont => new SizedOutlineFont(outlineFont, fontHeight, fontWidth, printDensityDpmm),
                    _ => throw new NotSupportedException($"Unsupported downloaded font {font.GetType().Name}.")
                };
            }

            /// <summary>
            /// Advance of the glyph for <paramref name="codePoint"/>; <c>false</c> when the font has no glyph for it.
            /// </summary>
            public abstract bool TryGetAdvance(int codePoint, out float advance);

            /// <summary>
            /// Draws the glyph for <paramref name="codePoint"/> with the pen at <paramref name="penX"/> on the
            /// baseline <paramref name="baselineY"/>.
            /// </summary>
            public abstract void DrawGlyph(SKCanvas canvas, int codePoint, float penX, float baselineY, SKPaint paint);

            public virtual void Dispose()
            {
            }
        }

        /// <summary>
        /// ~DB font at integer magnification per axis (decision 5): <c>clamp(round_half_away(v / cell), 1, 10)</c>, a
        /// missing axis takes the other axis' magnification, both missing → 1×.
        /// </summary>
        private sealed class SizedBitmapFont : SizedFont
        {
            private readonly ZplDownloadedBitmapFont font;
            private readonly int magnificationX;
            private readonly int magnificationY;

            public SizedBitmapFont(ZplDownloadedBitmapFont font, int fontHeight, int fontWidth)
            {
                this.font = font;

                int magnificationY = fontHeight > 0 ? FontScale.GetMagnification(fontHeight, font.CellHeight) : 0;
                int magnificationX = fontWidth > 0 ? FontScale.GetMagnification(fontWidth, font.CellWidth) : 0;

                if (magnificationY == 0)
                {
                    magnificationY = magnificationX > 0 ? magnificationX : 1;
                }

                if (magnificationX == 0)
                {
                    magnificationX = magnificationY;
                }

                this.magnificationX = magnificationX;
                this.magnificationY = magnificationY;

                this.CellHeight = font.CellHeight * magnificationY;
                this.CellWidth = font.CellWidth * magnificationX;
                this.Baseline = font.Baseline * magnificationY;
                this.SpaceAdvance = font.SpaceWidth * magnificationX;
            }

            public override bool TryGetAdvance(int codePoint, out float advance)
            {
                if (this.font.TryGetGlyph(codePoint, out ZplDownloadedBitmapGlyph glyph))
                {
                    advance = glyph.Advance * this.magnificationX;
                    return true;
                }

                advance = 0;
                return false;
            }

            public override void DrawGlyph(SKCanvas canvas, int codePoint, float penX, float baselineY, SKPaint paint)
            {
                if (!this.font.TryGetGlyph(codePoint, out ZplDownloadedBitmapGlyph glyph))
                {
                    return;
                }

                // yOffset is the glyph's ascent above the baseline; ink never starts above the cell top
                float top = baselineY - this.Baseline;
                float inkLeft = penX + glyph.XOffset * this.magnificationX;
                float inkTop = top + Math.Max(0, this.font.Baseline - glyph.YOffset) * this.magnificationY;

                SKPoint origin = SnapToDevice(canvas, inkLeft, inkTop);

                for (int row = 0; row < glyph.Height; row++)
                {
                    int column = 0;

                    while (column < glyph.Width)
                    {
                        if (!glyph.IsSet(column, row))
                        {
                            column++;
                            continue;
                        }

                        int runStart = column;
                        while (column < glyph.Width && glyph.IsSet(column, row))
                        {
                            column++;
                        }

                        canvas.DrawRect(
                            origin.X + runStart * this.magnificationX,
                            origin.Y + row * this.magnificationY,
                            (column - runStart) * this.magnificationX,
                            this.magnificationY,
                            paint);
                    }
                }
            }
        }

        /// <summary>
        /// TrueType/OpenType font (decision 4): em = h dots, horizontal scale w/h, a missing axis takes the other,
        /// both missing → the font A base cell; baseline <c>floor(3h/4)</c> below the cell top; unhinted proportional
        /// advances with a fractional pen; 1-bit glyphs.
        /// </summary>
        private sealed class SizedOutlineFont : SizedFont
        {
            private const string DefaultFontName = "A";
            private const int DefaultCellHeight = 9;
            private const int DefaultCellWidth = 5;

            private readonly SKFont font;
            private readonly Dictionary<int, (ushort Glyph, float Advance)> glyphs = [];

            public SizedOutlineFont(ZplDownloadedOutlineFont font, int fontHeight, int fontWidth, int printDensityDpmm)
            {
                int height = fontHeight > 0 ? fontHeight : fontWidth;
                int width = fontWidth > 0 ? fontWidth : fontHeight;

                if (height <= 0)
                {
                    (height, width) = FontScale.GetMatrixSize(DefaultFontName, printDensityDpmm) ?? (DefaultCellHeight, DefaultCellWidth);
                }

                // Linear (unrounded) hmtx advances give the fractional pen; Skia rounds them otherwise
                this.font = new SKFont(font.Typeface, height)
                {
                    ScaleX = (float)width / height,
                    Edging = SKFontEdging.Alias,
                    Hinting = SKFontHinting.None,
                    Subpixel = true,
                    LinearMetrics = true
                };

                this.CellHeight = height;
                this.Baseline = MathF.Floor(height * 3 / 4f);

                // Missing glyphs advance like the space glyph (decision 6); .notdef's advance without one
                (_, float spaceAdvance) = this.GetGlyph(Space);
                this.SpaceAdvance = spaceAdvance;

                (ushort wGlyph, float wAdvance) = this.GetGlyph('W');
                this.CellWidth = wGlyph != 0 ? wAdvance : width;
            }

            public override bool TryGetAdvance(int codePoint, out float advance)
            {
                (ushort glyph, advance) = this.GetGlyph(codePoint);
                return glyph != 0;
            }

            public override void DrawGlyph(SKCanvas canvas, int codePoint, float penX, float baselineY, SKPaint paint)
            {
                (ushort glyph, _) = this.GetGlyph(codePoint);
                if (glyph == 0)
                {
                    return;
                }

                SKPoint origin = SnapToDevice(canvas, penX, baselineY);

                using SKTextBlobBuilder builder = new();
                SKRunBuffer run = builder.AllocateRun(this.font, 1, 0, 0);
                run.SetGlyphs([glyph]);

                using SKTextBlob textBlob = builder.Build();
                canvas.DrawText(textBlob, origin.X, origin.Y, paint);
            }

            public override void Dispose()
            {
                this.font.Dispose();
            }

            private (ushort Glyph, float Advance) GetGlyph(int codePoint)
            {
                if (!this.glyphs.TryGetValue(codePoint, out (ushort Glyph, float Advance) entry))
                {
                    ushort glyph = this.font.GetGlyph(codePoint);
                    float[] advances = this.font.GetGlyphWidths([glyph]);

                    entry = (glyph, advances[0]);
                    this.glyphs[codePoint] = entry;
                }

                return entry;
            }
        }
    }
}
