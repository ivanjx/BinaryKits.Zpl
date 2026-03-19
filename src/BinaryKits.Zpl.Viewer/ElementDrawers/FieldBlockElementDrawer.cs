using BinaryKits.Zpl.Label;
using BinaryKits.Zpl.Label.Elements;
using BinaryKits.Zpl.Viewer.Helpers;

using SkiaSharp;
using SkiaSharp.HarfBuzz;

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

                (float fontSize, float scaleX) = FontScale.GetFontScaling(font.FontName, font.FontHeight, font.FontWidth, printDensityDpmm);

                SKTypeface typeface = options.FontManager.FontLoader(font.FontName);
                string text = fieldBlock.Text;
                if (fieldBlock.HexadecimalIndicator is char hexIndicator)
                {
                    text = text.ReplaceHexEscapes(hexIndicator, internationalFont);
                }

                if (font.FontName == "0")
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

                SKFont skFont = new(typeface, fontSize, scaleX);
                using SKPaint skPaint = new()
                {
                    IsAntialias = options.Antialias
                };

                skFont.MeasureText("X", out SKRect textBoundBaseline);

                float x = fieldBlock.PositionX;
                float y = fieldBlock.PositionY + textBoundBaseline.Height;

                if (fieldBlock.UseDefaultPosition)
                {
                    x = currentPosition.X;
                    y = currentPosition.Y + textBoundBaseline.Height;
                }

                List<WrappedTextLine> textLines = WordWrap(text, skFont, fieldBlock.Width);
                int hangingIndent = 0;
                float lineHeight = fontSize + fieldBlock.LineSpace;

                // actual ZPL printer does not include trailing line spacing in total height
                float totalHeight = lineHeight * fieldBlock.MaxLineCount - fieldBlock.LineSpace;
                // labelary
                //var totalHeight = lineHeight * fieldBlock.MaxLineCount;

                if (fieldBlock.FieldTypeset != null)
                {
                    totalHeight = lineHeight * (fieldBlock.MaxLineCount - 1) + textBoundBaseline.Height;
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
                                matrix = SKMatrix.CreateRotationDegrees(90, fieldBlock.PositionX + totalHeight / 2, fieldBlock.PositionY + totalHeight / 2);
                                break;
                            case FieldOrientation.Rotated180:
                                matrix = SKMatrix.CreateRotationDegrees(180, fieldBlock.PositionX + fieldBlock.Width / 2, fieldBlock.PositionY + totalHeight / 2);
                                break;
                            case FieldOrientation.Rotated270:
                                matrix = SKMatrix.CreateRotationDegrees(270, fieldBlock.PositionX + fieldBlock.Width / 2, fieldBlock.PositionY + fieldBlock.Width / 2);
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
                                matrix = SKMatrix.CreateRotationDegrees(90, fieldBlock.PositionX, fieldBlock.PositionY);
                                break;
                            case FieldOrientation.Rotated180:
                                matrix = SKMatrix.CreateRotationDegrees(180, fieldBlock.PositionX, fieldBlock.PositionY);
                                break;
                            case FieldOrientation.Rotated270:
                                matrix = SKMatrix.CreateRotationDegrees(270, fieldBlock.PositionX, fieldBlock.PositionY);
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

                    float clipTop = y - textBoundBaseline.Height;
                    this.skCanvas.ClipRect(new SKRect(
                        fieldBlock.PositionX,
                        clipTop,
                        fieldBlock.PositionX + fieldBlock.Width,
                        clipTop + totalHeight));
                    int lineIndex = 0;

                    foreach (WrappedTextLine wrappedLine in textLines)
                    {
                        string textLine = wrappedLine.Text;
                        int visibleLineIndex = Math.Min(lineIndex, Math.Max(fieldBlock.MaxLineCount - 1, 0));
                        float lineY = y + visibleLineIndex * lineHeight;
                        x = fieldBlock.PositionX + hangingIndent;

                        skFont.MeasureText(textLine, out SKRect textBounds);
                        float diff = fieldBlock.Width - textBounds.Width;

                        switch (fieldBlock.TextJustification)
                        {
                            case TextJustification.Center:
                                x += diff / 2 - textBounds.Left;
                                break;
                            case TextJustification.Right:
                                x += diff - textBounds.Left * 2;
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
                            float currentLineIndent = x - fieldBlock.PositionX;
                            float availableWidth = Math.Max(fieldBlock.Width - currentLineIndent, 1);
                            this.DrawJustifiedTextLine(textLine, x, lineY, availableWidth, skFont, skPaint);
                        }
                        else
                        {
                            this.skCanvas.DrawShapedText(textLine, x, lineY, skFont, skPaint);
                        }

                        lineIndex++;
                    }

                    return this.CalculateNextDefaultPosition(fieldBlock.PositionX, fieldBlock.PositionY, fieldBlock.Width, totalHeight, fieldBlock.FieldOrigin != null, fieldBlock.Font.FieldOrientation, currentPosition);
                }
            }

            return currentPosition;
        }

        private void DrawJustifiedTextLine(string textLine, float x, float y, float lineWidth, SKFont font, SKPaint paint)
        {
            string[] words = textLine.Split([' '], StringSplitOptions.None);

            if (words.Length <= 1)
            {
                this.skCanvas.DrawShapedText(textLine, x, y, font, paint);
                return;
            }

            float wordsWidth = 0;

            foreach (string word in words)
            {
                wordsWidth += font.MeasureText(word);
            }

            int gapCount = words.Length - 1;
            float gapWidth = (lineWidth - wordsWidth) / gapCount;

            if (!float.IsFinite(gapWidth) || gapWidth <= 0)
            {
                this.skCanvas.DrawShapedText(textLine, x, y, font, paint);
                return;
            }

            float currentX = x;

            for (int i = 0; i < words.Length; i++)
            {
                string word = words[i];

                if (word.Length > 0)
                {
                    this.skCanvas.DrawShapedText(word, currentX, y, font, paint);
                    currentX += font.MeasureText(word);
                }

                if (i < gapCount)
                {
                    currentX += gapWidth;
                }
            }
        }

        private static List<WrappedTextLine> WordWrap(string text, SKFont font, int maxWidth)
        {
            float spaceWidth = font.MeasureText(" ");
            List<WrappedTextLine> lines = [];

            Stack<string> words = new(text.Split([' '], StringSplitOptions.None).AsEnumerable().Reverse());
            StringBuilder line = new();
            float width = 0;
            while (words.Count != 0)
            {
                string word = words.Pop();
                if (word.Contains(@"\&"))
                {
                    string[] subwords = word.Split([@"\&"], 2, StringSplitOptions.None);
                    word = subwords[0];
                    words.Push(subwords[1]);
                    float wordWidth = font.MeasureText(word);
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
                    float wordWidth = font.MeasureText(word);
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
