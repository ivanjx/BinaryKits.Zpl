using BinaryKits.Zpl.Label;
using BinaryKits.Zpl.Label.Elements;
using BinaryKits.Zpl.Viewer.BitmapFonts;
using BinaryKits.Zpl.Viewer.Helpers;

using SkiaSharp;

using System;

using ZXing.OneD;

namespace BinaryKits.Zpl.Viewer.ElementDrawers
{
    public class BarcodeUpcEElementDrawer : BarcodeDrawerBase
    {
        private static readonly bool[] guards = new bool[51];

        static BarcodeUpcEElementDrawer()
        {
            foreach (int idx in new int[] { 0, 2, 46, 48, 50 })
            {
                guards[idx] = true;
            }
        }

        ///<inheritdoc/>
        public override bool CanDraw(ZplElementBase element)
        {
            return element is ZplBarcodeUpcE;
        }

        ///<inheritdoc/>
        public override SKPoint Draw(ZplElementBase element, DrawerOptions options, SKPoint currentPosition, InternationalFont internationalFont, int printDensityDpmm)
        {
            if (element is ZplBarcodeUpcE barcode)
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

                // [S]DDDDDD[C]
                if (content.Length < 7)
                {
                    // number system 0
                    content = content.PadLeft(7, '0');
                }
                else if (content.Length <= 8)
                {
                    // ignore user provided checksum
                    content = content.Substring(0, 7);
                }
                else
                {
                    // UPC-A to UPC-E
                    string numberSystem = "0";
                    content = content.PadRight(10, '0');
                    if (content.Length > 10)
                    {
                        numberSystem = content.Substring(0, 1);
                        content = content.Substring(1, 10);
                    }

                    int manufacturer = int.Parse(content.Substring(0, 5));
                    int product = int.Parse(content.Substring(5, 5));

                    if (manufacturer % 100 == 0)
                    {
                        int trail = manufacturer / 100 % 10;
                        if (trail <= 2)
                        {
                            content = $"{numberSystem}{manufacturer / 1000:D2}{product % 1000:D3}{trail}";
                        }
                        else
                        {
                            content = $"{numberSystem}{manufacturer / 100:D3}{product % 100:D2}{3}";
                        }
                    }
                    else if (manufacturer % 10 == 0)
                    {
                        content = $"{numberSystem}{manufacturer / 10:D4}{product % 10:D1}{4}";
                    }
                    else
                    {
                        content = $"{numberSystem}{manufacturer:D5}{Math.Max(product % 10, 5):D1}";
                    }
                }

                string interpretation = content;

                if (barcode.PrintCheckDigit)
                {
                    string expanded = UPCEReader.convertUPCEtoUPCA(content);
                    int checksum = 0;
                    for (int i = 0; i < 11; i++)
                    {
                        checksum += (expanded[i] - 48) * (i % 2 * 2 + 7);
                    }

                    interpretation = string.Format("{0}{1}", interpretation, checksum % 10);
                }

                UPCEWriter writer = new();
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
                        this.DrawUpcEInterpretationLine(result, interpretation, x, y, resizedImage.Width, resizedImage.Height, barcode.FieldOrigin != null, barcode.FieldOrientation, barcode.ModuleWidth, options, internationalFont, printDensityDpmm);
                    }
                }

                return this.CalculateNextDefaultPosition(x, y, resizedImage.Width, resizedImage.Height, barcode.FieldOrigin != null, barcode.FieldOrientation, currentPosition);
            }

            return currentPosition;
        }

        private void DrawUpcEInterpretationLine(
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

                    using SKBitmap bitmapGuardImage = BoolArrayToSKBitmap(guards, (int)(bitmapMargin + metrics.RenderedGlyphHeight / 2f), moduleWidth);
                    byte[] bitmapGuardPng = bitmapGuardImage.Encode(SKEncodedImageFormat.Png, 100).ToArray();
                    this.skCanvas.DrawBitmap(SKBitmap.Decode(bitmapGuardPng), x, y + barcodeHeight);

                    using SKPaint paint = ZplBitmapFontRenderer.CreatePaint(false);
                    for (int i = 0; i < interpretation.Length; i++)
                    {
                        string digit = interpretation[i].ToString();
                        int digitWidth = ZplBitmapFontRenderer.MeasureTextWidth(digit, metrics);
                        this.DrawBitmapDigit(digit, fontData, metrics, x - (bitmapSpacing + digitWidth) / 2f - moduleWidth, y + barcodeHeight + bitmapMargin, paint);
                        x += bitmapSpacing;

                        if (i == 0)
                        {
                            x += moduleWidth * 4;
                        }
                        else if (i == 6)
                        {
                            x += moduleWidth * 6;
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
                this.skCanvas.DrawBitmap(SKBitmap.Decode(guardPng), x, y + barcodeHeight);

                for (int i = 0; i < interpretation.Length; i++)
                {
                    string digit = interpretation[i].ToString();
                    skFont.MeasureText(digit, out SKRect digitBounds);
                    this.skCanvas.DrawText(digit, x - (spacing + digitBounds.Width) / 2 - moduleWidth, y + barcodeHeight + textBounds.Height + margin, skFont, skPaint);
                    x += spacing;

                    if (i == 0)
                    {
                        x += moduleWidth * 4;
                    }
                    else if (i == 6)
                    {
                        x += moduleWidth * 6;
                    }
                }
            }
        }

    }
}
