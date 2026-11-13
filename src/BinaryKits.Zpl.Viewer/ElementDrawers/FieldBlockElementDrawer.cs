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
    /// Drawer for Field Block elements
    /// </summary>
    public class FieldBlockElementDrawer : ElementDrawerBase
    {
        private const string FallbackBitmapFontName = "A";

        ///<inheritdoc/>
        public override bool CanDraw(ZplElementBase element)
        {
            return element is ZplFieldBlock;
        }

        ///<inheritdoc/>
        public override bool IsReverseDraw(ZplElementBase element)
        {
            if (element is ZplFieldBlock fieldBlock)
            {
                return fieldBlock.ReversePrint;
            }

            return false;
        }

        ///<inheritdoc/>
        public override SKPoint Draw(ZplElementBase element, DrawerOptions options, SKPoint currentPosition, InternationalFont internationalFont, int printDensityDpmm)
        {
            if (element is ZplFieldBlock fieldBlock)
            {
                ZplFont font = fieldBlock.Font;

                string text = fieldBlock.Text;
                if (fieldBlock.HexadecimalIndicator is char hexIndicator)
                {
                    text = text.ReplaceHexEscapes(hexIndicator, internationalFont);
                }

                List<string> paragraphs = SplitParagraphs(text);
                for (int i = 0; i < paragraphs.Count; i++)
                {
                    paragraphs[i] = paragraphs[i].ApplyInternationalCharacterSet(font.FontName, internationalFont);
                }

                if (this.TryDrawBitmapFieldBlock(
                        fieldBlock,
                        options,
                        currentPosition,
                        internationalFont,
                        printDensityDpmm,
                        paragraphs,
                        out SKPoint nextPosition))
                {
                    return nextPosition;
                }

                (float cellHeight, float scaleX) = FontScale.GetFontScaling(font.FontName, font.FontHeight, font.FontWidth, printDensityDpmm);
                bool isScalable = IsFont0Compatible(font.FontName);
                string typefaceFontName = isScalable ? "0" : font.FontName;
                SKTypeface typeface = options.FontManager.FontLoader(typefaceFontName);
                if (isScalable)
                {
                    for (int i = 0; i < paragraphs.Count; i++)
                    {
                        string paragraph = paragraphs[i];

                        if (options.ReplaceDashWithEnDash)
                        {
                            paragraph = paragraph.Replace("-", " \u2013 ");
                        }

                        if (options.ReplaceUnderscoreWithEnSpace)
                        {
                            paragraph = paragraph.Replace('_', '\u2002');
                        }

                        paragraphs[i] = paragraph;
                    }
                }

                using SKFont skFont = FontScale.CreateTextFont(typeface, cellHeight, scaleX, isScalable);
                using SKPaint skPaint = new()
                {
                    IsAntialias = options.Antialias
                };

                float baseline = FontScale.GetTextBaseline(skFont, cellHeight, isScalable);

                float positionX = fieldBlock.PositionX;
                float positionY = fieldBlock.PositionY;

                if (fieldBlock.UseDefaultPosition)
                {
                    positionX = currentPosition.X;
                    positionY = currentPosition.Y;
                }

                List<WrappedTextLine> textLines = WordWrap(paragraphs, skFont, fieldBlock.Width);
                int hangingIndent = 0;
                float lineHeight = cellHeight + fieldBlock.LineSpace;
                float y = positionY + baseline;

                // actual ZPL printer does not include trailing line spacing in total height
                float totalHeight = lineHeight * fieldBlock.MaxLineCount - fieldBlock.LineSpace;
                // labelary
                //var totalHeight = lineHeight * fieldBlock.MaxLineCount;

                if (fieldBlock.FieldTypeset != null)
                {
                    totalHeight = lineHeight * (fieldBlock.MaxLineCount - 1) + baseline;
                    y -= totalHeight;
                }

                using (new SKAutoCanvasRestore(this.skCanvas))
                {
                    SKMatrix matrix = SKMatrix.Empty;

                    if (fieldBlock.FieldOrigin != null)
                    {
                        switch (fieldBlock.Font.FieldOrientation)
                        {
                            case FieldOrientation.Rotated90:
                                matrix = SKMatrix.CreateRotationDegrees(90, positionX + totalHeight / 2, positionY + totalHeight / 2);
                                break;
                            case FieldOrientation.Rotated180:
                                matrix = SKMatrix.CreateRotationDegrees(180, positionX + fieldBlock.Width / 2, positionY + totalHeight / 2);
                                break;
                            case FieldOrientation.Rotated270:
                                matrix = SKMatrix.CreateRotationDegrees(270, positionX + fieldBlock.Width / 2, positionY + fieldBlock.Width / 2);
                                break;
                            case FieldOrientation.Normal:
                                break;
                        }
                    }
                    else
                    {
                        switch (fieldBlock.Font.FieldOrientation)
                        {
                            case FieldOrientation.Rotated90:
                                matrix = SKMatrix.CreateRotationDegrees(90, positionX, positionY);
                                break;
                            case FieldOrientation.Rotated180:
                                matrix = SKMatrix.CreateRotationDegrees(180, positionX, positionY);
                                break;
                            case FieldOrientation.Rotated270:
                                matrix = SKMatrix.CreateRotationDegrees(270, positionX, positionY);
                                break;
                            case FieldOrientation.Normal:
                                break;
                        }
                    }

                    if (matrix != SKMatrix.Empty)
                    {
                        SKMatrix currentMatrix = this.skCanvas.TotalMatrix;
                        SKMatrix concatMatrix = SKMatrix.Concat(currentMatrix, matrix);
                        this.skCanvas.SetMatrix(concatMatrix);
                    }

                    int lineIndex = 0;

                    foreach (WrappedTextLine wrappedLine in textLines)
                    {
                        string textLine = wrappedLine.Text;
                        int visibleLineIndex = Math.Min(lineIndex, Math.Max(fieldBlock.MaxLineCount - 1, 0));
                        float lineY = y + visibleLineIndex * lineHeight;
                        float x = positionX + hangingIndent;

                        skFont.MeasureShapedText(textLine, out SKRect textBounds);
                        float diff = fieldBlock.Width - textBounds.Width;

                        // A line wider than the block starts at the block's x for every justification (Labelary)
                        switch (fieldBlock.TextJustification)
                        {
                            case TextJustification.Center:
                                if (diff > 0)
                                {
                                    x += diff / 2 - textBounds.Left;
                                }
                                break;
                            case TextJustification.Right:
                                if (diff > 0)
                                {
                                    x += diff - textBounds.Left * 2;
                                }
                                hangingIndent = -fieldBlock.HangingIndent;
                                break;
                            case TextJustification.Left:
                            case TextJustification.Justified:
                            default:
                                hangingIndent = fieldBlock.HangingIndent;
                                break;
                        }

                        if (fieldBlock.ReversePrint)
                        {
                            skPaint.BlendMode = SKBlendMode.Xor;
                        }

                        if (fieldBlock.TextJustification == TextJustification.Justified &&
                            wrappedLine.ShouldJustify)
                        {
                            float currentLineIndent = x - positionX;
                            float availableWidth = Math.Max(fieldBlock.Width - currentLineIndent, 1);
                            this.DrawJustifiedTextLine(textLine, x, lineY, availableWidth, skFont, skPaint);
                        }
                        else
                        {
                            this.skCanvas.DrawShapedTextSafe(textLine, x, lineY, SKTextAlign.Left, skFont, skPaint);
                        }

                        lineIndex++;
                    }

                    return this.CalculateNextDefaultPosition(positionX, positionY, fieldBlock.Width, totalHeight, fieldBlock.FieldOrigin != null, fieldBlock.Font.FieldOrientation, currentPosition);
                }
            }

            return currentPosition;
        }

        private bool TryDrawBitmapFieldBlock(
            ZplFieldBlock fieldBlock,
            DrawerOptions options,
            SKPoint currentPosition,
            InternationalFont internationalFont,
            int printDensityDpmm,
            IReadOnlyList<string> paragraphs,
            out SKPoint nextPosition)
        {
            nextPosition = currentPosition;

            if (options.TextRenderingMode != ZplTextRenderingMode.BitmapStrict)
            {
                return false;
            }

            ZplFont font = fieldBlock.Font;
            if (IsFont0Compatible(font.FontName))
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

            ZplBitmapFontMetrics metrics = CreateExpandedMetrics(fontData.Metrics, font.FontHeight, font.FontWidth, fieldBlock.LineSpace);
            List<WrappedTextLine> textLines = WordWrap(paragraphs, metrics, fieldBlock.Width);
            int maxLineCount = Math.Max(fieldBlock.MaxLineCount, 1);
            int totalHeight = metrics.LineHeight * maxLineCount - fieldBlock.LineSpace;
            float x = fieldBlock.PositionX;
            float topY = fieldBlock.PositionY;

            if (fieldBlock.UseDefaultPosition)
            {
                x = currentPosition.X;
                topY = currentPosition.Y;
            }

            if (fieldBlock.FieldTypeset != null)
            {
                topY -= (maxLineCount - 1) * metrics.LineHeight + metrics.Baseline * metrics.ExpansionY;
            }

            using (new SKAutoCanvasRestore(this.skCanvas))
            {
                SKMatrix matrix = CreateFieldBlockRotationMatrix(fieldBlock, totalHeight);

                if (matrix != SKMatrix.Empty)
                {
                    SKMatrix currentMatrix = this.skCanvas.TotalMatrix;
                    SKMatrix concatMatrix = SKMatrix.Concat(currentMatrix, matrix);
                    this.skCanvas.SetMatrix(concatMatrix);
                }

                using SKPaint paint = ZplBitmapFontRenderer.CreatePaint(fieldBlock.ReversePrint);
                ZplBitmapFontRenderer renderer = new(this.skCanvas);
                int hangingIndent = 0;
                int lineIndex = 0;

                foreach (WrappedTextLine wrappedLine in textLines)
                {
                    string textLine = wrappedLine.Text;
                    int visibleLineIndex = Math.Min(lineIndex, maxLineCount - 1);
                    int lineWidth = ZplBitmapFontRenderer.MeasureTextWidth(textLine, metrics);
                    float lineX = x + hangingIndent;
                    float lineY = topY + visibleLineIndex * metrics.LineHeight;
                    float diff = fieldBlock.Width - lineWidth;

                    // A line wider than the block starts at the block's x for every justification (Labelary)
                    switch (fieldBlock.TextJustification)
                    {
                        case TextJustification.Center:
                            if (diff > 0)
                            {
                                lineX += diff / 2;
                            }
                            break;
                        case TextJustification.Right:
                            if (diff > 0)
                            {
                                lineX += diff;
                            }
                            hangingIndent = -fieldBlock.HangingIndent;
                            break;
                        case TextJustification.Left:
                        case TextJustification.Justified:
                        default:
                            hangingIndent = fieldBlock.HangingIndent;
                            break;
                    }

                    if (fieldBlock.TextJustification == TextJustification.Justified &&
                        wrappedLine.ShouldJustify)
                    {
                        float currentLineIndent = lineX - x;
                        float availableWidth = Math.Max(fieldBlock.Width - currentLineIndent, 1);
                        this.DrawJustifiedBitmapTextLine(textLine, lineX, lineY, availableWidth, fontData, metrics, renderer, paint);
                    }
                    else
                    {
                        renderer.DrawText(textLine, fontData, metrics, lineX, lineY, paint);
                    }

                    lineIndex++;
                }

                nextPosition = this.CalculateNextDefaultPosition(
                    fieldBlock.PositionX,
                    fieldBlock.PositionY,
                    fieldBlock.Width,
                    totalHeight,
                    fieldBlock.FieldOrigin != null,
                    fieldBlock.Font.FieldOrientation,
                    currentPosition);
            }

            return true;
        }

        private static SKMatrix CreateFieldBlockRotationMatrix(ZplFieldBlock fieldBlock, float totalHeight)
        {
            if (fieldBlock.FieldOrigin != null)
            {
                switch (fieldBlock.Font.FieldOrientation)
                {
                    case FieldOrientation.Rotated90:
                        return SKMatrix.CreateRotationDegrees(90, fieldBlock.PositionX + totalHeight / 2, fieldBlock.PositionY + totalHeight / 2);
                    case FieldOrientation.Rotated180:
                        return SKMatrix.CreateRotationDegrees(180, fieldBlock.PositionX + fieldBlock.Width / 2, fieldBlock.PositionY + totalHeight / 2);
                    case FieldOrientation.Rotated270:
                        return SKMatrix.CreateRotationDegrees(270, fieldBlock.PositionX + fieldBlock.Width / 2, fieldBlock.PositionY + fieldBlock.Width / 2);
                    case FieldOrientation.Normal:
                    default:
                        return SKMatrix.Empty;
                }
            }

            switch (fieldBlock.Font.FieldOrientation)
            {
                case FieldOrientation.Rotated90:
                    return SKMatrix.CreateRotationDegrees(90, fieldBlock.PositionX, fieldBlock.PositionY);
                case FieldOrientation.Rotated180:
                    return SKMatrix.CreateRotationDegrees(180, fieldBlock.PositionX, fieldBlock.PositionY);
                case FieldOrientation.Rotated270:
                    return SKMatrix.CreateRotationDegrees(270, fieldBlock.PositionX, fieldBlock.PositionY);
                case FieldOrientation.Normal:
                default:
                    return SKMatrix.Empty;
            }
        }

        private static bool IsFont0Compatible(string fontName)
        {
            return fontName is "0" or "P" or "Q" or "R" or "S" or "T" or "U" or "V";
        }

        private static bool TryGetBitmapFontData(
            DrawerOptions options,
            string fontName,
            int printDensityDpmm,
            InternationalFont internationalFont,
            out ZplBitmapFontData fontData)
        {
            fontData = null;

            if (options.BitmapFontProvider == null)
            {
                return false;
            }

            if (options.BitmapFontProvider.TryGet(fontName, printDensityDpmm, internationalFont, out fontData))
            {
                return true;
            }

            return options.BitmapFontProvider.TryGet(
                FallbackBitmapFontName,
                printDensityDpmm,
                internationalFont,
                out fontData);
        }

        private void DrawJustifiedBitmapTextLine(
            string textLine,
            float x,
            float y,
            float lineWidth,
            ZplBitmapFontData fontData,
            ZplBitmapFontMetrics metrics,
            ZplBitmapFontRenderer renderer,
            SKPaint paint)
        {
            string[] words = textLine.Split([' '], StringSplitOptions.None);

            if (words.Length <= 1)
            {
                renderer.DrawText(textLine, fontData, metrics, x, y, paint);
                return;
            }

            int wordsWidth = 0;

            foreach (string word in words)
            {
                wordsWidth += ZplBitmapFontRenderer.MeasureTextWidth(word, metrics);
            }

            int gapCount = words.Length - 1;
            float gapWidth = (lineWidth - wordsWidth) / gapCount;

            if (!float.IsFinite(gapWidth) || gapWidth <= 0)
            {
                renderer.DrawText(textLine, fontData, metrics, x, y, paint);
                return;
            }

            float currentX = x;

            for (int i = 0; i < words.Length; i++)
            {
                string word = words[i];

                if (word.Length > 0)
                {
                    currentX += renderer.DrawText(word, fontData, metrics, currentX, y, paint);
                }

                if (i < gapCount)
                {
                    currentX += gapWidth;
                }
            }
        }

        private void DrawJustifiedTextLine(string textLine, float x, float y, float lineWidth, SKFont font, SKPaint paint)
        {
            string[] words = textLine.Split([' '], StringSplitOptions.None);

            if (words.Length <= 1)
            {
                this.skCanvas.DrawShapedTextSafe(textLine, x, y, SKTextAlign.Left, font, paint);
                return;
            }

            float wordsWidth = 0;

            foreach (string word in words)
            {
                wordsWidth += font.MeasureShapedText(word);
            }

            int gapCount = words.Length - 1;
            float gapWidth = (lineWidth - wordsWidth) / gapCount;

            if (!float.IsFinite(gapWidth) || gapWidth <= 0)
            {
                this.skCanvas.DrawShapedTextSafe(textLine, x, y, SKTextAlign.Left, font, paint);
                return;
            }

            float currentX = x;

            for (int i = 0; i < words.Length; i++)
            {
                string word = words[i];

                if (word.Length > 0)
                {
                    this.skCanvas.DrawShapedTextSafe(word, currentX, y, SKTextAlign.Left, font, paint);
                    currentX += font.MeasureShapedText(word);
                }

                if (i < gapCount)
                {
                    currentX += gapWidth;
                }
            }
        }

        /// <summary>
        /// Splits ^FB field data at <c>\&amp;</c> line breaks and decodes <c>\\</c> to a backslash.
        /// </summary>
        private static List<string> SplitParagraphs(string text)
        {
            List<string> paragraphs = [];
            StringBuilder paragraph = new();

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '\\' && i + 1 < text.Length)
                {
                    char next = text[i + 1];
                    if (next == '&')
                    {
                        paragraphs.Add(paragraph.ToString());
                        paragraph.Clear();
                        i++;
                        continue;
                    }

                    if (next == '\\')
                    {
                        paragraph.Append('\\');
                        i++;
                        continue;
                    }
                }

                paragraph.Append(c);
            }

            paragraphs.Add(paragraph.ToString());
            return paragraphs;
        }

        /// <summary>
        /// Splits paragraphs into space separated words; the last word of every paragraph but the last ends its line.
        /// </summary>
        private static IEnumerable<(string Word, bool EndsParagraph)> SplitWords(IReadOnlyList<string> paragraphs)
        {
            for (int i = 0; i < paragraphs.Count; i++)
            {
                string[] words = paragraphs[i].Split(' ');
                bool isLastParagraph = i == paragraphs.Count - 1;

                for (int j = 0; j < words.Length; j++)
                {
                    yield return (words[j], !isLastParagraph && j == words.Length - 1);
                }
            }
        }

        private static List<WrappedTextLine> WordWrap(IReadOnlyList<string> paragraphs, SKFont font, int maxWidth)
        {
            float spaceWidth = font.MeasureShapedText(" ");
            List<WrappedTextLine> lines = [];

            StringBuilder line = new();
            float width = 0;
            foreach ((string word, bool endsParagraph) in SplitWords(paragraphs))
            {
                if (endsParagraph)
                {
                    float wordWidth = font.MeasureShapedText(word);
                    if (width + wordWidth <= maxWidth)
                    {
                        line.Append(word);
                        lines.Add(new WrappedTextLine(line.ToString(), shouldJustify: false));
                        line = new StringBuilder();
                        width = 0;
                    }
                    else
                    {
                        if (line.Length > 0)
                        {
                            lines.Add(new WrappedTextLine(line.ToString().Trim(), shouldJustify: true));
                        }
                        lines.Add(new WrappedTextLine(word, shouldJustify: false));
                        line = new StringBuilder();
                        width = 0;
                    }
                }
                else
                {
                    float wordWidth = font.MeasureShapedText(word);
                    if (wordWidth > maxWidth)
                    {
                        if (line.Length > 0)
                        {
                            lines.Add(new WrappedTextLine(line.ToString().Trim(), shouldJustify: true));
                            line = new StringBuilder();
                            width = 0;
                        }

                        List<string> chunks = SplitLongWord(
                            word,
                            maxWidth,
                            value => font.MeasureShapedText(value));

                        AddLongWordChunks(chunks, lines, line, ref width, spaceWidth, value => font.MeasureShapedText(value));
                        continue;
                    }

                    if (width + wordWidth <= maxWidth)
                    {
                        line.Append(word + " ");
                        width += wordWidth + spaceWidth;
                    }
                    else
                    {
                        if (line.Length > 0)
                        {
                            lines.Add(new WrappedTextLine(line.ToString().Trim(), shouldJustify: true));
                        }

                        line = new StringBuilder(word + " ");
                        width = wordWidth + spaceWidth;
                    }
                }
            }

            lines.Add(new WrappedTextLine(line.ToString().Trim(), shouldJustify: false));
            return lines;
        }

        private static List<WrappedTextLine> WordWrap(IReadOnlyList<string> paragraphs, ZplBitmapFontMetrics metrics, int maxWidth)
        {
            int spaceWidth = ZplBitmapFontRenderer.MeasureTextWidth(" ", metrics);
            List<WrappedTextLine> lines = [];

            StringBuilder line = new();
            float width = 0;
            foreach ((string word, bool endsParagraph) in SplitWords(paragraphs))
            {
                if (endsParagraph)
                {
                    int wordWidth = ZplBitmapFontRenderer.MeasureTextWidth(word, metrics);
                    if (width + wordWidth <= maxWidth)
                    {
                        line.Append(word);
                        lines.Add(new WrappedTextLine(line.ToString(), shouldJustify: false));
                        line = new StringBuilder();
                        width = 0;
                    }
                    else
                    {
                        if (line.Length > 0)
                        {
                            lines.Add(new WrappedTextLine(line.ToString().Trim(), shouldJustify: true));
                        }

                        lines.Add(new WrappedTextLine(word, shouldJustify: false));
                        line = new StringBuilder();
                        width = 0;
                    }
                }
                else
                {
                    int wordWidth = MeasureRenderedBitmapTextWidth(word, metrics);
                    if (wordWidth > maxWidth)
                    {
                        if (line.Length > 0)
                        {
                            lines.Add(new WrappedTextLine(line.ToString().Trim(), shouldJustify: true));
                            line = new StringBuilder();
                            width = 0;
                        }

                        List<string> chunks = SplitLongWord(
                            word,
                            maxWidth,
                            value => MeasureRenderedBitmapTextWidth(value, metrics));

                        AddLongWordChunks(
                            chunks,
                            lines,
                            line,
                            ref width,
                            spaceWidth,
                            value => ZplBitmapFontRenderer.MeasureTextWidth(value, metrics));
                        continue;
                    }

                    wordWidth = ZplBitmapFontRenderer.MeasureTextWidth(word, metrics);
                    if (width + wordWidth <= maxWidth)
                    {
                        line.Append(word + " ");
                        width += wordWidth + spaceWidth;
                    }
                    else
                    {
                        if (line.Length > 0)
                        {
                            lines.Add(new WrappedTextLine(line.ToString().Trim(), shouldJustify: true));
                        }

                        line = new StringBuilder(word + " ");
                        width = wordWidth + spaceWidth;
                    }
                }
            }

            lines.Add(new WrappedTextLine(line.ToString().Trim(), shouldJustify: false));
            return lines;
        }

        private static List<string> SplitLongWord(
            string word,
            float maxWidth,
            Func<string, float> measureText)
        {
            List<Rune> runes = [.. word.EnumerateRunes()];
            List<string> chunks = [];
            int start = 0;

            while (start < runes.Count)
            {
                string remaining = CreateString(runes, start, runes.Count - start);

                if (measureText(remaining) <= maxWidth)
                {
                    chunks.Add(remaining);
                    break;
                }

                int runeCount = FindLongestPrefixThatFits(
                    runes,
                    start,
                    maxWidth,
                    measureText,
                    appendHyphen: true);

                if (runeCount <= 0)
                {
                    runeCount = Math.Max(
                        1,
                        FindLongestPrefixThatFits(
                            runes,
                            start,
                            maxWidth,
                            measureText,
                            appendHyphen: false));
                }

                bool hasMoreText = start + runeCount < runes.Count;
                chunks.Add(CreateString(runes, start, runeCount) + (hasMoreText ? "-" : ""));
                start += runeCount;
            }

            return chunks;
        }

        private static int FindLongestPrefixThatFits(
            IReadOnlyList<Rune> runes,
            int start,
            float maxWidth,
            Func<string, float> measureText,
            bool appendHyphen)
        {
            int best = 0;

            for (int count = 1; start + count <= runes.Count; count++)
            {
                string text = CreateString(runes, start, count) + (appendHyphen ? "-" : "");

                if (measureText(text) > maxWidth)
                {
                    break;
                }

                best = count;
            }

            return best;
        }

        private static void AddLongWordChunks(
            List<string> chunks,
            List<WrappedTextLine> lines,
            StringBuilder line,
            ref float width,
            float spaceWidth,
            Func<string, float> measureText)
        {
            for (int i = 0; i < chunks.Count; i++)
            {
                string chunk = chunks[i];
                bool isLastChunk = i == chunks.Count - 1;

                if (!isLastChunk)
                {
                    lines.Add(new WrappedTextLine(chunk, shouldJustify: false));
                    continue;
                }

                line.Append(chunk + " ");
                width = measureText(chunk) + spaceWidth;
            }
        }

        private static string CreateString(IReadOnlyList<Rune> runes, int start, int count)
        {
            StringBuilder builder = new();

            for (int i = start; i < start + count; i++)
            {
                builder.Append(runes[i]);
            }

            return builder.ToString();
        }

        private static int MeasureRenderedBitmapTextWidth(string text, ZplBitmapFontMetrics metrics)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0;
            }

            int runeCount = text.EnumerateRunes().Count();
            return (runeCount - 1) * metrics.Advance + metrics.RenderedGlyphWidth;
        }

        private static ZplBitmapFontMetrics CreateExpandedMetrics(
            ZplBitmapFontMetrics metrics,
            int fontHeight,
            int fontWidth,
            int lineSpacing)
        {
            int expansionY = fontHeight > 0 ?
                FontScale.GetMagnification(fontHeight, metrics.MatrixHeight) :
                1;
            int expansionX = fontWidth > 0 ?
                FontScale.GetMagnification(fontWidth, metrics.MatrixWidth) :
                expansionY;

            if (fontHeight == 0 && fontWidth > 0)
            {
                expansionY = expansionX;
            }

            return metrics.WithExpansion(expansionX, expansionY, lineSpacing);
        }

        private sealed class WrappedTextLine
        {
            public string Text { get; }

            public bool ShouldJustify { get; }

            public WrappedTextLine(string text, bool shouldJustify)
            {
                this.Text = text;
                this.ShouldJustify = shouldJustify;
            }
        }

    }
}
