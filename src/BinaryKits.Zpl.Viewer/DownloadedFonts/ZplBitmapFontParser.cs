using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace BinaryKits.Zpl.Viewer.DownloadedFonts
{
    /// <summary>
    /// Parses and validates a ~DB bitmap font download (<c>.FNT</c>): <c>~DBd:o.x,a,h,w,base,space,#char,©,data</c>.
    /// </summary>
    public static class ZplBitmapFontParser
    {
        private const string commandPrefix = "~DB";
        private const int maxMetric = 32000;
        private const int maxCharacters = 256;
        private const int maxCopyrightLength = 63;

        private static readonly Regex verticalWhitespaceRegex = new(@"[\n\v\f\r]", RegexOptions.Compiled);
        private static readonly Regex glyphRegex = new(
            @"#([0-9A-Fa-f]{1,4})\.(-?\d+)\.(-?\d+)\.(-?\d+)\.(-?\d+)\.(-?\d+)\.(.*?)(?=#[0-9A-Fa-f]{1,4}\.|\z)",
            RegexOptions.Compiled | RegexOptions.Singleline);

        /// <summary>
        /// Parses a ~DB command, optionally surrounded by whitespace or <c>^XA</c>…<c>^XZ</c>.
        /// </summary>
        /// <param name="command">The ~DB command</param>
        /// <param name="name">Normalized object name: <c>R:</c> and <c>UNKNOWN</c> by default, <c>.FNT</c> appended when the name has no extension</param>
        /// <param name="font">The parsed font, <c>null</c> on failure</param>
        /// <param name="error">Why parsing failed, <c>null</c> on success</param>
        public static bool TryParse(string command, out PrinterObjectName name, out ZplDownloadedBitmapFont font, out string error)
        {
            name = default;
            font = null;
            error = null;

            try
            {
                font = Parse(command, out name);
                return true;
            }
            catch (FormatException exception)
            {
                name = default;
                error = exception.Message;
                return false;
            }
        }

        private static ZplDownloadedBitmapFont Parse(string command, out PrinterObjectName name)
        {
            string text = verticalWhitespaceRegex.Replace(command ?? string.Empty, string.Empty).Trim();

            if (text.StartsWith("^XA", StringComparison.OrdinalIgnoreCase))
            {
                text = text.Substring(3).Trim();
            }

            if (text.EndsWith("^XZ", StringComparison.OrdinalIgnoreCase))
            {
                text = text.Substring(0, text.Length - 3).Trim();
            }

            if (!text.StartsWith(commandPrefix, StringComparison.OrdinalIgnoreCase))
            {
                throw new FormatException("Not a ~DB command.");
            }

            string[] parts = text.Substring(commandPrefix.Length).Split(',', 9);
            if (parts.Length < 9)
            {
                throw new FormatException("~DB needs drive/name, orientation, height, width, baseline, space, character count, copyright and data.");
            }

            if (!ZplDownloadData.TryParseObjectName(parts[0], ".FNT", out name))
            {
                throw new FormatException($"Invalid font name '{parts[0].Trim()}'.");
            }

            if (parts[1].Trim().ToUpperInvariant() != "N")
            {
                throw new FormatException("Orientation must be N.");
            }

            int cellHeight = ParseRange(parts[2], 1, maxMetric, "Cell height");
            int cellWidth = ParseRange(parts[3], 1, maxMetric, "Cell width");
            int baseline = ParseRange(parts[4], 1, maxMetric, "Baseline");
            int spaceWidth = ParseRange(parts[5], 1, maxMetric, "Space width");
            int characterCount = ParseRange(parts[6], 1, maxCharacters, "Character count");

            if (baseline > cellHeight)
            {
                throw new FormatException("Baseline must not exceed the cell height.");
            }

            string copyright = parts[7];
            if (copyright.Length < 1 || copyright.Length > maxCopyrightLength)
            {
                throw new FormatException($"Copyright must be 1 to {maxCopyrightLength} characters.");
            }

            List<ZplDownloadedBitmapGlyph> glyphs = ParseGlyphs(parts[8]);

            if (glyphs.Count != characterCount)
            {
                throw new FormatException($"Font declares {characterCount} characters but supplies {glyphs.Count}.");
            }

            return new ZplDownloadedBitmapFont(cellHeight, cellWidth, baseline, spaceWidth, copyright, glyphs);
        }

        private static List<ZplDownloadedBitmapGlyph> ParseGlyphs(string source)
        {
            List<ZplDownloadedBitmapGlyph> glyphs = [];
            HashSet<int> codes = [];

            Match match = glyphRegex.Match(source);
            if (!match.Success)
            {
                throw new FormatException("Font contains no glyphs.");
            }

            if (source.Substring(0, match.Index).Trim().Length > 0)
            {
                throw new FormatException("Font contains data before its first glyph.");
            }

            for (; match.Success; match = match.NextMatch())
            {
                int code = int.Parse(match.Groups[1].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);

                if (!TryParseInt(match.Groups[2].Value, out int height)
                    || !TryParseInt(match.Groups[3].Value, out int width)
                    || !TryParseInt(match.Groups[4].Value, out int xOffset)
                    || !TryParseInt(match.Groups[5].Value, out int yOffset)
                    || !TryParseInt(match.Groups[6].Value, out int advance)
                    || height <= 0
                    || width <= 0
                    || advance < 0)
                {
                    throw new FormatException($"Glyph {code:X4} has invalid metrics.");
                }

                if (!codes.Add(code))
                {
                    throw new FormatException($"Glyph {code:X4} is defined twice.");
                }

                int bytesPerRow = (width + 7) / 8;
                long expectedBytes = (long)bytesPerRow * height;

                byte[] rows;
                try
                {
                    rows = ZplDownloadData.Decode(match.Groups[7].Value, expectedBytes, bytesPerRow, allowAcs: true, legacyZeros: true);
                }
                catch (FormatException exception)
                {
                    throw new FormatException($"Glyph {code:X4}: {exception.Message}", exception);
                }

                glyphs.Add(new ZplDownloadedBitmapGlyph(code, width, height, xOffset, yOffset, advance, rows));
            }

            return glyphs;
        }

        private static int ParseRange(string value, int min, int max, string field)
        {
            if (!TryParseInt(value.Trim(), out int result) || result < min || result > max)
            {
                throw new FormatException($"{field} must be {min} to {max}.");
            }

            return result;
        }

        private static bool TryParseInt(string value, out int result)
        {
            return int.TryParse(value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out result);
        }
    }
}
