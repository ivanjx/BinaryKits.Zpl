using BinaryKits.Zpl.Label.Elements;
using SkiaSharp;
using System;
using ZXing.OneD;

namespace BinaryKits.Zpl.Viewer.ElementDrawers;

public class BarcodeUPCAElementDrawer : BarcodeDrawerBase
{
    public override bool CanDraw(ZplElementBase element)
    {
        return element is ZplBarcodeUpcA;
    }

    public override void Draw(ZplElementBase element, DrawerOptions options)
    {
        if (element is ZplBarcodeUpcA barcode)
        {
            float x = barcode.PositionX;
            float y = barcode.PositionY;

            var content = barcode.Content;
            content = content.PadLeft(11, '0').Substring(0, 11);
            var interpretation = content;

            int checksum = 0;
            for (int i = 1; i < 12; i++)
            {
                checksum += (content[i - 1] - 48) * (9 - i % 2 * 2);
            }
            content = content.Substring(0, 11);
            interpretation = string.Format("{0}{1}", interpretation, checksum % 10);

            var writer = new EAN13Writer();
            var result = writer.encode(content.PadLeft(12, '0'));
            using var resizedImage = this.BoolArrayToSKBitmap(result, barcode.Height, barcode.ModuleWidth);
            var png = resizedImage.Encode(SKEncodedImageFormat.Png, 100).ToArray();
            this.DrawBarcode(png, x, y, resizedImage.Width, resizedImage.Height, barcode.FieldOrigin != null,
                barcode.FieldOrientation);

            if (barcode.PrintInterpretationLine)
            {
                float labelFontSize = Math.Min(barcode.ModuleWidth * 10f, 100f);
                var labelTypeFace = options.FontLoader("A");
                var labelFont = new SKFont(labelTypeFace, labelFontSize);
                if (barcode.PrintInterpretationLineAboveCode)
                {
                    this.DrawInterpretationLine(interpretation, labelFont, x, y, resizedImage.Width,
                        resizedImage.Height, barcode.FieldOrigin != null, barcode.FieldOrientation, true, options);
                }
                else
                {
                    this.DrawUPCAInterpretationLine(result, interpretation, labelFont, x, y, resizedImage.Width,
                        resizedImage.Height, barcode.FieldOrigin != null, barcode.PrintCheckDigit,
                        barcode.FieldOrientation, barcode.ModuleWidth,
                        options);
                }
            }

        }
    }

    private void DrawUPCAInterpretationLine(
        bool[] encoded,
        string interpretation,
        SKFont skFont,
        float x,
        float y,
        int barcodeWidth,
        int barcodeHeight,
        bool useFieldOrigin,
        bool drawCheckDigit,
        Label.FieldOrientation fieldOrientation,
        int moduleWidth,
        DrawerOptions options)
    {
        using (new SKAutoCanvasRestore(this._skCanvas))
        {
            using var skPaint = new SKPaint();
            skPaint.IsAntialias = options.Antialias;

            SKMatrix matrix = this.GetRotationMatrix(x, y, barcodeWidth, barcodeHeight, useFieldOrigin, fieldOrientation);

            if (matrix != SKMatrix.Empty)
            {
                var currentMatrix = _skCanvas.TotalMatrix;
                var concatMatrix = SKMatrix.Concat(currentMatrix, matrix);
                this._skCanvas.SetMatrix(in concatMatrix);
            }

            skFont.MeasureText(interpretation, out var textBounds);

            if (!useFieldOrigin)
            {
                y -= barcodeHeight;
            }

            float margin = Math.Max((skFont.Spacing - textBounds.Height) / 2, MIN_LABEL_MARGIN);
            int spacing = moduleWidth * 7;
            bool[] guards = new bool[encoded.Length];

            for (int i = 0; i < guards.Length; i++)
            {
                if (i < 10 ||
                    i == 46 ||
                    i == 47 ||
                    i == 48 ||
                    i >= 85)
                {
                    guards[i] = encoded[i];
                }
            }
            
            using var guardImage = this.BoolArrayToSKBitmap(guards, (int)(margin + textBounds.Height / 2), moduleWidth);
            var guardPng = guardImage.Encode(SKEncodedImageFormat.Png, 100).ToArray();
            this._skCanvas.DrawBitmap(SKBitmap.Decode(guardPng), x, y + barcodeHeight);
            int len = interpretation.Length;

            if (!drawCheckDigit)
            {
                len = interpretation.Length - 1;
            }
            
            for (int i = 0; i < len; i++)
            {
                string digit = interpretation[i].ToString();
                skFont.MeasureText(digit, out var digitBounds);
                this._skCanvas.DrawText(digit, x - (spacing + digitBounds.Width) / 2 - moduleWidth, y + barcodeHeight + textBounds.Height + margin, skFont, skPaint);
                x += spacing;
                
                if (i == 0)
                {
                    x += moduleWidth * 11;
                }

                if (i == 5)
                {
                    x += moduleWidth * 4;
                }

                if (i == 10)
                {
                    x += moduleWidth * 11;
                }
            }
        }
    }
}
