using BinaryKits.Zpl.Label.Elements;
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

    public override void Draw(ZplElementBase element, DrawerOptions options)
    {
        if (element is not ZplBarcodeLogmars barcode)
        {
            return;
        }

        if (string.IsNullOrEmpty(barcode.Content))
        {
            return;
        }

        float x = barcode.PositionX;
        float y = barcode.PositionY;
        string content = barcode.Content;
        char? mod43 = CalculateMod43(content);

        if (mod43 == null)
        {
            // Invalid characters for LOGMARS barcode.
            return;
        }

        content += mod43.Value;
        string interpretation = content;

        var writer = new Code39Writer();
        var result = writer.encode(content);
        int narrow = barcode.ModuleWidth;
        int wide = (int)Math.Floor(barcode.WideBarToNarrowBarWidthRatio * narrow);
        result = this.AdjustWidths(result, wide, narrow);
        using var resizedImage = this.BoolArrayToSKBitmap(result, barcode.Height);
        var png = resizedImage.Encode(SKEncodedImageFormat.Png, 100).ToArray();
        this.DrawBarcode(png, x, y, resizedImage.Width, resizedImage.Height, barcode.FieldOrigin != null, barcode.FieldOrientation);

        if (barcode.PrintInterpretationLine)
        {
            float labelFontSize = Math.Min(barcode.ModuleWidth * 10f, 100f);
            var labelTypeFace = options.FontLoader("A");
            var labelFont = new SKFont(labelTypeFace, labelFontSize);
            this.DrawInterpretationLine(interpretation, labelFont, x, y, resizedImage.Width, resizedImage.Height, barcode.FieldOrigin != null, barcode.FieldOrientation, barcode.PrintInterpretationLineAboveCode, options);
        }
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
