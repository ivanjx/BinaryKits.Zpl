using BinaryKits.Zpl.Label;
using BinaryKits.Zpl.Label.Elements;
using BinaryKits.Zpl.Viewer.BitmapFonts;
using BinaryKits.Zpl.Viewer.Helpers;

using SkiaSharp;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace BinaryKits.Zpl.Viewer.ElementDrawers
{
    /// <summary>
    /// Drawer for Text Field elements
    /// </summary>
    public class TextFieldElementDrawer : ElementDrawerBase
    {
        private const string FallbackBitmapFontName = "A";

        ///<inheritdoc/>
        public override bool CanDraw(ZplElementBase element)
        {
            return element.GetType() == typeof(ZplTextField);
        }

        ///<inheritdoc/>
        public override bool IsReverseDraw(ZplElementBase element)
        {
            if (element is ZplTextField textField)
            {
                return textField.ReversePrint;
            }

            return false;
        }

        ///<inheritdoc/>
        public override SKPoint Draw(ZplElementBase element, DrawerOptions options, SKPoint currentPosition, InternationalFont internationalFont, int printDensityDpmm)
        {
            if (element is ZplTextField textField)
            {
                float x = textField.PositionX;
                float y = textField.PositionY;

                if (textField.UseDefaultPosition)
                {
                    x = currentPosition.X;
                    y = currentPosition.Y;
                }

                ZplFont font = textField.Font;

                string displayText = textField.Text;
                if (textField.HexadecimalIndicator is char hexIndicator)
                {
                    displayText = displayText.ReplaceHexEscapes(hexIndicator, internationalFont);
                }

                displayText = displayText.ApplyInternationalCharacterSet(font.FontName, internationalFont);

                if (this.TryDrawBitmapTextField(
                        textField,
                        options,
                        currentPosition,
                        internationalFont,
                        printDensityDpmm,
                        displayText,
                        x,
                        y,
                        out SKPoint nextPosition))
                {
                    return nextPosition;
                }

                (float cellHeight, float scaleX) = FontScale.GetFontScaling(font.FontName, font.FontHeight, font.FontWidth, printDensityDpmm);
                bool isScalable = FontScale.IsScalableFont(font.FontName);

                string typefaceFontName = isScalable ? "0" : font.FontName;
                SKTypeface typeface = options.FontManager.FontLoader(typefaceFontName);

                using SKFont skFont = FontScale.CreateTextFont(typeface, font.FontName, cellHeight, scaleX);
                using SKPaint skPaint = new()
                {
                    IsAntialias = options.Antialias
                };

                if (isScalable)
                {
                    if (options.ReplaceDashWithEnDash)
                    {
                        displayText = displayText.Replace("-", " \u2013 ");
                    }

                    if (options.ReplaceUnderscoreWithEnSpace)
                    {
                        displayText = displayText.Replace('_', '\u2002');
                    }
                }

                FieldDirection fieldDirection = textField.FieldDirection;
                float characterGap = textField.CharacterGap;

                // Characters of V (one per row) and R (reversed order) fields
                List<string> characters = null;
                if (fieldDirection != FieldDirection.Horizontal)
                {
                    characters = GetTextElements(displayText);

                    if (fieldDirection == FieldDirection.Reverse)
                    {
                        characters.Reverse();
                        displayText = string.Concat(characters);
                    }
                }

                float totalWidth = skFont.MeasureShapedText(displayText, characterGap, out SKRect textBounds);

                FieldJustification fieldJustification = textField.FieldOrigin != null ?
                    textField.FieldOrigin.FieldJustification :
                    textField.FieldTypeset.FieldJustification;
                bool rightJustified = IsRightJustified(displayText, fieldJustification);

                (float boxWidth, float boxHeight) = fieldDirection switch
                {
                    FieldDirection.Vertical => (cellHeight, GetVerticalFieldHeight(characters.Count, cellHeight, characterGap)),
                    FieldDirection.Reverse => (skFont.MeasureShapedText("W"), cellHeight),
                    _ => (totalWidth, cellHeight)
                };

                if (textField.FieldOrigin != null && rightJustified)
                {
                    x = GetRightJustifiedFieldOriginX(x, boxWidth, boxHeight, textField.Font.FieldOrientation);
                }

                float textOffsetX = GetTextOffsetX(fieldDirection, textField.FieldOrigin != null, rightJustified, totalWidth, boxWidth);

                using (new SKAutoCanvasRestore(this.skCanvas))
                {
                    SKMatrix matrix = CreateFieldRotationMatrix(textField.Font.FieldOrientation, textField.FieldOrigin != null, x, y, boxWidth, boxHeight);

                    if (matrix != SKMatrix.Empty)
                    {
                        this.skCanvas.Concat(matrix);
                    }

                    if (textField.FieldTypeset == null)
                    {
                        y += FontScale.GetTextBaseline(skFont, font.FontName, cellHeight);
                    }

                    if (textField.ReversePrint)
                    {
                        skPaint.BlendMode = SKBlendMode.Xor;
                    }

                    if (fieldDirection == FieldDirection.Vertical)
                    {
                        // One character per cell row, left-aligned in the column (Labelary)
                        for (int i = 0; i < characters.Count; i++)
                        {
                            this.skCanvas.DrawShapedTextSafe(characters[i], x + textOffsetX, y + i * (cellHeight + characterGap), SKTextAlign.Left, skFont, skPaint);
                        }
                    }
                    else
                    {
                        this.skCanvas.DrawShapedTextSafe(displayText, x + textOffsetX, y, SKTextAlign.Left, skFont, skPaint, characterGap);
                    }

                    // Update the next default field position after rendering
                    float advance = fieldDirection == FieldDirection.Vertical ? boxWidth : totalWidth;
                    return this.CalculateNextDefaultPosition(x, y, advance, textBounds.Height, false, textField.Font.FieldOrientation, currentPosition);
                }
            }

            return currentPosition;
        }

        private bool TryDrawBitmapTextField(
            ZplTextField textField,
            DrawerOptions options,
            SKPoint currentPosition,
            InternationalFont internationalFont,
            int printDensityDpmm,
            string displayText,
            float x,
            float y,
            out SKPoint nextPosition)
        {
            nextPosition = currentPosition;

            if (options.TextRenderingMode != ZplTextRenderingMode.BitmapStrict)
            {
                return false;
            }

            ZplFont font = textField.Font;
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
            FieldDirection fieldDirection = textField.FieldDirection;
            int characterGap = textField.CharacterGap;

            // Characters of V (one per row) and R (reversed order) fields
            List<string> characters = null;
            if (fieldDirection != FieldDirection.Horizontal)
            {
                characters = [.. displayText.EnumerateRunes().Select(rune => rune.ToString())];

                if (fieldDirection == FieldDirection.Reverse)
                {
                    characters.Reverse();
                    displayText = string.Concat(characters);
                }
            }

            int totalWidth = ZplBitmapFontRenderer.MeasureTextWidth(displayText, metrics, characterGap);
            float renderedHeight = metrics.RenderedGlyphHeight;
            float topY = textField.FieldTypeset == null ?
                y :
                y - metrics.Baseline * metrics.ExpansionY;
            bool rightJustified = IsRightJustified(displayText, textField.FieldOrigin != null ?
                textField.FieldOrigin.FieldJustification :
                textField.FieldTypeset.FieldJustification);

            (float boxWidth, float boxHeight) = fieldDirection switch
            {
                FieldDirection.Vertical => (renderedHeight, GetVerticalFieldHeight(characters.Count, renderedHeight, characterGap)),
                FieldDirection.Reverse => (metrics.Advance, renderedHeight),
                _ => (totalWidth, renderedHeight)
            };

            if (textField.FieldOrigin != null && rightJustified)
            {
                x = GetRightJustifiedFieldOriginX(x, boxWidth, boxHeight, textField.Font.FieldOrientation);
            }

            using (new SKAutoCanvasRestore(this.skCanvas))
            {
                SKMatrix matrix = CreateFieldRotationMatrix(textField.Font.FieldOrientation, textField.FieldOrigin != null, x, y, boxWidth, boxHeight);

                if (matrix != SKMatrix.Empty)
                {
                    this.skCanvas.Concat(matrix);
                }

                float drawX = x + GetTextOffsetX(fieldDirection, textField.FieldOrigin != null, rightJustified, totalWidth, boxWidth);

                using SKPaint paint = ZplBitmapFontRenderer.CreatePaint(textField.ReversePrint);
                ZplBitmapFontRenderer renderer = new(this.skCanvas);

                if (fieldDirection == FieldDirection.Vertical)
                {
                    for (int i = 0; i < characters.Count; i++)
                    {
                        renderer.DrawText(characters[i], fontData, metrics, drawX, topY + i * (renderedHeight + characterGap), paint);
                    }
                }
                else
                {
                    renderer.DrawText(displayText, fontData, metrics, drawX, topY, paint, characterGap);
                }

                nextPosition = this.CalculateNextDefaultPosition(
                    x,
                    textField.FieldTypeset == null ? y + renderedHeight : y,
                    fieldDirection == FieldDirection.Vertical ? boxWidth : totalWidth,
                    renderedHeight,
                    false,
                    textField.Font.FieldOrientation,
                    currentPosition);
            }

            return true;
        }

        /// <summary>
        /// Height of a V field: one cell row per character, the gap between rows.
        /// </summary>
        private static float GetVerticalFieldHeight(int characterCount, float cellHeight, float characterGap)
        {
            return characterCount <= 0 ?
                cellHeight :
                characterCount * cellHeight + (characterCount - 1) * characterGap;
        }

        /// <summary>
        /// Start of the text along the unrotated field, relative to the (right-justification shifted) origin.
        /// ^FT justifies along the text from its origin. Labelary anchors R fields on one W cell
        /// (<paramref name="boxWidth"/>): the reversed text ends that far right of x, right-justified it starts that
        /// far left of x. Right-justified ^FT V columns end at x.
        /// </summary>
        private static float GetTextOffsetX(FieldDirection fieldDirection, bool useFieldOrigin, bool rightJustified, float textWidth, float boxWidth)
        {
            switch (fieldDirection)
            {
                case FieldDirection.Reverse:
                    if (!rightJustified)
                    {
                        return boxWidth - textWidth;
                    }

                    return useFieldOrigin ? 0 : -boxWidth;
                case FieldDirection.Vertical:
                    return !useFieldOrigin && rightJustified ? -boxWidth : 0;
                default:
                    return !useFieldOrigin && rightJustified ? -textWidth : 0;
            }
        }

        private static List<string> GetTextElements(string text)
        {
            List<string> elements = [];
            TextElementEnumerator enumerator = StringInfo.GetTextElementEnumerator(text);

            while (enumerator.MoveNext())
            {
                elements.Add(enumerator.GetTextElement());
            }

            return elements;
        }

        protected static ZplBitmapFontMetrics CreateExpandedMetrics(
            ZplBitmapFontMetrics metrics,
            int fontHeight,
            int fontWidth)
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

            return metrics.WithExpansion(expansionX, expansionY);
        }

        protected static bool TryGetBitmapFontData(
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

        protected static bool IsRightJustified(string displayText, FieldJustification fieldJustification)
        {
            return fieldJustification == FieldJustification.Right ||
                (fieldJustification == FieldJustification.Auto && IsRightToLeft(displayText));
        }

        private static bool IsRightToLeft(string text)
        {
            HarfBuzzSharp.Buffer buffer = new();
            buffer.AddUtf16(text);
            buffer.GuessSegmentProperties();
            return buffer.Direction == HarfBuzzSharp.Direction.RightToLeft;
        }
    }
}
