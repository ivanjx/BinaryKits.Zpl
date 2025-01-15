using System;
using System.Collections.Generic;

namespace BinaryKits.Zpl.Label.Elements;

public class ZplBarcodeUpcExtension : ZplBarcode
{
    public ZplBarcodeUpcExtension(
        string content,
        int positionX,
        int positionY,
        int height,
        int moduleWidth,
        double wideBarToNarrowBarWidthRatio,
        FieldOrientation fieldOrientation,
        bool printInterpretationLine,
        bool printInterpretationLineAboveCode,
        bool bottomToTop = false) :
        base(
            content,
            positionX,
            positionY,
            height,
            moduleWidth,
            wideBarToNarrowBarWidthRatio,
            fieldOrientation,
            printInterpretationLine,
            printInterpretationLineAboveCode,
            bottomToTop)
    {
        if (!IsDigitsOnly(content))
        {
            throw new ArgumentException("UPC Extension Barcode allow only digits", nameof(content));
        }
    }
    
    public override IEnumerable<string> Render(ZplRenderOptions context)
    {
        var result = new List<string>();
        result.AddRange(RenderPosition(context));
        result.Add(RenderModuleWidth());
        result.Add($"^BS{RenderFieldOrientation()},{context.Scale(Height)},{RenderPrintInterpretationLine()},{RenderPrintInterpretationLineAboveCode()}");
        result.Add($"^FD{Content}^FS");

        return result;
    }
}
