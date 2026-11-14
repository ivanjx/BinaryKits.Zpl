using BinaryKits.Zpl.Label;
using BinaryKits.Zpl.Label.Elements;
using BinaryKits.Zpl.Viewer.BitmapFonts;
using BinaryKits.Zpl.Viewer.Helpers;

using SkiaSharp;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BinaryKits.Zpl.Viewer.ElementDrawers
{
    /// <summary>
    /// Drawer for ^TB Text Block elements: word-wrapped lines in a block <c>Width × Height</c> dots, cut off at the
    /// bottom of the block. The Zebra documentation defines the wrapping, the truncation and the escapes; line pitch,
    /// anchoring and clipping follow Labelary.
    /// </summary>
    public class TextBlockElementDrawer : TextFieldElementDrawer
    {
        /// <summary>
        /// Margin of the clip rectangle on the sides the block does not clip.
        /// </summary>
        private const float unclippedMargin = 100000;

        ///<inheritdoc/>
        public override bool CanDraw(ZplElementBase element)
        {
            return element is ZplTextBlock;
        }

        ///<inheritdoc/>
        public override SKPoint Draw(ZplElementBase element, DrawerOptions options, SKPoint currentPosition, InternationalFont internationalFont, int printDensityDpmm)
        {
            if (element is ZplTextBlock textBlock)
            {
                ZplFont font = textBlock.Font;

                string text = textBlock.Text;
                if (textBlock.HexadecimalIndicator is char hexIndicator)
                {
                    text = text.ReplaceHexEscapes(hexIndicator, internationalFont);
                }

                text = RemoveEscapeSequences(text);
                text = text.ApplyInternationalCharacterSet(font.FontName, internationalFont);

                // Soft hyphens are neither printed nor break points
                text = text.Replace("\u00AD", "");

                float x = textBlock.PositionX;
                float y = textBlock.PositionY;

                if (textBlock.UseDefaultPosition)
                {
                    x = currentPosition.X;
                    y = currentPosition.Y;
                }

                FieldJustification fieldJustification = textBlock.FieldOrigin != null ?
                    textBlock.FieldOrigin.FieldJustification :
                    textBlock.FieldTypeset.FieldJustification;
                bool rightJustified = IsRightJustified(text, fieldJustification);

                if (this.TryDrawBitmapTextBlock(
                        textBlock,
                        options,
                        currentPosition,
                        internationalFont,
                        printDensityDpmm,
                        text,
                        x,
                        y,
                        rightJustified,
                        out SKPoint nextPosition))
                {
                    return nextPosition;
                }

                (float cellHeight, float scaleX) = FontScale.GetFontScaling(font.FontName, font.FontHeight, font.FontWidth, printDensityDpmm);
                bool isScalable = FontScale.IsScalableFont(font.FontName);

                string typefaceFontName = isScalable ? "0" : font.FontName;
                SKTypeface typeface = options.FontManager.FontLoader(typefaceFontName);

                if (isScalable)
                {
                    if (options.ReplaceDashWithEnDash)
                    {
                        text = text.Replace("-", " \u2013 ");
                    }

                    if (options.ReplaceUnderscoreWithEnSpace)
                    {
                        text = text.Replace('_', '\u2002');
                    }
                }

                using SKFont skFont = FontScale.CreateTextFont(typeface, font.FontName, cellHeight, scaleX);
                using SKPaint skPaint = new()
                {
                    IsAntialias = options.Antialias
                };

                if (textBlock.ReversePrint)
                {
                    skPaint.BlendMode = SKBlendMode.Xor;
                }

                // Every line sits like a plain ^FO field at its line top
                float baseline = FontScale.GetTextBaseline(skFont, font.FontName, cellHeight);
                List<string> lines = WrapText(text, textBlock.Width, value => skFont.MeasureShapedText(value));

                return this.DrawTextBlock(
                    textBlock,
                    lines,
                    x,
                    y,
                    rightJustified,
                    lineIndex => FontScale.GetTextBlockLineOffset(font.FontName, cellHeight, lineIndex),
                    value => skFont.MeasureShapedText(value),
                    (line, lineX, lineTop) => this.skCanvas.DrawShapedTextSafe(line, lineX, lineTop + baseline, SKTextAlign.Left, skFont, skPaint),
                    currentPosition);
            }

            return currentPosition;
        }

        private bool TryDrawBitmapTextBlock(
            ZplTextBlock textBlock,
            DrawerOptions options,
            SKPoint currentPosition,
            InternationalFont internationalFont,
            int printDensityDpmm,
            string text,
            float x,
            float y,
            bool rightJustified,
            out SKPoint nextPosition)
        {
            nextPosition = currentPosition;

            if (options.TextRenderingMode != ZplTextRenderingMode.BitmapStrict)
            {
                return false;
            }

            ZplFont font = textBlock.Font;
            if (FontScale.IsScalableFont(font.FontName))
            {
                return false;
            }

            if (!TryGetBitmapFontData(
                    options,
                    font.FontName,
                    printDensityDpmm,
                    internationalFont,
                    out ZplBitmapFontData fontData))
            {
                return false;
            }

            ZplBitmapFontMetrics metrics = CreateExpandedMetrics(fontData.Metrics, font.FontHeight, font.FontWidth);
            List<string> lines = WrapText(text, textBlock.Width, value => ZplBitmapFontRenderer.MeasureTextWidth(value, metrics));

            using SKPaint paint = ZplBitmapFontRenderer.CreatePaint(textBlock.ReversePrint);
            ZplBitmapFontRenderer renderer = new(this.skCanvas);

            nextPosition = this.DrawTextBlock(
                textBlock,
                lines,
                x,
                y,
                rightJustified,
                lineIndex => FontScale.GetTextBlockLineOffset(font.FontName, metrics.RenderedGlyphHeight, lineIndex),
                value => ZplBitmapFontRenderer.MeasureTextWidth(value, metrics),
                (line, lineX, lineTop) => renderer.DrawText(line, fontData, metrics, lineX, lineTop, paint),
                currentPosition);

            return true;
        }

        /// <summary>
        /// Draws the wrapped lines in the block: ^FO puts the block's top-left corner on the origin, ^FT its bottom-left
        /// corner (Labelary). Right justification ends the block at x and aligns every line to its right edge. Lines
        /// below the block are not drawn and the last visible line is cut off at the block's bottom edge.
        /// </summary>
        private SKPoint DrawTextBlock(
            ZplTextBlock textBlock,
            IReadOnlyList<string> lines,
            float x,
            float y,
            bool rightJustified,
            Func<int, float> getLineOffset,
            Func<string, float> measureText,
            Action<string, float, float> drawLine,
            SKPoint currentPosition)
        {
            FieldOrientation fieldOrientation = textBlock.Font.FieldOrientation;
            bool useFieldOrigin = textBlock.FieldOrigin != null;

            if (useFieldOrigin && rightJustified)
            {
                x = GetRightJustifiedFieldOriginX(x, textBlock.Width, textBlock.Height, fieldOrientation);
            }

            using (new SKAutoCanvasRestore(this.skCanvas))
            {
                SKMatrix matrix = CreateFieldRotationMatrix(fieldOrientation, useFieldOrigin, x, y, textBlock.Width, textBlock.Height);

                if (matrix != SKMatrix.Empty)
                {
                    this.skCanvas.Concat(matrix);
                }

                float blockLeft = !useFieldOrigin && rightJustified ? x - textBlock.Width : x;
                float blockTop = useFieldOrigin ? y : y - textBlock.Height;
                float blockBottom = blockTop + textBlock.Height;

                this.skCanvas.ClipRect(new SKRect(
                    blockLeft - unclippedMargin,
                    blockTop - unclippedMargin,
                    blockLeft + textBlock.Width + unclippedMargin,
                    blockBottom));

                for (int i = 0; i < lines.Count; i++)
                {
                    float lineTop = blockTop + getLineOffset(i);
                    if (lineTop >= blockBottom)
                    {
                        break;
                    }

                    string line = lines[i];
                    float lineX = rightJustified ?
                        blockLeft + textBlock.Width - measureText(line) :
                        blockLeft;

                    drawLine(line, lineX, lineTop);
                }

                return this.CalculateNextDefaultPosition(x, y, textBlock.Width, textBlock.Height, useFieldOrigin, fieldOrientation, currentPosition);
            }
        }

        /// <summary>
        /// Removes ^TB escape sequences: data from <c>&lt;</c> to the next <c>&gt;</c> (or the end of the data) is not
        /// printed, except that a sequence starting with <c>&lt;&lt;</c> prints one <c>&lt;</c> (Zebra: <c>&lt;&lt; &gt;</c>;
        /// Labelary also for <c>&lt;&lt;&gt;</c> and an unterminated <c>&lt;&lt;</c>).
        /// </summary>
        private static string RemoveEscapeSequences(string text)
        {
            if (!text.Contains('<'))
            {
                return text;
            }

            StringBuilder result = new(text.Length);

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c != '<')
                {
                    result.Append(c);
                    continue;
                }

                if (i + 1 < text.Length && text[i + 1] == '<')
                {
                    result.Append('<');
                }

                int end = text.IndexOf('>', i + 1);
                i = end < 0 ? text.Length : end;
            }

            return result.ToString();
        }

        /// <summary>
        /// ^TB word wrap (Labelary): lines break at spaces, spaces inside a line are kept and spaces at a break are
        /// dropped; a word wider than the block is split between characters without a hyphen, at least one character
        /// per line. <c>\&amp;</c> is printed, not a line break.
        /// </summary>
        private static List<string> WrapText(string text, float maxWidth, Func<string, float> measureText)
        {
            List<string> lines = [];
            string line = null;

            foreach (string word in text.Split(' '))
            {
                if (line != null)
                {
                    string candidate = line + " " + word;
                    if (measureText(candidate) <= maxWidth)
                    {
                        line = candidate;
                        continue;
                    }

                    lines.Add(line.TrimEnd(' '));
                    line = null;
                }

                // Spaces at a line break are dropped; leading spaces of the first line are kept
                if (word.Length == 0 && lines.Count > 0)
                {
                    continue;
                }

                line = AddWordChunks(word, maxWidth, measureText, lines);
            }

            if (line != null)
            {
                lines.Add(line.TrimEnd(' '));
            }

            return lines;
        }

        /// <summary>
        /// Splits a word wider than the block into the longest prefixes that fit (at least one character each), adds
        /// all but the last as lines and returns the last one.
        /// </summary>
        private static string AddWordChunks(string word, float maxWidth, Func<string, float> measureText, List<string> lines)
        {
            if (measureText(word) <= maxWidth)
            {
                return word;
            }

            string[] runes = [.. word.EnumerateRunes().Select(rune => rune.ToString())];
            int start = 0;

            while (true)
            {
                int count = 1;
                while (start + count < runes.Length &&
                       measureText(string.Concat(runes.Skip(start).Take(count + 1))) <= maxWidth)
                {
                    count++;
                }

                string chunk = string.Concat(runes.Skip(start).Take(count));
                start += count;

                if (start >= runes.Length)
                {
                    return chunk;
                }

                lines.Add(chunk);
            }
        }
    }
}
