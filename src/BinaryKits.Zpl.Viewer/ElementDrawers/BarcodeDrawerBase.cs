using BinaryKits.Zpl.Label;
using BinaryKits.Zpl.Viewer.BitmapFonts;

using SkiaSharp;
using SkiaSharp.HarfBuzz;

using System;
using System.Collections.Generic;
using System.Linq;

using ZXing.Common;

namespace BinaryKits.Zpl.Viewer.ElementDrawers
{
    /// <summary>
    /// Base clase for Barcode element drawers
    /// </summary>
    public abstract class BarcodeDrawerBase : ElementDrawerBase
    {
        /// <summary>
        /// Minimum acceptable magin between a barcode and its interpretation line, in pixels
        /// </summary>
        protected const float MIN_LABEL_MARGIN = 5f;
        protected const string INTERPRETATION_LINE_FONT_NAME = "A";

        protected void DrawBarcode(byte[] barcodeImageData, float x, float y, int barcodeWidth, int barcodeHeight, bool useFieldOrigin, Label.FieldOrientation fieldOrientation)
        {
            using (new SKAutoCanvasRestore(this.skCanvas))
            {
                SKMatrix matrix = GetRotationMatrix(x, y, barcodeWidth, barcodeHeight, useFieldOrigin, fieldOrientation);
                if (!useFieldOrigin)
                {
                    y -= barcodeHeight;
                    if (y < 0)
                    {
                        y = 0;
                    }
                }

                if (matrix != SKMatrix.Empty)
                {
                    this.skCanvas.Concat(matrix);
                }

                this.skCanvas.DrawBitmap(SKBitmap.Decode(barcodeImageData), x, y, SKSamplingOptions.Default);
            }
        }

        protected void DrawInterpretationLine(string interpretation, SKFont skFont, float x, float y, int barcodeWidth, int barcodeHeight, bool useFieldOrigin, Label.FieldOrientation fieldOrientation, bool printInterpretationLineAboveCode, DrawerOptions options)
        {
            using (new SKAutoCanvasRestore(this.skCanvas))
            {
                using SKPaint skPaint = new()
                {
                    IsAntialias = options.Antialias
                };

                SKMatrix matrix = GetRotationMatrix(x, y, barcodeWidth, barcodeHeight, useFieldOrigin, fieldOrientation);
                if (matrix != SKMatrix.Empty)
                {
                    this.skCanvas.Concat(matrix);
                }

                skFont.MeasureText(interpretation, out SKRect textBounds);
                x += (barcodeWidth - textBounds.Width) / 2;
                if (!useFieldOrigin)
                {
                    y -= barcodeHeight;
                    if (y < 0)
                    {
                        y = 0;
                    }
                }

                float margin = Math.Max((skFont.Spacing - textBounds.Height) / 2, MIN_LABEL_MARGIN);
                if (printInterpretationLineAboveCode)
                {
                    this.skCanvas.DrawShapedText(interpretation, x, y - margin, SKTextAlign.Left, skFont, skPaint);
                }
                else
                {
                    this.skCanvas.DrawShapedText(
                        interpretation,
                        x,
                        y + barcodeHeight + textBounds.Height + margin,
                        SKTextAlign.Left,
                        skFont,
                        skPaint);
                }
            }
        }

        protected void DrawBitmapInterpretationLine(
            string interpretation,
            float x,
            float y,
            int barcodeWidth,
            int barcodeHeight,
            bool useFieldOrigin,
            Label.FieldOrientation fieldOrientation,
            bool printInterpretationLineAboveCode,
            DrawerOptions options,
            InternationalFont internationalFont,
            int printDensityDpmm,
            int moduleWidth)
        {
            if (!TryGetBitmapInterpretationLineFont(options, internationalFont, printDensityDpmm, moduleWidth, out ZplBitmapFontData fontData, out ZplBitmapFontMetrics metrics))
            {
                float labelFontSize = Helpers.FontScale.GetBitmappedFontSize(INTERPRETATION_LINE_FONT_NAME, Math.Min(moduleWidth, 10), printDensityDpmm).Value;
                SKTypeface labelTypeFace = options.FontManager.FontLoader(INTERPRETATION_LINE_FONT_NAME);
                using SKFont labelFont = new(labelTypeFace, labelFontSize);
                this.DrawInterpretationLine(interpretation, labelFont, x, y, barcodeWidth, barcodeHeight, useFieldOrigin, fieldOrientation, printInterpretationLineAboveCode, options);
                return;
            }

            using (new SKAutoCanvasRestore(this.skCanvas))
            {
                SKMatrix matrix = GetRotationMatrix(x, y, barcodeWidth, barcodeHeight, useFieldOrigin, fieldOrientation);
                if (matrix != SKMatrix.Empty)
                {
                    this.skCanvas.Concat(matrix);
                }

                int textWidth = ZplBitmapFontRenderer.MeasureTextWidth(interpretation, metrics);
                float drawX = (float)Math.Round(x + (barcodeWidth - textWidth) / 2f);

                if (!useFieldOrigin)
                {
                    y -= barcodeHeight;
                    if (y < 0)
                    {
                        y = 0;
                    }
                }

                float drawY = printInterpretationLineAboveCode ?
                    y - MIN_LABEL_MARGIN - metrics.RenderedGlyphHeight :
                    y + barcodeHeight + MIN_LABEL_MARGIN;

                using SKPaint paint = ZplBitmapFontRenderer.CreatePaint(false);
                ZplBitmapFontRenderer renderer = new(this.skCanvas);
                renderer.DrawText(interpretation, fontData, metrics, drawX, drawY, paint);
            }
        }

        protected bool TryGetBitmapInterpretationLineFont(
            DrawerOptions options,
            InternationalFont internationalFont,
            int printDensityDpmm,
            int moduleWidth,
            out ZplBitmapFontData fontData,
            out ZplBitmapFontMetrics metrics)
        {
            fontData = null;
            metrics = null;

            if (options.TextRenderingMode != ZplTextRenderingMode.BitmapStrict ||
                options.BitmapFontProvider == null ||
                !options.BitmapFontProvider.TryGet(INTERPRETATION_LINE_FONT_NAME, printDensityDpmm, internationalFont, out fontData))
            {
                return false;
            }

            int expansion = Math.Max(1, Math.Min(moduleWidth, 10));
            metrics = fontData.Metrics.WithExpansion(expansion, expansion);
            return true;
        }

        protected void DrawBitmapDigit(
            string digit,
            ZplBitmapFontData fontData,
            ZplBitmapFontMetrics metrics,
            float x,
            float y,
            SKPaint paint)
        {
            ZplBitmapFontRenderer renderer = new(this.skCanvas);
            renderer.DrawText(digit, fontData, metrics, x, y, paint);
        }

        protected static SKMatrix GetRotationMatrix(float x, float y, int width, int height, bool useFieldOrigin, Label.FieldOrientation fieldOrientation)
        {
            SKMatrix matrix = SKMatrix.Empty;
            if (useFieldOrigin)
            {
                switch (fieldOrientation)
                {
                    case Label.FieldOrientation.Rotated90:
                        matrix = SKMatrix.CreateRotationDegrees(90, x + height / 2, y + height / 2);
                        break;
                    case Label.FieldOrientation.Rotated180:
                        matrix = SKMatrix.CreateRotationDegrees(180, x + width / 2, y + height / 2);
                        break;
                    case Label.FieldOrientation.Rotated270:
                        matrix = SKMatrix.CreateRotationDegrees(270, x + width / 2, y + width / 2);
                        break;
                    case Label.FieldOrientation.Normal:
                        break;
                }
            }
            else
            {
                switch (fieldOrientation)
                {
                    case Label.FieldOrientation.Rotated90:
                        matrix = SKMatrix.CreateRotationDegrees(90, x, y);
                        break;
                    case Label.FieldOrientation.Rotated180:
                        matrix = SKMatrix.CreateRotationDegrees(180, x, y);
                        break;
                    case Label.FieldOrientation.Rotated270:
                        matrix = SKMatrix.CreateRotationDegrees(270, x, y);
                        break;
                    case Label.FieldOrientation.Normal:
                        break;
                }
            }

            return matrix;
        }

        protected static SKBitmap BoolArrayToSKBitmap(bool[] array, int height, int moduleWidth = 1)
        {
            using SKBitmap image = new(array.Length, 1);
            for (int col = 0; col < array.Length; col++)
            {
                SKColor color = array[col] ? SKColors.Black : SKColors.Transparent;
                image.SetPixel(col, 0, color);
            }

            SKSamplingOptions sampling = new(SKFilterMode.Nearest);
            return image.Resize(new SKSizeI(image.Width * moduleWidth, height), sampling);
        }

        protected static SKBitmap BoolArrayWithMaskToSKBitmap(bool[] array, bool[] mask, int height, int moduleWidth = 1)
        {
            using SKBitmap image = new(array.Length, 1);
            for (int col = 0; col < array.Length; col++)
            {
                SKColor color = array[col] && mask[col] ? SKColors.Black : SKColors.Transparent;
                image.SetPixel(col, 0, color);
            }

            SKSamplingOptions sampling = new(SKFilterMode.Nearest);
            return image.Resize(new SKSizeI(image.Width * moduleWidth, height), sampling);
        }

        protected static SKBitmap BitMatrixToSKBitmap(BitMatrix matrix, int pixelScale)
        {
            using SKBitmap image = new(matrix.Width, matrix.Height);
            for (int row = 0; row < matrix.Height; row++)
            {
                for (int col = 0; col < matrix.Width; col++)
                {
                    SKColor color = matrix[col, row] ? SKColors.Black : SKColors.Transparent;
                    image.SetPixel(col, row, color);
                }
            }

            SKSamplingOptions sampling = new(SKFilterMode.Nearest);
            return image.Resize(new SKSizeI(image.Width * pixelScale, image.Height * pixelScale), sampling);
        }

        protected static bool[] AdjustWidths(bool[] array, int wide, int narrow)
        {
            List<bool> result = [];
            bool last = true;
            int count = 0;
            foreach (bool current in array)
            {
                if (current != last)
                {
                    result.AddRange(Enumerable.Repeat(last, count == 1 ? narrow : wide));
                    last = current;
                    count = 0;
                }

                count += 1;
            }

            result.AddRange(Enumerable.Repeat(last, narrow));
            return result.ToArray();
        }
    }
}
