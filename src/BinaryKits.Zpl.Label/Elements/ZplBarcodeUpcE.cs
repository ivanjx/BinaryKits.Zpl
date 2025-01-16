using System;
using System.Collections.Generic;

namespace BinaryKits.Zpl.Label.Elements;

public class ZplBarcodeUpcE : ZplBarcode
{
    public bool PrintCheckDigit { get; }
    
    public ZplBarcodeUpcE(
        string content,
        int positionX,
        int positionY,
        int height,
        int moduleWidth,
        double wideBarToNarrowBarWidthRatio,
        FieldOrientation fieldOrientation,
        bool printInterpretationLine,
        bool printInterpretationLineAboveCode,
        bool printCheckDigit,
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
        PrintCheckDigit = printCheckDigit;
        
        if (!IsDigitsOnly(content))
        {
            throw new ArgumentException("UPC-E Barcode allow only digits", nameof(content));
        }
    }
    
    public override IEnumerable<string> Render(ZplRenderOptions context)
    {
        var result = new List<string>();
        result.AddRange(RenderPosition(context));
        result.Add(RenderModuleWidth());
        result.Add($"^B9{RenderFieldOrientation()},{context.Scale(Height)},{RenderPrintInterpretationLine()},{RenderPrintInterpretationLineAboveCode()},{this.RenderPrintCheckDigit()}");
        result.Add($"^FD{Content}^FS");

        return result;
    }
    
    private string RenderPrintCheckDigit() => PrintCheckDigit ? "Y" : "N";
}
