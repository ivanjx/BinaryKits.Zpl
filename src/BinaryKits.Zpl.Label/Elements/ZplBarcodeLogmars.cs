using System.Collections.Generic;

namespace BinaryKits.Zpl.Label.Elements;

public class ZplBarcodeLogmars : ZplBarcode
{
    public ZplBarcodeLogmars(
        string content,
        int positionX,
        int positionY,
        int height,
        int moduleWidth,
        double wideBarToNarrowBarWidthRatio,
        FieldOrientation fieldOrientation,
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
            true, // printInterpretationLine
            printInterpretationLineAboveCode,
            bottomToTop)
    {
    }

    public override IEnumerable<string> Render(ZplRenderOptions context)
    {
        List<string> result = [];
        result.AddRange(RenderPosition(context));
        result.Add(RenderModuleWidth());
        result.Add($"^BL{RenderFieldOrientation()},{context.Scale(Height)},{RenderPrintInterpretationLineAboveCode()}");
        result.Add($"^FD{Content}^FS");
        return result;
    }
}
