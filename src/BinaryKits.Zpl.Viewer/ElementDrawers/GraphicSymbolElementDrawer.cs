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
    public class GraphicSymbolElementDrawer : ElementDrawerBase
    {
        ///<inheritdoc/>
        public override bool CanDraw(ZplElementBase element)
        {
            return element.GetType() == typeof(ZplGraphicSymbol);
        }

        ///<inheritdoc/>
        public override SKPoint Draw(ZplElementBase element, DrawerOptions options, SKPoint currentPosition, InternationalFont internationalFont, int printDensityDpmm)
        {
            if (element is ZplGraphicSymbol graphicSymbol)
            {
                float x = graphicSymbol.PositionX;
                float y = graphicSymbol.PositionY;

                if (graphicSymbol.UseDefaultPosition)
                {
                    x = currentPosition.X;
                    y = currentPosition.Y;
                }

                string displayText = $"{(char?)graphicSymbol.Character}";

                if (this.TryDrawBitmapGraphicSymbol(
                        graphicSymbol,
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

                (float cellHeight, float scaleX) = FontScale.GetFontScaling("GS", graphicSymbol.Height, graphicSymbol.Width, printDensityDpmm);

                SKTypeface typeface = options.FontManager.TypefaceGS;

                // The symbols fill the whole cell: ZplGS.ttf draws them at its cap height
                using SKFont skFont = FontScale.CreateCapHeightFont(typeface, cellHeight, scaleX);
                using SKPaint skPaint = new()
                {
                    IsAntialias = options.Antialias
                };

                float totalWidth = skFont.MeasureText(displayText, out SKRect textBounds);
                bool rightJustified = GetFieldJustification(graphicSymbol) == FieldJustification.Right;

                if (graphicSymbol.FieldOrigin != null && rightJustified)
                {
                    x = GetRightJustifiedFieldOriginX(x, totalWidth, cellHeight, graphicSymbol.FieldOrientation);
                }

                using (new SKAutoCanvasRestore(this.skCanvas))
                {
                    SKMatrix matrix = SKMatrix.Empty;

                    if (graphicSymbol.FieldOrigin != null)
                    {
                        switch (graphicSymbol.FieldOrientation)
                        {
                            case FieldOrientation.Rotated90:
                                matrix = SKMatrix.CreateRotationDegrees(90, x + cellHeight / 2, y + cellHeight / 2);
                                break;
                            case FieldOrientation.Rotated180:
                                matrix = SKMatrix.CreateRotationDegrees(180, x + textBounds.Width / 2, y + cellHeight / 2);
                                break;
                            case FieldOrientation.Rotated270:
                                matrix = SKMatrix.CreateRotationDegrees(270, x + textBounds.Width / 2, y + textBounds.Width / 2);
                                break;
                            case FieldOrientation.Normal:
                                break;
                        }
                    }
                    else
                    {
                        switch (graphicSymbol.FieldOrientation)
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
                    }

                    if (matrix != SKMatrix.Empty)
                    {
                        this.skCanvas.Concat(matrix);
                    }

                    if (graphicSymbol.FieldTypeset == null)
                    {
                        y += cellHeight;
                    }

                    // ^FT justifies along the text from its origin
                    SKTextAlign textAlign = graphicSymbol.FieldOrigin == null && rightJustified ?
                        SKTextAlign.Right :
                        SKTextAlign.Left;

                    this.skCanvas.DrawText(displayText, x, y, textAlign, skFont, skPaint);

                    // Update the next default field position after rendering
                    return this.CalculateNextDefaultPosition(x, y, totalWidth, textBounds.Height, false, graphicSymbol.FieldOrientation, currentPosition);
                }
            }

            return currentPosition;
        }

        private bool TryDrawBitmapGraphicSymbol(
            ZplGraphicSymbol graphicSymbol,
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

            if (options.BitmapFontProvider == null ||
                !options.BitmapFontProvider.TryGet("GS", printDensityDpmm, internationalFont, out ZplBitmapFontData fontData))
            {
                return false;
            }

            ZplBitmapFontMetrics metrics = CreateExpandedMetrics(
                fontData.Metrics,
                graphicSymbol.Height,
                graphicSymbol.Width);
            int totalWidth = ZplBitmapFontRenderer.MeasureTextWidth(displayText, metrics);
            float renderedHeight = metrics.RenderedGlyphHeight;
            float topY = graphicSymbol.FieldTypeset == null ?
                y :
                y - metrics.Baseline * metrics.ExpansionY;
            bool rightJustified = GetFieldJustification(graphicSymbol) == FieldJustification.Right;

            if (graphicSymbol.FieldOrigin != null && rightJustified)
            {
                x = GetRightJustifiedFieldOriginX(x, totalWidth, renderedHeight, graphicSymbol.FieldOrientation);
            }

            using (new SKAutoCanvasRestore(this.skCanvas))
            {
                SKMatrix matrix = SKMatrix.Empty;

                if (graphicSymbol.FieldOrigin != null)
                {
                    switch (graphicSymbol.FieldOrientation)
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
                }
                else
                {
                    switch (graphicSymbol.FieldOrientation)
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
                }

                if (matrix != SKMatrix.Empty)
                {
                    this.skCanvas.Concat(matrix);
                }

                // ^FT justifies along the text from its origin
                float drawX = graphicSymbol.FieldOrigin == null && rightJustified ?
                    x - totalWidth :
                    x;

                using SKPaint paint = ZplBitmapFontRenderer.CreatePaint(false);
                ZplBitmapFontRenderer renderer = new(this.skCanvas);
                renderer.DrawText(displayText, fontData, metrics, drawX, topY, paint);

                nextPosition = this.CalculateNextDefaultPosition(
                    x,
                    graphicSymbol.FieldTypeset == null ? y + renderedHeight : y,
                    totalWidth,
                    renderedHeight,
                    false,
                    graphicSymbol.FieldOrientation,
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

        private static FieldJustification GetFieldJustification(ZplGraphicSymbol graphicSymbol)
        {
            return graphicSymbol.FieldOrigin != null ?
                graphicSymbol.FieldOrigin.FieldJustification :
                graphicSymbol.FieldTypeset.FieldJustification;
        }

    }
}
