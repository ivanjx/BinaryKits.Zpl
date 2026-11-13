using SkiaSharp;

using System;
using System.Collections.Generic;

namespace BinaryKits.Zpl.Viewer.Helpers
{
    using FontScaleDictionary = Dictionary<string, (int height, int width)>;

    internal static class FontScale
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
            ["GS"] = (24, 24),
            ["P"] = (20, 18),
            ["Q"] = (28, 24),
            ["R"] = (35, 31),
            ["S"] = (40, 35),
            ["T"] = (48, 42),
            ["U"] = (59, 53),
            ["V"] = (80, 71)
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
            ["GS"] = (24, 24),
            ["P"] = (20, 18),
            ["Q"] = (28, 24),
            ["R"] = (35, 31),
            ["S"] = (40, 35),
            ["T"] = (48, 42),
            ["U"] = (59, 53),
            ["V"] = (80, 71)
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
            ["GS"] = (24, 24),
            ["P"] = (20, 18),
            ["Q"] = (28, 24),
            ["R"] = (35, 31),
            ["S"] = (40, 35),
            ["T"] = (48, 42),
            ["U"] = (59, 53),
            ["V"] = (80, 71)
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
            ["GS"] = (24, 24),
            ["P"] = (20, 18),
            ["Q"] = (28, 24),
            ["R"] = (35, 31),
            ["S"] = (40, 35),
            ["T"] = (48, 42),
            ["U"] = (59, 53),
            ["V"] = (80, 71)
        };

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

        private static readonly (int height, int width) defaultScalingFontScale = (15, 12);

        /// <summary>
        /// Scalable font cap height and baseline as a fraction of the cell height: Zebra's spacing table puts the font 0
        /// baseline at 3 × HEIGHT / 4, and Labelary draws the cap height at 3/4 of the cell (Triumvirate cap height 750/1000).
        /// </summary>
        private const int scalableBaselineNumerator = 3;
        private const int scalableBaselineDenominator = 4;

        /// <summary>
        /// Character pitch (matrix width + intercharacter gap) of the A, C and D matrices as a fraction of their height
        /// (6/9, 12/18). Outline substitutes for resident bitmap fonts are monospace, so their advance is fitted to it.
        /// </summary>
        private const float residentPitchRatio = 2.0f / 3.0f;

        /// <summary>
        /// Cell height in dots of a font matrix at an integer magnification, or null for fonts without a matrix.
        /// </summary>
        public static int? GetCellHeight(string fontName, int magnification, int printDensityDpmm)
        {
            return GetFontScale(fontName, printDensityDpmm)?.height * magnification;
        }

        /// <summary>
        /// Creates the outline font for a text field cell: scalable fonts (0, P–V) by cap height, resident bitmap font
        /// substitutes (A–H) by character pitch.
        /// </summary>
        public static SKFont CreateTextFont(SKTypeface typeface, float cellHeight, float scaleX, bool isScalable)
        {
            return isScalable ?
                CreateCapHeightFont(typeface, cellHeight * scalableBaselineNumerator / scalableBaselineDenominator, scaleX) :
                CreateResidentFont(typeface, cellHeight, scaleX);
        }

        /// <summary>
        /// Baseline below the cell top (<c>^FO</c> origin): 3 × HEIGHT / 4 in whole dots for scalable fonts, the cap height
        /// of the substitute typeface for resident bitmap fonts.
        /// </summary>
        public static float GetTextBaseline(SKFont font, float cellHeight, bool isScalable)
        {
            if (isScalable)
            {
                return MathF.Floor(cellHeight * scalableBaselineNumerator / scalableBaselineDenominator);
            }

            font.MeasureText("X", out SKRect capBounds);

            return capBounds.Height;
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

            // Scalable fonts: a missing dimension follows the font 0 matrix ratio (15 high by 12 wide),
            // which renders at the natural aspect (scaleX 1).
            float fontSize = fontHeight > 0 ?
                fontHeight :
                fontWidth > 0 ? (float)Math.Round(fontWidth * (double)defaultScalingFontScale.height / defaultScalingFontScale.width, MidpointRounding.AwayFromZero) : defaultScalingFontScale.height;
            float scaleX = 1.0f;

            if (fontHeight > 0 && fontWidth > 0 && fontWidth != fontSize)
            {
                scaleX = fontWidth / fontSize;
            }

            return (fontSize, scaleX);
        }
    }
}
