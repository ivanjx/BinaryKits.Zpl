using BinaryKits.Zpl.Label;
using BinaryKits.Zpl.Label.Elements;
using BinaryKits.Zpl.Viewer.Helpers;
using SkiaSharp;
using System;
using ZXing.OneD;

namespace BinaryKits.Zpl.Viewer.ElementDrawers;

public class BarcodeLogmarsElementDrawer : BarcodeDrawerBase
{
    public override bool CanDraw(ZplElementBase element)
    {
        return element is ZplBarcodeLogmars;
    }

    public override SKPoint Draw(ZplElementBase element, DrawerOptions options, SKPoint currentPosition, InternationalFont internationalFont)
    {
        if (element is not ZplBarcodeLogmars barcode)
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

        if (string.IsNullOrEmpty(content))
        {
            return currentPosition;
        }

        char? mod43 = CalculateMod43(content);

        if (mod43 == null)
        {
            // Invalid characters for LOGMARS barcode.
            return currentPosition;
        }

        content += mod43.Value;
        string interpretation = content;

        Code39Writer writer = new();
        bool[] result = writer.encode(content);
        int narrow = barcode.ModuleWidth;
        int wide = (int)Math.Floor(barcode.WideBarToNarrowBarWidthRatio * narrow);
        result = AdjustWidths(result, wide, narrow);
        using SKBitmap resizedImage = BoolArrayToSKBitmap(result, barcode.Height);
        byte[] png = resizedImage.Encode(SKEncodedImageFormat.Png, 100).ToArray();
        this.DrawBarcode(png, x, y, resizedImage.Width, resizedImage.Height, barcode.FieldOrigin != null, barcode.FieldOrientation);

        if (barcode.PrintInterpretationLine)
        {
            float labelFontSize = Math.Min(barcode.ModuleWidth * 10f, 100f);
            SKTypeface labelTypeFace = options.FontLoader("A");
            SKFont labelFont = new(labelTypeFace, labelFontSize);
            this.DrawInterpretationLine(interpretation, labelFont, x, y, resizedImage.Width, resizedImage.Height, barcode.FieldOrigin != null, barcode.FieldOrientation, barcode.PrintInterpretationLineAboveCode, options);
        }

        return this.CalculateNextDefaultPosition(x, y, resizedImage.Width, resizedImage.Height, barcode.FieldOrigin != null, barcode.FieldOrientation, currentPosition);
    }

    private static char? CalculateMod43(string input)
    {
        const string charset = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ-. $/+%";
        int sum = 0;

        foreach (char c in input)
        {
            int idx = charset.IndexOf(c);
            if (idx == -1) return null;
            sum += idx;
        }

        return charset[sum % 43];
    }
}
