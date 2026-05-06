using BinaryKits.Zpl.Label;
using BinaryKits.Zpl.Label.Elements;
using BinaryKits.Zpl.Viewer.Helpers;
using BinaryKits.Zpl.Viewer.Symologies;

using SkiaSharp;

using System;
using System.Collections.Generic;

namespace BinaryKits.Zpl.Viewer.ElementDrawers
{
    /// <summary>
    /// Drawer for Code 128 Barcode elements
    /// </summary>
    public class Barcode128ElementDrawer : BarcodeDrawerBase
    {
        ///<inheritdoc/>
        public override bool CanDraw(ZplElementBase element)
        {
            return element is ZplBarcode128;
        }

        ///<inheritdoc/>
        public override SKPoint Draw(ZplElementBase element, DrawerOptions options, SKPoint currentPosition, InternationalFont internationalFont, int printDensityDpmm)
        {
            if (element is ZplBarcode128 barcode)
            {
                string content = barcode.Content;

                if (string.IsNullOrEmpty(content))
                {
                    return currentPosition;
                }

                if (barcode.HexadecimalIndicator is char hexIndicator)
                {
                    content = content.ReplaceHexEscapes(hexIndicator, internationalFont);
                }

                string mode = string.IsNullOrWhiteSpace(barcode.Mode) ? "N" : barcode.Mode.ToUpperInvariant();
                Code128CodeSet codeSet = Code128CodeSet.Code128B;
                bool gs1 = false;
                if (mode == "A")
                {
                    codeSet = Code128CodeSet.Code128;
                }
                else if (mode == "D")
                {
                    codeSet = Code128CodeSet.Code128;
                    gs1 = true;
                }
                else if (mode == "U")
                {
                    codeSet = Code128CodeSet.Code128C;
                }

                float x = barcode.PositionX;
                float y = barcode.PositionY;

                bool[] data;
                string interpretation;

                try
                {
                    (data, interpretation) = ZplCode128Symbology.Encode(content, codeSet, gs1, mode, barcode.UccCheckDigit);
                }
                catch
                {
                    return currentPosition;
                }

                if (barcode.UseDefaultPosition)
                {
                    x = currentPosition.X;
                    y = currentPosition.Y;
                }

                using SKBitmap resizedImage = BoolArrayToSKBitmap(data, barcode.Height, barcode.ModuleWidth);
                byte[] png = resizedImage.Encode(SKEncodedImageFormat.Png, 100).ToArray();
                this.DrawBarcode(png, x, y, resizedImage.Width, resizedImage.Height, barcode.FieldOrigin != null, barcode.FieldOrientation);

                if (barcode.PrintInterpretationLine)
                {
                    this.DrawBitmapInterpretationLine(interpretation, x, y, resizedImage.Width, resizedImage.Height, barcode.FieldOrigin != null, barcode.FieldOrientation, barcode.PrintInterpretationLineAboveCode, options, internationalFont, printDensityDpmm, barcode.ModuleWidth);
                }

                return this.CalculateNextDefaultPosition(x, y, resizedImage.Width, resizedImage.Height, barcode.FieldOrigin != null, barcode.FieldOrientation, currentPosition);
            }

            return currentPosition;
        }

    }
}
