using BinaryKits.Zpl.Label;
using BinaryKits.Zpl.Label.Elements;
using BinaryKits.Zpl.Viewer.Helpers;

using SkiaSharp;

using System;
using System.Linq;

namespace BinaryKits.Zpl.Viewer.ElementDrawers
{
    /// <summary>
    /// Drawer for Industrial 2 of 5 barcode elements.
    /// </summary>
    public class Industrial2of5BarcodeDrawer : BarcodeDrawerBase
    {
        private static readonly string[] digitPatterns =
        [
            "10101110111010",
            "11101010101110",
            "10111010101110",
            "11101110101010",
            "10101110101110",
            "11101011101010",
            "10111011101010",
            "10101011101110",
            "11101010111010",
            "10111010111010"
        ];

        ///<inheritdoc/>
        public override bool CanDraw(ZplElementBase element)
        {
            return element is ZplBarcodeIndustrial2of5;
        }

        ///<inheritdoc/>
        public override SKPoint Draw(ZplElementBase element, DrawerOptions options, SKPoint currentPosition, InternationalFont internationalFont)
        {
            if (element is ZplBarcodeIndustrial2of5 barcode)
            {
                if (string.IsNullOrEmpty(barcode.Content))
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

                if (string.IsNullOrEmpty(content) || content.Any(c => !char.IsDigit(c)))
                {
                    return currentPosition;
                }

                bool[] result = EncodeIndustrial2of5(content);
                int narrow = barcode.ModuleWidth;
                int wide = Math.Max(narrow, (int)Math.Floor(barcode.WideBarToNarrowBarWidthRatio * narrow));
                result = AdjustWidths(result, wide, narrow);

                using SKBitmap resizedImage = BoolArrayToSKBitmap(result, barcode.Height);
                byte[] png = resizedImage.Encode(SKEncodedImageFormat.Png, 100).ToArray();
                this.DrawBarcode(png, x, y, resizedImage.Width, resizedImage.Height, barcode.FieldOrigin != null, barcode.FieldOrientation);

                if (barcode.PrintInterpretationLine)
                {
                    float labelFontSize = Math.Min(barcode.ModuleWidth * 10f, 100f);
                    SKTypeface labelTypeFace = options.FontManager.FontLoader("A");
                    SKFont labelFont = new(labelTypeFace, labelFontSize);
                    this.DrawInterpretationLine(content, labelFont, x, y, resizedImage.Width, resizedImage.Height, barcode.FieldOrigin != null, barcode.FieldOrientation, barcode.PrintInterpretationLineAboveCode, options);
                }

                return this.CalculateNextDefaultPosition(x, y, resizedImage.Width, resizedImage.Height, barcode.FieldOrigin != null, barcode.FieldOrientation, currentPosition);
            }

            return currentPosition;
        }

        private static bool[] EncodeIndustrial2of5(string content)
        {
            string encodedValue = "11011010";

            for (int index = 0; index < content.Length; index++)
            {
                int digit = content[index] - '0';
                encodedValue += digitPatterns[digit];
            }

            encodedValue += "11010110";
            return encodedValue.Select(bit => bit == '1').ToArray();
        }
    }
}
