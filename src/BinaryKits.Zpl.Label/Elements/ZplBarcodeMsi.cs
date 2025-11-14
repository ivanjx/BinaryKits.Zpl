using System.Collections.Generic;

namespace BinaryKits.Zpl.Label.Elements;

public class ZplBarcodeMsi : ZplBarcode
{
    public MsiBarcodeCheckDigitMode CheckDigitMode { get; private set; }
    public bool PrintCheckDigit { get; private set; }

    public ZplBarcodeMsi(
        string content,
        int positionX,
        int positionY,
        int height,
        int moduleWidth,
        double wideBarToNarrowBarWidthRatio,
        FieldOrientation fieldOrientation,
        MsiBarcodeCheckDigitMode checkDigitMode,
        char? hexadecimalIndicator,
        bool printInterpretationLine = true,
        bool printInterpretationLineAboveCode = false,
        bool printCheckDigit = false,
        bool bottomToTop = false,
        bool useDefaultPosition = false) :
        base(
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
        CheckDigitMode = checkDigitMode;
        PrintCheckDigit = printCheckDigit;
    }

    public override IEnumerable<string> Render(ZplRenderOptions context)
    {
        List<string> result = [];
        result.AddRange(RenderPosition(context));
        result.Add(RenderModuleWidth());
        result.Add($"^BM{RenderFieldOrientation()},{RenderPrintCheckDigitMode()},{context.Scale(Height)},{RenderPrintInterpretationLine()},{RenderPrintInterpretationLineAboveCode()},{RenderBoolean(PrintCheckDigit)}");
        result.Add($"^FD{Content}^FS");
        return result;
    }

    private string RenderPrintCheckDigitMode()
    {
        return CheckDigitMode switch
        {
            MsiBarcodeCheckDigitMode.None => "A",
            MsiBarcodeCheckDigitMode.Mod2_10 => "C",
            MsiBarcodeCheckDigitMode.Mod_1_11_1_10 => "D",
            _ => "B"
        };
    }
}
