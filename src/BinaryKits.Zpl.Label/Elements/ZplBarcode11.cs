using System.Collections.Generic;

namespace BinaryKits.Zpl.Label.Elements;

public class ZplBarcode11 : ZplBarcode
{
    public Code11CheckDigitCount CheckDigitCount { get; private set; }

    public ZplBarcode11(
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
        Code11CheckDigitCount checkDigitCount = Code11CheckDigitCount.Two,
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
        CheckDigitCount = checkDigitCount;
    }

    public override IEnumerable<string> Render(ZplRenderOptions context)
    {
        List<string> result = [];
        result.AddRange(RenderPosition(context));
        result.Add(RenderModuleWidth());
        result.Add($"^B1{RenderFieldOrientation()},{RenderCheckDigitCount()},{context.Scale(Height)},{RenderPrintInterpretationLine()},{RenderPrintInterpretationLineAboveCode()}");
        result.Add(RenderFieldDataSection());

        return result;
    }

    private string RenderCheckDigitCount()
    {
        return CheckDigitCount == Code11CheckDigitCount.One ? "Y" : "N";
    }
}
