using BinaryKits.Zpl.Label.Elements;
using SkiaSharp;
using System;
using ZXing.OneD;

namespace BinaryKits.Zpl.Viewer.ElementDrawers
{
    /// <summary>
    /// Drawer for Code 39 Barcode elements
    /// </summary>
    public class BarcodeAnsiCodabarElementDrawer : BarcodeDrawerBase
    {
        ///<inheritdoc/>
        public override bool CanDraw(ZplElementBase element)
        {
            return element is ZplBarcodeAnsiCodabar;
        }

        ///<inheritdoc/>
        public override void Draw(ZplElementBase element, DrawerOptions options)
        {
            if (element is ZplBarcodeAnsiCodabar barcode)
            {
                float x = barcode.PositionX;
                float y = barcode.PositionY;
                char start = barcode.StartCharacter;
                char end = barcode.StopCharacter;
                var content = barcode.Content.Trim('*');
                var interpretation = $"{start}{content}{end}";

                if (string.IsNullOrEmpty(content))
                {
                    return;
                }

                var writer = new CodaBarWriter();
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
        }
    }
}
