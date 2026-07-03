using BinaryKits.Zpl.Label;
using BinaryKits.Zpl.Label.Elements;
using BinaryKits.Zpl.Viewer.BitmapFonts;
using BinaryKits.Zpl.Viewer.Helpers;

using SkiaSharp;

using System;

using ZXing.OneD;

namespace BinaryKits.Zpl.Viewer.ElementDrawers
{
    public class BarcodeUpcAElementDrawer : BarcodeDrawerBase
    {
        private static readonly bool[] guards = new bool[95];

        static BarcodeUpcAElementDrawer()
        {
            int[] guardIndicies = [
                0, 2,
                4, 5, 6, 7, 8, 9,
                46, 48,
                85, 86, 87, 88, 89, 90,
                92, 94
            ];

            foreach (int idx in guardIndicies)
            {
                guards[idx] = true;
            }
        }

        ///<inheritdoc/>
        public override bool CanDraw(ZplElementBase element)
        {
            return element is ZplBarcodeUpcA;
        }

        ///<inheritdoc/>
        public override SKPoint Draw(ZplElementBase element, DrawerOptions options, SKPoint currentPosition, InternationalFont internationalFont, int printDensityDpmm)
        {
            if (element is ZplBarcodeUpcA barcode)
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

                content = content.PadLeft(11, '0').Substring(0, 11);
                string interpretation = content;

                if (barcode.PrintCheckDigit)
                {
                    int checksum = 0;
                    for (int i = 0; i < 11; i++)
                    {
                        checksum += (content[i] - 48) * (i % 2 * 2 + 7);
                    }

                    interpretation = string.Format("{0}{1}", interpretation, checksum % 10);
                }


                EAN13Writer writer = new();
                bool[] result = writer.encode(content.PadLeft(12, '0'));
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
                        this.DrawUpcAInterpretationLine(result, interpretation, x, y, resizedImage.Width, resizedImage.Height, barcode.FieldOrigin != null, barcode.FieldOrientation, barcode.ModuleWidth, options, internationalFont, printDensityDpmm);
                    }
                }

                return this.CalculateNextDefaultPosition(x, y, resizedImage.Width, resizedImage.Height, barcode.FieldOrigin != null, barcode.FieldOrientation, currentPosition);
            }

            return currentPosition;
        }

        private void DrawUpcAInterpretationLine(
            bool[] data,
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

                    using SKBitmap bitmapGuardImage = BoolArrayWithMaskToSKBitmap(data, guards, (int)(bitmapMargin + metrics.RenderedGlyphHeight / 2f), moduleWidth);
                    byte[] bitmapGuardPng = bitmapGuardImage.Encode(SKEncodedImageFormat.Png, 100).ToArray();
                    this.skCanvas.DrawBitmap(SKBitmap.Decode(bitmapGuardPng), x, y + barcodeHeight, SKSamplingOptions.Default);

                    using SKPaint paint = ZplBitmapFontRenderer.CreatePaint(false);
                    for (int i = 0; i < interpretation.Length; i++)
                    {
                        string digit = interpretation[i].ToString();
                        int digitWidth = ZplBitmapFontRenderer.MeasureTextWidth(digit, metrics);
                        this.DrawBitmapDigit(digit, fontData, metrics, x - (bitmapSpacing + digitWidth) / 2f - moduleWidth, y + barcodeHeight + bitmapMargin, paint);
                        x += bitmapSpacing;

                        if (i == 0 || i == 10)
                        {
                            x += moduleWidth * 11;
                        }
                        else if (i == 5)
                        {
                            x += moduleWidth * 4;
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

                using SKBitmap guardImage = BoolArrayWithMaskToSKBitmap(data, guards, (int)(margin + textBounds.Height / 2), moduleWidth);
                byte[] guardPng = guardImage.Encode(SKEncodedImageFormat.Png, 100).ToArray();
                this.skCanvas.DrawBitmap(SKBitmap.Decode(guardPng), x, y + barcodeHeight, SKSamplingOptions.Default);

                for (int i = 0; i < interpretation.Length; i++)
                {
                    string digit = interpretation[i].ToString();
                    skFont.MeasureText(digit, out SKRect digitBounds);
                    this.skCanvas.DrawText(
                        digit,
                        x - (spacing + digitBounds.Width) / 2 - moduleWidth,
                        y + barcodeHeight + textBounds.Height + margin,
                        SKTextAlign.Left,
                        skFont,
                        skPaint);
                    x += spacing;

                    if (i == 0 || i == 10)
                    {
                        x += moduleWidth * 11;
                    }
                    else if (i == 5)
                    {
                        x += moduleWidth * 4;
                    }
                }
            }
        }

    }
}
