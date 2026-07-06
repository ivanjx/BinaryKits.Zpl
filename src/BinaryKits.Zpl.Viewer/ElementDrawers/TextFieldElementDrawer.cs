using BinaryKits.Zpl.Label;
using BinaryKits.Zpl.Label.Elements;
using BinaryKits.Zpl.Viewer.BitmapFonts;
using BinaryKits.Zpl.Viewer.Helpers;

using SkiaSharp;

using System;

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
                FieldJustification fieldJustification = FieldJustification.None;

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

                (float fontSize, float scaleX) = FontScale.GetFontScaling(font.FontName, font.FontHeight, font.FontWidth, printDensityDpmm);

                string typefaceFontName = IsFont0Compatible(font.FontName) ? "0" : font.FontName;
                SKTypeface typeface = options.FontManager.FontLoader(typefaceFontName);

                SKFont skFont = new(typeface, fontSize, scaleX);
                using SKPaint skPaint = new()
                {
                    IsAntialias = options.Antialias
                };

                if (IsFont0Compatible(font.FontName))
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

                skFont.MeasureText("X", out SKRect textBoundBaseline);
                float totalWidth = skFont.MeasureText(displayText, out SKRect textBounds);

                using (new SKAutoCanvasRestore(this.skCanvas))
                {
                    SKMatrix matrix = SKMatrix.Empty;

                    if (textField.FieldOrigin != null)
                    {
                        switch (textField.Font.FieldOrientation)
                        {
                            case FieldOrientation.Rotated90:
                                matrix = SKMatrix.CreateRotationDegrees(90, x + fontSize / 2, y + fontSize / 2);
                                break;
                            case FieldOrientation.Rotated180:
                                matrix = SKMatrix.CreateRotationDegrees(180, x + textBounds.Width / 2, y + fontSize / 2);
                                break;
                            case FieldOrientation.Rotated270:
                                matrix = SKMatrix.CreateRotationDegrees(270, x + textBounds.Width / 2, y + textBounds.Width / 2);
                                break;
                            case FieldOrientation.Normal:
                                break;
                        }

                        fieldJustification = textField.FieldOrigin.FieldJustification;
                    }
                    else
                    {
                        switch (textField.Font.FieldOrientation)
                        {
                            case FieldOrientation.Rotated90:
                                matrix = SKMatrix.CreateRotationDegrees(90, x, y);
                                break;
                            case FieldOrientation.Rotated180:
                                matrix = SKMatrix.CreateRotationDegrees(180, x, y);
                                break;
                            case FieldOrientation.Rotated270:
                                matrix = SKMatrix.CreateRotationDegrees(270, x, y);
                                break;
                            case FieldOrientation.Normal:
                                break;
                        }

                        fieldJustification = textField.FieldTypeset.FieldJustification;
                    }

                    if (matrix != SKMatrix.Empty)
                    {
                        this.skCanvas.Concat(matrix);
                    }

                    if (textField.FieldTypeset == null)
                    {
                        y += textBoundBaseline.Height;
                    }

                    if (textField.ReversePrint)
                    {
                        skPaint.BlendMode = SKBlendMode.Xor;
                    }

                    SKTextAlign textAlign = SKTextAlign.Left;
                    if (fieldJustification == FieldJustification.Left)
                    {
                        textAlign = SKTextAlign.Left;
                    }
                    else if (fieldJustification == FieldJustification.Right)
                    {
                        textAlign = SKTextAlign.Right;
                    }
                    else if (fieldJustification == FieldJustification.Auto)
                    {
                        HarfBuzzSharp.Buffer buffer = new();
                        buffer.AddUtf16(displayText);
                        buffer.GuessSegmentProperties();
                        if (buffer.Direction == HarfBuzzSharp.Direction.RightToLeft)
                        {
                            textAlign = SKTextAlign.Right;
                        }
                    }

                    this.skCanvas.DrawShapedTextSafe(displayText, x, y, textAlign, skFont, skPaint);

                    // Update the next default field position after rendering
                    return this.CalculateNextDefaultPosition(x, y, totalWidth, textBounds.Height, false, textField.Font.FieldOrientation, currentPosition);
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

            ZplBitmapFontMetrics metrics = CreateExpandedMetrics(fontData.Metrics, font.FontHeight, font.FontWidth);
            int totalWidth = ZplBitmapFontRenderer.MeasureTextWidth(displayText, metrics);
            float renderedHeight = metrics.RenderedGlyphHeight;
            float topY = textField.FieldTypeset == null ?
                y :
                y - metrics.Baseline * metrics.ExpansionY;
            FieldJustification fieldJustification;

            using (new SKAutoCanvasRestore(this.skCanvas))
            {
                SKMatrix matrix = SKMatrix.Empty;

                if (textField.FieldOrigin != null)
                {
                    switch (textField.Font.FieldOrientation)
                    {
                        case FieldOrientation.Rotated90:
                            matrix = SKMatrix.CreateRotationDegrees(90, x + renderedHeight / 2, y + renderedHeight / 2);
                            break;
                        case FieldOrientation.Rotated180:
                            matrix = SKMatrix.CreateRotationDegrees(180, x + totalWidth / 2f, y + renderedHeight / 2);
                            break;
                        case FieldOrientation.Rotated270:
                            matrix = SKMatrix.CreateRotationDegrees(270, x + totalWidth / 2f, y + totalWidth / 2f);
                            break;
                        case FieldOrientation.Normal:
                            break;
                    }

                    fieldJustification = textField.FieldOrigin.FieldJustification;
                }
                else
                {
                    switch (textField.Font.FieldOrientation)
                    {
                        case FieldOrientation.Rotated90:
                            matrix = SKMatrix.CreateRotationDegrees(90, x, y);
                            break;
                        case FieldOrientation.Rotated180:
                            matrix = SKMatrix.CreateRotationDegrees(180, x, y);
                            break;
                        case FieldOrientation.Rotated270:
                            matrix = SKMatrix.CreateRotationDegrees(270, x, y);
                            break;
                        case FieldOrientation.Normal:
                            break;
                    }

                    fieldJustification = textField.FieldTypeset.FieldJustification;
                }

                if (matrix != SKMatrix.Empty)
                {
                    this.skCanvas.Concat(matrix);
                }

                float drawX = GetBitmapAlignedX(displayText, x, totalWidth, fieldJustification);

                using SKPaint paint = ZplBitmapFontRenderer.CreatePaint(textField.ReversePrint);
                ZplBitmapFontRenderer renderer = new(this.skCanvas);
                renderer.DrawText(displayText, fontData, metrics, drawX, topY, paint);

                nextPosition = this.CalculateNextDefaultPosition(
                    x,
                    textField.FieldTypeset == null ? y + renderedHeight : y,
                    totalWidth,
                    renderedHeight,
                    false,
                    textField.Font.FieldOrientation,
                    currentPosition);
            }

            return true;
        }

        private static ZplBitmapFontMetrics CreateExpandedMetrics(
            ZplBitmapFontMetrics metrics,
            int fontHeight,
            int fontWidth)
        {
            int expansionY = fontHeight > 0 ?
                GetExpansion(fontHeight, metrics.MatrixHeight) :
                1;
            int expansionX = fontWidth > 0 ?
                GetExpansion(fontWidth, metrics.MatrixWidth) :
                expansionY;

            if (fontHeight == 0 && fontWidth > 0)
            {
                expansionY = expansionX;
            }

            return metrics.WithExpansion(expansionX, expansionY);
        }

        private static int GetExpansion(int requestedSize, int matrixSize)
        {
            return (int)Math.Max(1, Math.Round((double)requestedSize / matrixSize));
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

        private static float GetBitmapAlignedX(
            string displayText,
            float x,
            int totalWidth,
            FieldJustification fieldJustification)
        {
            if (fieldJustification == FieldJustification.Right)
            {
                return x - totalWidth;
            }

            if (fieldJustification == FieldJustification.Auto && IsRightToLeft(displayText))
            {
                return x - totalWidth;
            }

            return x;
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
