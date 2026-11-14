using SkiaSharp;

using System;
using System.Collections.Generic;

namespace BinaryKits.Zpl.Viewer.Helpers
{
    using FontScaleDictionary = Dictionary<string, (int height, int width)>;

    public static class FontScale
    {
        private static readonly FontScaleDictionary fontScales6mm = new()
        {
            ["A"] = (9, 5),
            ["B"] = (11, 7),
            ["C"] = (18, 10),
            ["D"] = (18, 10),
            ["E"] = (21, 10),
            ["F"] = (26, 13),
            ["G"] = (60, 40),
            ["H"] = (17, 11),
            ["GS"] = (24, 24)
        };

        private static readonly FontScaleDictionary fontScales8mm = new()
        {
            ["A"] = (9, 5),
            ["B"] = (11, 7),
            ["C"] = (18, 10),
            ["D"] = (18, 10),
            ["E"] = (28, 15),
            ["F"] = (26, 13),
            ["G"] = (60, 40),
            ["H"] = (21, 13),
            ["GS"] = (24, 24)
        };

        private static readonly FontScaleDictionary fontScales12mm = new()
        {
            ["A"] = (9, 5),
            ["B"] = (11, 7),
            ["C"] = (18, 10),
            ["D"] = (18, 10),
            ["E"] = (42, 20),
            ["F"] = (26, 13),
            ["G"] = (60, 40),
            ["H"] = (34, 22),
            ["GS"] = (24, 24)
        };

        private static readonly FontScaleDictionary fontScales24mm = new()
        {
            ["A"] = (9, 5),
            ["B"] = (11, 7),
            ["C"] = (18, 10),
            ["D"] = (18, 10),
            ["E"] = (42, 20),
            ["F"] = (26, 13),
            ["G"] = (60, 40),
            ["H"] = (34, 22),
            ["GS"] = (24, 24)
        };

        /// <summary>
        /// Resident fonts P–V at 1×, the same at every density: matrix height and width, baseline below the cell top,
        /// <c>^FB</c> line height, and the em height and width in dots of the scalable font 0 typeface they are drawn with.
        /// Measured on Labelary (6/8/12 dpmm): the matrix magnifies per axis like A–H, but an omitted axis stays 1×; the
        /// em comes from the cap height (vertical) and the advance of repeated glyphs (horizontal), which differ for
        /// R–V; <c>^FB</c> lines are closer than the cell and keep its descent below the baseline.
        /// </summary>
        private static readonly Dictionary<string, MatrixScalableFont> matrixScalableFonts = new()
        {
            ["P"] = new(20, 18, 15, 17, 16.12f, 16.12f),
            ["Q"] = new(28, 24, 21, 24, 22.92f, 22.67f),
            ["R"] = new(35, 31, 27, 30, 29.90f, 29.27f),
            ["S"] = new(40, 35, 31, 34, 33.69f, 33.31f),
            ["T"] = new(48, 42, 37, 40, 40.67f, 39.40f),
            ["U"] = new(59, 53, 46, 50, 50.02f, 49.99f),
            ["V"] = new(80, 71, 62, 67, 67.27f, 66.53f)
        };

        private readonly record struct MatrixScalableFont(
            int Height,
            int Width,
            int Baseline,
            int FieldBlockLineHeight,
            float EmHeight,
            float EmWidth);

        private static (int height, int width)? GetFontScale(string fontName, int printDensityDpmm)
        {
            FontScaleDictionary dict;
            switch (printDensityDpmm)
            {
                case 6:
                    dict = fontScales6mm;
                    break;
                case 8:
                    dict = fontScales8mm;
                    break;
                case 12:
                    dict = fontScales12mm;
                    break;
                case 24:
                    dict = fontScales24mm;
                    break;
                default:
                    return null;
            }

            if (dict.TryGetValue(fontName, out (int height, int width) value))
            {
                return value;
            }

            return null;
        }

        /// <summary>
        /// Scalable font height and width range in dots (Zebra <c>^A</c>: 10 to 32000).
        /// </summary>
        private const int scalableMinSize = 10;
        private const int scalableMaxSize = 32000;

        /// <summary>
        /// Scalable font baseline as a fraction of the cell height: Zebra's spacing table puts the font 0 baseline at
        /// 3 × HEIGHT / 4 (Triumvirate cap height 750/1000).
        /// </summary>
        private const int scalableBaselineNumerator = 3;
        private const int scalableBaselineDenominator = 4;

        /// <summary>
        /// Scalable font em size as a fraction of the cell height. Labelary draws font 0 slightly smaller than an em of
        /// HEIGHT: glyph advances measure 0.9958–0.9967 of the Triumvirate advances at every height and width, the cap
        /// height 0.7475 × HEIGHT. 72/72.27 (PostScript to TeX point) fits both.
        /// </summary>
        private const float scalableEmRatio = 72.0f / 72.27f;

        /// <summary>
        /// Character pitch (matrix width + intercharacter gap) of the A, C and D matrices as a fraction of their height
        /// (6/9, 12/18). Outline substitutes for resident bitmap fonts are monospace, so their advance is fitted to it.
        /// </summary>
        private const float residentPitchRatio = 2.0f / 3.0f;

        /// <summary>
        /// ^TB line pitch of the scalable fonts as a fraction of the cell height (Labelary, font 0 at 10–150 dots).
        /// </summary>
        private const double textBlockScalableLinePitchRatio = 1.26;

        /// <summary>
        /// ^TB line pitch of fonts A–D as a fraction of the cell height (Labelary: A 9 → 9.5, D 18 → 19 dots).
        /// </summary>
        private const double textBlockResidentLinePitchRatio = 19.0 / 18.0;

        /// <summary>
        /// Fonts drawn with the scalable font 0 typeface: font 0 and the resident fonts P–V.
        /// </summary>
        public static bool IsScalableFont(string fontName)
        {
            return fontName == "0" || matrixScalableFonts.ContainsKey(fontName);
        }

        /// <summary>
        /// Cell height in dots of a font matrix at an integer magnification, or null for fonts without a matrix.
        /// </summary>
        public static int? GetCellHeight(string fontName, int magnification, int printDensityDpmm)
        {
            return GetFontScale(fontName, printDensityDpmm)?.height * magnification;
        }

        /// <summary>
        /// Creates the outline font for a text field cell: scalable fonts (0, P–V) by cap height, resident bitmap font
        /// substitutes (A–H) by character pitch. Scalable fonts are rasterized 1-bit (a dot is inked when its center
        /// lies inside the outline), like the printer's thermal head, instead of Labelary's antialiased edges.
        /// </summary>
        public static SKFont CreateTextFont(SKTypeface typeface, string fontName, float cellHeight, float scaleX)
        {
            if (!IsScalableFont(fontName))
            {
                return CreateResidentFont(typeface, cellHeight, scaleX);
            }

            // Triumvirate cap height is 3/4 em
            float emHeight = matrixScalableFonts.TryGetValue(fontName, out MatrixScalableFont matrixFont) ?
                matrixFont.EmHeight * cellHeight / matrixFont.Height :
                cellHeight * scalableEmRatio;

            SKFont font = CreateCapHeightFont(typeface, emHeight * scalableBaselineNumerator / scalableBaselineDenominator, scaleX);
            font.Edging = SKFontEdging.Alias;

            return font;
        }

        /// <summary>
        /// Baseline below the cell top (<c>^FO</c> origin): 3 × HEIGHT / 4 in whole dots for font 0, the magnified
        /// matrix baseline for P–V, the cap height of the substitute typeface for resident bitmap fonts.
        /// </summary>
        public static float GetTextBaseline(SKFont font, string fontName, float cellHeight)
        {
            if (matrixScalableFonts.TryGetValue(fontName, out MatrixScalableFont matrixFont))
            {
                return matrixFont.Baseline * cellHeight / matrixFont.Height;
            }

            if (IsScalableFont(fontName))
            {
                return MathF.Floor(cellHeight * scalableBaselineNumerator / scalableBaselineDenominator);
            }

            font.MeasureText("X", out SKRect capBounds);

            return capBounds.Height;
        }

        /// <summary>
        /// Line cell height of a <c>^FB</c> line (pitch without the line spacing): the cell height, except P–V, whose
        /// lines drop the top of the matrix cell and keep its descent below the baseline.
        /// </summary>
        public static float GetFieldBlockCellHeight(string fontName, float cellHeight)
        {
            return matrixScalableFonts.TryGetValue(fontName, out MatrixScalableFont matrixFont) ?
                matrixFont.FieldBlockLineHeight * cellHeight / matrixFont.Height :
                cellHeight;
        }

        /// <summary>
        /// Top of line <paramref name="lineIndex"/> of a <c>^TB</c> text block below the first line, in whole dots
        /// (half away from zero), for a cell <paramref name="cellHeight"/> dots high. Labelary pitch (the Zebra
        /// documentation leaves it open): 1.26 × the cell for the scalable fonts (0, P–V), 19/18 × the cell for A–D,
        /// the cell for the other fonts.
        /// </summary>
        public static float GetTextBlockLineOffset(string fontName, float cellHeight, int lineIndex)
        {
            double pitchRatio = 1;

            if (IsScalableFont(fontName))
            {
                pitchRatio = textBlockScalableLinePitchRatio;
            }
            else if (fontName is "A" or "B" or "C" or "D")
            {
                pitchRatio = textBlockResidentLinePitchRatio;
            }

            return (float)Math.Round(lineIndex * pitchRatio * cellHeight, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Creates the monospace outline substitute of a resident bitmap font (A–H, interpretation lines): the advance of
        /// "0" in the typeface equals <see cref="residentPitchRatio"/> of the cell height.
        /// </summary>
        public static SKFont CreateResidentFont(SKTypeface typeface, float cellHeight, float scaleX = 1.0f)
        {
            float unitsPerEm = typeface.UnitsPerEm;
            SKFont font = new(typeface, unitsPerEm);

            float advance = font.MeasureText("0");
            font.Size = advance > 0 ?
                cellHeight * residentPitchRatio * unitsPerEm / advance :
                cellHeight;
            font.ScaleX = scaleX;

            return font;
        }

        /// <summary>
        /// Creates an unhinted outline font whose typeface cap height is <paramref name="capHeight"/> dots, matching
        /// Labelary's fractional glyph edges. Typefaces without a cap height metric use an em size of <paramref name="capHeight"/>.
        /// </summary>
        public static SKFont CreateCapHeightFont(SKTypeface typeface, float capHeight, float scaleX = 1.0f)
        {
            // Metrics are read at one dot per font unit; small sizes report pixel-rounded values
            float unitsPerEm = typeface.UnitsPerEm;
            SKFont font = new(typeface, unitsPerEm, scaleX)
            {
                Hinting = SKFontHinting.None
            };

            float typefaceCapHeight = font.Metrics.CapHeight;
            font.Size = typefaceCapHeight > 0 ?
                capHeight * unitsPerEm / typefaceCapHeight :
                capHeight;

            return font;
        }

        /// <summary>
        /// Integer magnification of a font matrix axis: nearest multiple (half away from zero), 1 to 10.
        /// </summary>
        public static int GetMagnification(int requestedSize, int matrixSize)
        {
            double ratio = Math.Round((double)requestedSize / matrixSize, MidpointRounding.AwayFromZero);

            return (int)Math.Clamp(ratio, 1, 10);
        }

        /// <summary>
        /// Cell height in dots and horizontal scale of an outline-rendered font; matrix fonts use integer magnification.
        /// </summary>
        public static (float cellHeight, float scaleX) GetFontScaling(string fontName, int fontHeight, int fontWidth, int printDensityDpmm)
        {
            // P–V: an omitted axis stays 1× (Labelary: ^APN,40 doubles only the height)
            if (matrixScalableFonts.TryGetValue(fontName, out MatrixScalableFont matrixFont))
            {
                int magnificationY = fontHeight > 0 ? GetMagnification(fontHeight, matrixFont.Height) : 1;
                int magnificationX = fontWidth > 0 ? GetMagnification(fontWidth, matrixFont.Width) : 1;

                return (matrixFont.Height * magnificationY, matrixFont.EmWidth * magnificationX / (matrixFont.EmHeight * magnificationY));
            }

            (int height, int width)? fontScale = GetFontScale(fontName, printDensityDpmm);

            if (fontScale != null)
            {
                (int height, int width) = fontScale.Value;
                if (fontHeight > 0)
                {
                    int intHeightRatio = GetMagnification(fontHeight, height);
                    float cellHeight = height * intHeightRatio;

                    if (fontWidth == 0)
                    {
                        return (cellHeight, 1.0f);
                    }

                    int intWidthRatio = GetMagnification(fontWidth, width);

                    return (cellHeight, (float)intWidthRatio / intHeightRatio);
                }
                else if (fontWidth > 0)
                {
                    int intWidthRatio = GetMagnification(fontWidth, width);

                    return (height * intWidthRatio, 1.0f);
                }
                else
                {
                    return (height, 1.0f);
                }
            }

            // Scalable fonts: a missing dimension equals the other one (natural aspect), each clamped to 10–32000
            // (Labelary: ^A0N,,20 is 20×20, ^A0N,9,5 is 10×10)
            int scalableHeight = fontHeight > 0 ? fontHeight : fontWidth;
            int scalableWidth = fontWidth > 0 ? fontWidth : fontHeight;
            float fontSize = Math.Clamp(scalableHeight, scalableMinSize, scalableMaxSize);
            float scaleX = Math.Clamp(scalableWidth, scalableMinSize, scalableMaxSize) / fontSize;

            return (fontSize, scaleX);
        }
    }
}
