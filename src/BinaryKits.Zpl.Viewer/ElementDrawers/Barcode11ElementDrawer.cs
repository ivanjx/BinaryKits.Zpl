using BinaryKits.Zpl.Label;
using BinaryKits.Zpl.Label.Elements;
using BinaryKits.Zpl.Viewer.Helpers;
using BinaryKits.Zpl.Viewer.Symologies;

using SkiaSharp;

using System;

namespace BinaryKits.Zpl.Viewer.ElementDrawers;

public class Barcode11ElementDrawer : BarcodeDrawerBase
{
    public override bool CanDraw(ZplElementBase element)
    {
        return element is ZplBarcode11;
    }

    public override SKPoint Draw(ZplElementBase element, DrawerOptions options, SKPoint currentPosition, InternationalFont internationalFont, int printDensityDpmm)
    {
        if (element is not ZplBarcode11 barcode)
        {
            return currentPosition;
        }

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

        (bool[] data, string interpretation) = Code11Symbology.Encode(content, barcode.CheckDigitCount);
        int narrow = barcode.ModuleWidth;
        int wide = (int)Math.Floor(barcode.WideBarToNarrowBarWidthRatio * narrow);
        bool[] result = AdjustWidths(data, wide, narrow);
        using SKBitmap resizedImage = BoolArrayToSKBitmap(result, barcode.Height);
        byte[] png = resizedImage.Encode(SKEncodedImageFormat.Png, 100).ToArray();
        this.DrawBarcode(png, x, y, resizedImage.Width, resizedImage.Height, barcode.FieldOrigin != null, barcode.FieldOrientation);

        if (barcode.PrintInterpretationLine)
        {
            this.DrawBitmapInterpretationLine(interpretation, x, y, resizedImage.Width, resizedImage.Height, barcode.FieldOrigin != null, barcode.FieldOrientation, barcode.PrintInterpretationLineAboveCode, options, internationalFont, printDensityDpmm, barcode.ModuleWidth);
        }

        return this.CalculateNextDefaultPosition(x, y, resizedImage.Width, resizedImage.Height, barcode.FieldOrigin != null, barcode.FieldOrientation, currentPosition);
    }
}
