using BinaryKits.Zpl.Label;
using BinaryKits.Zpl.Label.Elements;
using BinaryKits.Zpl.Viewer.BitmapFonts;
using BinaryKits.Zpl.Viewer.Helpers;

using SkiaSharp;

using System;

using ZXing.OneD;

namespace BinaryKits.Zpl.Viewer.ElementDrawers
{
    /// <summary>
    /// Drawer for EAN-8 Barcode elements
    /// </summary>
    public class BarcodeEAN8ElementDrawer : BarcodeDrawerBase
    {
        private static readonly bool[] guards = new bool[67];

        static BarcodeEAN8ElementDrawer()
        {
            foreach (int idx in new[] { 0, 2, 32, 34, 64, 66 })
            {
                guards[idx] = true;
            }
        }

        ///<inheritdoc/>
        public override bool CanDraw(ZplElementBase element)
        {
            return element is ZplBarcodeEan8;
        }

        ///<inheritdoc/>
        public override SKPoint Draw(ZplElementBase element, DrawerOptions options, SKPoint currentPosition, InternationalFont internationalFont, int printDensityDpmm)
        {
            if (element is ZplBarcodeEan8 barcode)
            {
                float x = barcode.PositionX;
                float y = barcode.PositionY;

                if (barcode.UseDefaultPosition)
                {
                    x = currentPosition.X;
                    y = currentPosition.Y;
                }

                string content = barcode.Content;
                if (barcode.HexadecimalIndicator is char hexIndicator)
                {
                    content = content.ReplaceHexEscapes(hexIndicator, internationalFont);
                }

                content = content.PadLeft(7, '0').Substring(0, 7);
                string interpretation = content;

                int checksum = 0;
                for (int i = 0; i < 7; i++)
                {
                    checksum += (content[i] - 48) * (i % 2 * 2 + 3);
                }

                interpretation = string.Format("{0}{1}", interpretation, checksum % 10);

                EAN8Writer writer = new();
                bool[] result = writer.encode(content);
                using SKBitmap resizedImage = BoolArrayToSKBitmap(result, barcode.Height, barcode.ModuleWidth);
                byte[] png = resizedImage.Encode(SKEncodedImageFormat.Png, 100).ToArray();
                this.DrawBarcode(png, x, y, resizedImage.Width, resizedImage.Height, barcode.FieldOrigin != null, barcode.FieldOrientation);

                if (barcode.PrintInterpretationLine)
                {
                    if (barcode.PrintInterpretationLineAboveCode)
                    {
                        this.DrawBitmapInterpretationLine(interpretation, x, y, resizedImage.Width, resizedImage.Height, barcode.FieldOrigin != null, barcode.FieldOrientation, true, options, internationalFont, printDensityDpmm, barcode.ModuleWidth);
                    }
                    else
                    {
                        this.DrawEAN8InterpretationLine(interpretation, x, y, resizedImage.Width, resizedImage.Height, barcode.FieldOrigin != null, barcode.FieldOrientation, barcode.ModuleWidth, options, internationalFont, printDensityDpmm);
                    }
                }

                return this.CalculateNextDefaultPosition(x, y, resizedImage.Width, resizedImage.Height, barcode.FieldOrigin != null, barcode.FieldOrientation, currentPosition);
            }

            return currentPosition;
        }

        private void DrawEAN8InterpretationLine(
            string interpretation,
            float x,
            float y,
            int barcodeWidth,
            int barcodeHeight,
            bool useFieldOrigin,
            FieldOrientation fieldOrientation,
            int moduleWidth,
            DrawerOptions options,
            InternationalFont internationalFont,
            int printDensityDpmm)
        {
            using (new SKAutoCanvasRestore(this.skCanvas))
            {
                SKMatrix matrix = GetRotationMatrix(x, y, barcodeWidth, barcodeHeight, useFieldOrigin, fieldOrientation);

                if (matrix != SKMatrix.Empty)
                {
                    SKMatrix currentMatrix = this.skCanvas.TotalMatrix;
                    SKMatrix concatMatrix = SKMatrix.Concat(currentMatrix, matrix);
                    this.skCanvas.SetMatrix(concatMatrix);
                }

                if (!useFieldOrigin)
                {
                    y -= barcodeHeight;
                    if (y < 0)
                    {
                        y = 0;
                    }
                }

                if (this.TryGetBitmapInterpretationLineFont(options, internationalFont, printDensityDpmm, moduleWidth, out ZplBitmapFontData fontData, out ZplBitmapFontMetrics metrics))
                {
                    float bitmapMargin = MIN_LABEL_MARGIN;
                    int bitmapSpacing = moduleWidth * 7;

                    using SKBitmap bitmapGuardImage = BoolArrayToSKBitmap(guards, (int)(bitmapMargin + metrics.RenderedGlyphHeight / 2f), moduleWidth);
                    byte[] bitmapGuardPng = bitmapGuardImage.Encode(SKEncodedImageFormat.Png, 100).ToArray();
                    this.skCanvas.DrawBitmap(SKBitmap.Decode(bitmapGuardPng), x, y + barcodeHeight, SKSamplingOptions.Default);

                    using SKPaint paint = ZplBitmapFontRenderer.CreatePaint(false);
                    float bitmapBaseX = x + moduleWidth * 3;
                    for (int i = 0; i < interpretation.Length; i++)
                    {
                        string digit = interpretation[i].ToString();
                        int digitWidth = ZplBitmapFontRenderer.MeasureTextWidth(digit, metrics);
                        float drawX = bitmapBaseX + (bitmapSpacing - digitWidth) / 2f;
                        this.DrawBitmapDigit(digit, fontData, metrics, drawX, y + barcodeHeight + bitmapMargin, paint);
                        bitmapBaseX += bitmapSpacing;

                        if (i == 3)
                        {
                            bitmapBaseX += moduleWidth * 5;
                        }
                    }

                    return;
                }

                float labelFontSize = FontScale.GetBitmappedFontSize("A", Math.Min(moduleWidth, 10), printDensityDpmm).Value;
                SKTypeface labelTypeFace = options.FontManager.FontLoader("A");
                using SKFont skFont = new(labelTypeFace, labelFontSize);
                using SKPaint skPaint = new()
                {
                    IsAntialias = options.Antialias
                };

                skFont.MeasureText(interpretation, out SKRect textBounds);
                float margin = Math.Max((skFont.Spacing - textBounds.Height) / 2, MIN_LABEL_MARGIN);
                int spacing = moduleWidth * 7;

                using SKBitmap guardImage = BoolArrayToSKBitmap(guards, (int)(margin + textBounds.Height / 2), moduleWidth);
                byte[] guardPng = guardImage.Encode(SKEncodedImageFormat.Png, 100).ToArray();
                this.skCanvas.DrawBitmap(SKBitmap.Decode(guardPng), x, y + barcodeHeight, SKSamplingOptions.Default);

                float baseX = x + moduleWidth * 3;
                for (int i = 0; i < interpretation.Length; i++)
                {
                    string digit = interpretation[i].ToString();
                    skFont.MeasureText(digit, out SKRect digitBounds);
                    float drawX = baseX + (spacing - digitBounds.Width) / 2;
                    this.skCanvas.DrawText(
                        digit,
                        drawX,
                        y + barcodeHeight + textBounds.Height + margin,
                        SKTextAlign.Left,
                        skFont,
                        skPaint);
                    baseX += spacing;

                    if (i == 3)
                    {
                        baseX += moduleWidth * 5;
                    }
                }
            }
        }
    }
}
