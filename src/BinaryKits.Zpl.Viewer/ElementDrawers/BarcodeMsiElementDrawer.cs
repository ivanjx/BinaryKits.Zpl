using BinaryKits.Zpl.Label;
using BinaryKits.Zpl.Label.Elements;
using SkiaSharp;
using System;
using System.Linq;
using ZXing.OneD;

namespace BinaryKits.Zpl.Viewer.ElementDrawers;

public class BarcodeMsiElementDrawer : BarcodeDrawerBase
{
    public override bool CanDraw(ZplElementBase element)
    {
        return element is ZplBarcodeMsi;
    }

    public override void Draw(ZplElementBase element, DrawerOptions options)
    {
        if (element is not ZplBarcodeMsi msi)
        {
            return;
        }

        float x = msi.PositionX;
        float y = msi.PositionY;
        string content = msi.Content.Trim();
        string checkDigit = msi.CheckDigitMode switch
        {
            MsiBarcodeCheckDigitMode.None => NoCheck(content),
            MsiBarcodeCheckDigitMode.Mod1_10 => Mod10(content),
            MsiBarcodeCheckDigitMode.Mod2_10 => DoubleMod10(content),
            MsiBarcodeCheckDigitMode.Mod_1_11_1_10 => Mod11AndMod10(content),
            _ => ""
        };
        checkDigit = checkDigit.Substring(content.Length);
        string interpretation = content;
        content += checkDigit;

        if (msi.PrintCheckDigit)
        {
            interpretation += checkDigit;
        }

        MSIWriter writer = new();
        var result = writer.encode(content);
        int narrow = msi.ModuleWidth;
        int wide = (int)Math.Floor(msi.WideBarToNarrowBarWidthRatio * narrow);
        result = this.AdjustWidths(result, wide, narrow);
        using var resizedImage = this.BoolArrayToSKBitmap(result, msi.Height);
        var png = resizedImage.Encode(SKEncodedImageFormat.Png, 100).ToArray();
        this.DrawBarcode(png, x, y, resizedImage.Width, resizedImage.Height, msi.FieldOrigin != null, msi.FieldOrientation);

        if (msi.PrintInterpretationLine)
        {
            float labelFontSize = Math.Min(msi.ModuleWidth * 10f, 100f);
            var labelTypeFace = options.FontLoader("A");
            var labelFont = new SKFont(labelTypeFace, labelFontSize);
            this.DrawInterpretationLine(interpretation, labelFont, x, y, resizedImage.Width, resizedImage.Height, msi.FieldOrigin != null, msi.FieldOrientation, msi.PrintInterpretationLineAboveCode, options);
        }
    }

    // A: No check digit
    private string NoCheck(string input) => input;

    // B: 1 Mod 10 (Luhn-like)
    private string Mod10(string input)
    {
        if (string.IsNullOrEmpty(input) ||
            input.Any(c => c < '0' || c > '9'))
        {
            return string.Empty;
        }
        
        int sum = 0;
        bool doubleIt = true;

        for (int i = input.Length - 1; i >= 0; i--)
        {
            int digit = input[i] - '0';
            if (doubleIt)
            {
                digit *= 2;
                if (digit > 9) digit -= 9;
            }
            sum += digit;
            doubleIt = !doubleIt;
        }

        int check = (10 - (sum % 10)) % 10;
        return input + check.ToString();
    }

    // C: 2 Mod 10 (Double Mod 10, i.e. Mod 10, then Mod 10 again)
    private string DoubleMod10(string input)
    {
        if (string.IsNullOrEmpty(input) ||
            input.Any(c => c < '0' || c > '9'))
        {
            return string.Empty;
        }

        string first = Mod10(input);

        if (string.IsNullOrEmpty(first))
        {
            return string.Empty;
        }

        return Mod10(first);
    }

    // D: 1 Mod 11 and 1 Mod 10 (Mod 11, then append Mod 10)
    private string Mod11AndMod10(string input)
    {
        if (string.IsNullOrEmpty(input) ||
            input.Any(c => c < '0' || c > '9'))
        {
            return string.Empty;
        }

        string mod11 = input + Mod11CheckDigit(input);
        return Mod10(mod11);
    }

    // Helper: Single Mod 11 (IBM version, weights 2..7)
    private static string Mod11CheckDigit(string input)
    {
        int sum = 0;
        int weight = 2;
        for (int i = input.Length - 1; i >= 0; i--)
        {
            sum += (input[i] - '0') * weight;
            weight++;
            if (weight > 7) weight = 2;
        }
        int check = sum % 11;
        if (check == 10) return "0";
        return check.ToString();
    }
}
