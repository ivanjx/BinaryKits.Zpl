using System.Collections.Generic;

namespace BinaryKits.Zpl.Label.Elements
{
    /// <summary>
    /// Industrial 2 of 5 barcode (^BI).
    /// </summary>
    public class ZplBarcodeIndustrial2of5 : ZplBarcode
    {
        /// <summary>
        /// Industrial 2 of 5 barcode (^BI).
        /// </summary>
        public ZplBarcodeIndustrial2of5(
            string content,
            int positionX,
            int positionY,
            int height = 100,
            int moduleWidth = 2,
            double wideBarToNarrowBarWidthRatio = 3,
            FieldOrientation fieldOrientation = FieldOrientation.Normal,
            char? hexadecimalIndicator = null,
            bool printInterpretationLine = true,
            bool printInterpretationLineAboveCode = false,
            bool bottomToTop = false,
            bool useDefaultPosition = false)
            : base(
                  content,
                  positionX,
                  positionY,
                  height,
                  moduleWidth,
                  wideBarToNarrowBarWidthRatio,
                  fieldOrientation,
                  hexadecimalIndicator,
                  printInterpretationLine,
                  printInterpretationLineAboveCode,
                  bottomToTop,
                  useDefaultPosition)
        {
        }

        ///<inheritdoc/>
        public override IEnumerable<string> Render(ZplRenderOptions context)
        {
            List<string> result = [];
            result.AddRange(RenderPosition(context));
            result.Add(RenderModuleWidth());
            result.Add($"^BI{RenderFieldOrientation()},{context.Scale(Height)},{RenderPrintInterpretationLine()},{RenderPrintInterpretationLineAboveCode()}");
            result.Add(RenderFieldDataSection());

            return result;
        }
    }
}
